"""Authored explorer sculpture and garment construction revision.
Original Rivet Reach artwork, copyright (c) 2026 Starbugstone. See LICENSE.md.
Run in Blender background mode. Versioned/idempotent on the current source;
--from-backup is a local iteration option, never needed by a checkout.
"""
import bpy, bmesh, math, json, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
sys.dont_write_bytecode=True;sys.path.insert(0,str(ROOT/'Tools'))
from refine_player_surfaces import activate

def glove_lining(model):
    # The lining uses the same leather under compressed thumb creases, so the
    # underlying skin cannot flash through the fitted outer surface during flexion.
    uv=model.data.uv_layers.active
    for p in model.data.polygons:
        co=uv.data[p.loop_start].uv;tile=int(co.x*4)+4*int(co.y*4)
        if tile not in (4,5) or co.x*4%1<.05 and co.y*4%1<.05:continue
        if all(.879<v.co.z<.967 for v in (model.data.vertices[i] for i in p.vertices)):
            for li in p.loop_indices:uv.data[li].uv=((tile-4+.978)/4,1.5/4)

def sculpt(rig, model):
    # Dense fitted plates follow the fingertip curvature through tight shaft grips.
    if model.get('avatar_nail_revision')!=2:
        from polish_player_rig import renew_nails
        renew_nails(rig,model,rings=8,sides=32)
        model['avatar_nail_revision']=2
    for polygon in model.data.polygons:polygon.material_index=0
    while len(model.data.materials)>1:model.data.materials.pop(index=1)
    if model.get('avatar_rework_version')==1:return glove_lining(model)
    rig.data.pose_position='REST'
    uv=model.data.uv_layers.active
    tiles={}
    for p in model.data.polygons:
        co=uv.data[p.loop_start].uv;tile=int(co.x*4)+4*int(co.y*4)
        for i in p.vertices:tiles[i]=tile
    names={g.index:g.name for g in model.vertex_groups}
    # Skin is still a connected surface. Sculpt the forearm extensor ridge and
    # flattened dorsal palm without moving the palmar shaft-contact surface.
    for v in model.data.vertices:
        tile=tiles.get(v.index,-1);x,y,z=v.co;side='L' if x>0 else 'R';sign=1 if x>0 else -1
        wx=rig.data.bones['Hand'+side].head_local.x
        if tile in (4,5):
            dorsal=max(0,min(1,(y+.035)/.027))
            if .944<z<1.17:
                t=(z-.944)/.226;cx=wx-sign*.025*t
                angle=math.atan2(y+.026,x-cx);rad=Vector((x-cx,y+.026,0)).normalized()
                relief=(.0025*math.cos(angle*2+.5)*math.sin(math.pi*t)**2
                        +.0014*math.exp(-((z-1.015)/.035)**2)*math.cos(angle*3))
                v.co+=rad*relief
            elif .875<z<.942:
                # Metacarpal tendons, shallow knuckle valleys, fleshy thumb pad.
                ridge=sum(math.exp(-((x-wx-off)/.004)**2) for off in [-.028,-.008,.014,.034])
                v.co.y+=dorsal*(.0017*ridge*math.exp(-((z-.902)/.027)**2)-.001*math.exp(-((z-.923)/.005)**2))
        if tile in (2,3):
            # Flatten the inflated sleeve silhouette; sculpt diagonal rolled folds.
            ex=rig.data.bones['Forearm'+side].head_local.x
            a=math.atan2(y+.012,x-ex);rad=Vector((x-ex,y+.012,0)).normalized()
            factor=math.sin(math.pi*max(0,min(1,(z-1.15)/.285)))
            v.co+=rad*(-.008*factor+.0028*math.sin(a*4+z*45)*factor)
        if tile in (6,7) and .32<z<.89:
            cx=sign*.128;rad=Vector((x-cx,y,0)).normalized();a=math.atan2(y,x-cx)
            # Tapered work trousers with a pressed front plane and knee folds.
            v.co+=rad*(-.006*math.sin(math.pi*(z-.32)/.57)+.003*math.cos(a*4)*math.exp(-((z-.62)/.19)**2))
        if z>1.68:
            # Compress the tall rounded crown into the reference's swept silhouette.
            v.co.z=1.68+(z-1.68)*.87
    # Smooth only the main facial shell, retaining deliberately shaped eyelids/nose.
    bm=bmesh.new();bm.from_mesh(model.data);bm.verts.ensure_lookup_table()
    unseen=set(bm.verts);components=[]
    while unseen:
        seed=unseen.pop();component=[seed]
        queue=[seed]
        while queue:
            current=queue.pop()
            for edge in current.link_edges:
                other=edge.other_vert(current)
                if other in unseen:unseen.remove(other);queue.append(other);component.append(other)
        components.append(component)
    for component in components:
        if len(component)>1000 and all(v.co.z>1.50 for v in component) and all(tiles.get(v.index)==0 for v in component):
            bmesh.ops.smooth_vert(bm,verts=component,factor=.42,use_axis_x=True,use_axis_y=True,use_axis_z=True)
            bmesh.ops.smooth_vert(bm,verts=component,factor=.42,use_axis_x=True,use_axis_y=True,use_axis_z=True)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(model.data);bm.free();model.data.update()
    additions=[]
    def tube(name,path,radius,tile,weight,sides=6):
        points=[Vector(p) for p in path];verts=[];faces=[]
        for j,p in enumerate(points):
            direction=(points[min(j+1,len(points)-1)]-points[max(0,j-1)]).normalized()
            across=direction.cross(Vector((0,1,0)))
            if across.length<.01:across=direction.cross(Vector((1,0,0)))
            across.normalize();up=direction.cross(across).normalized()
            for i in range(sides):verts.append(p+radius*(across*math.cos(math.tau*i/sides)+up*math.sin(math.tau*i/sides)))
        for j in range(len(points)-1):
            for i in range(sides):faces.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i))
        faces.extend([tuple(reversed(range(sides))),tuple(range((len(points)-1)*sides,len(points)*sides))])
        mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
        ob=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(ob);mesh.materials.append(model.data.materials[0]);layer=mesh.uv_layers.new(name='SkinUV')
        for p in mesh.polygons:
            p.use_smooth=True
            for li in p.loop_indices:layer.data[li].uv=((tile%4+.5)/4,(tile//4+.62)/4)
        for v in mesh.vertices:
            weights=weight(v.co) if callable(weight) else {weight:1}
            for bone,w in weights.items():
                if w>0:(ob.vertex_groups.get(bone) or ob.vertex_groups.new(name=bone)).add([v.index],w,'REPLACE')
        additions.append(ob)
    def torso(v):
        t=max(0,min(1,(v.z-1.08)/.17));return {'Spine':1-t,'Chest':t}
    # Tailoring: topstitched neckline, pocket welts, buttonholes, back adjuster.
    for sign in [-1,1]:
        path=[(sign*.018,-.143,1.31),(sign*.035,-.137,1.34),(sign*.055,-.123,1.38),(sign*.077,-.113,1.417)]
        tube('Waistcoat stitched lapel',path,.0013,13,torso)
        for z,y in [(1.083,-.124),(1.161,-.130),(1.24,-.142)]:
            if sign==1:tube('Bound buttonhole',[(.001,y,z-.008),(.001,y-.001,z+.008)],.0012,9,torso)
        side='L' if sign>0 else 'R';ex=rig.data.bones['Forearm'+side].head_local.x
        # Two rolled cuff edges and a short seam anchored to the sleeve weights.
        def sleeve(v,side=side):
            t=max(0,min(1,(v.z-1.135)/.083));return {'UpperArm'+side:t,'Forearm'+side:1-t}
        for height,radius in [(1.157,.076),(1.183,.078)]:
            path=[(ex+radius*math.cos(i*math.tau/48),-.014+radius*.92*math.sin(i*math.tau/48),height+.0015*math.sin(i*math.tau/24)) for i in range(49)]
            tube('Rolled linen topstitch '+side,path,.0009,1,sleeve)
        # Leather boot lacing follows the convex vamp, with paired brass eyelets.
        ax=sign*.139
        def bootweight(v,side=side):
            t=max(0,min(1,(v.z-.14)/.08));return {'Foot'+side:1-t,'Shin'+side:t}
        for j in range(4):
            z=.139+j*.027;y=-.125 if j==0 else -.095 if j==1 else -.083
            for s in [-1,1]:
                loop=[(ax+s*.032+.004*math.cos(a*math.tau/12),y-.006,z+.004*math.sin(a*math.tau/12)) for a in range(13)]
                tube('Boot eyelet '+side,loop,.0014,12,bootweight)
            tube('Crossed leather lace '+side,[(ax-.031,y-.007,z),(ax,y-.018,z+.012),(ax+.031,y-.007,z+.025)],.0022,9,bootweight)
            tube('Crossed leather lace '+side,[(ax+.031,y-.007,z),(ax,y-.020,z+.012),(ax-.031,y-.007,z+.025)],.0022,9,bootweight)
    # Fitted fingerless work gloves follow the existing anatomical skin and weights.
    # Reserved edge swatches in each hand tile keep gloves independently recolourable.
    source_uv=model.data.uv_layers.active
    for side in ['L','R']:
        polygons=[]
        for p in model.data.polygons:
            co=source_uv.data[p.loop_start].uv
            if co.x*4%1<.05 and co.y*4%1<.05:continue
            if int(co.x*4)+int(co.y*4)*4 != (4 if side=='L' else 5):continue
            if all(.78<v.co.z<1.17 for v in (model.data.vertices[i] for i in p.vertices)):polygons.append(p)
        used=sorted({i for p in polygons for i in p.vertices});remap={old:i for i,old in enumerate(used)}
        vertices=[model.data.vertices[i].co+model.data.vertices[i].normal*.0012 for i in used]
        faces=[tuple(remap[i] for i in p.vertices) for p in polygons]
        data=bpy.data.meshes.new('Fitted fingerless leather '+side);data.from_pydata(vertices,[],faces);data.update()
        glove=bpy.data.objects.new('Fitted work glove '+side,data);bpy.context.collection.objects.link(glove);data.materials.append(model.data.materials[0])
        layer=data.uv_layers.new(name='SkinUV')
        for p in data.polygons:
            p.use_smooth=True
            for li in p.loop_indices:layer.data[li].uv=((int(side=='R')+.978)/4,1.5/4)
        for old,new in remap.items():
            for g in model.data.vertices[old].groups:
                name=names[g.group];group=glove.vertex_groups.get(name) or glove.vertex_groups.new(name=name);group.add([new],g.weight,'REPLACE')
        # Rolled glove edges use the same weights as the adjacent skin vertices.
        bm=bmesh.new();bm.from_mesh(data);bm.verts.ensure_lookup_table();bm.edges.ensure_lookup_table()
        bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.000001,plane_co=(0,0,.879),plane_no=(0,0,1),clear_inner=True)
        bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.000001,plane_co=(0,0,.967),plane_no=(0,0,1),clear_outer=True)
        bm.normal_update()
        boundary=[e for e in bm.edges if e.is_boundary]
        # A thin inward rim gives the openings physical thickness with no floating cards.
        if boundary:
            result=bmesh.ops.extrude_edge_only(bm,edges=boundary)
            for v in [g for g in result['geom'] if isinstance(g,bmesh.types.BMVert)]:v.co-=v.normal*.0008
        bm.to_mesh(data);bm.free()
        for p in data.polygons:
            p.material_index=0;p.use_smooth=True
            for li in p.loop_indices:data.uv_layers.active.data[li].uv=((int(side=='R')+.978)/4,1.5/4)
        additions.append(glove)
    activate(model)
    for ob in additions:ob.select_set(True)
    bpy.ops.object.join()
    bpy.ops.object.mode_set(mode='WEIGHT_PAINT');bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL',limit=4)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL',lock_active=False);bpy.ops.object.mode_set(mode='OBJECT')
    # Every joined part shares a single semantic atlas and one material.
    for polygon in model.data.polygons:polygon.material_index=0
    while len(model.data.materials)>1:model.data.materials.pop(index=1)
    glove_lining(model)
    model['avatar_rework_version']=1;rig.data.pose_position='POSE'

def run():
    from create_player_assets import animate
    report=[]
    for name in ['ExplorerMale','ExplorerFemale']:
        source=ROOT/'ArtSource/Characters'/(name+'.blend')
        read=ROOT/'Logs/AvatarRework'/(name+'-before.blend') if '--from-backup' in sys.argv else source
        bpy.ops.wm.open_mainfile(filepath=str(read));rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');model=next(o for o in bpy.data.objects if o.type=='MESH')
        for im in bpy.data.images:
            if im.source=='FILE':im.filepath=str(ROOT/'Assets/RivetReach/Resources/Characters'/Path(im.filepath.replace('\\','/')).name);im.reload()
        sculpt(rig,model)
        rig.animation_data_clear()
        only=sys.argv[sys.argv.index('--clips')+1:] if '--clips' in sys.argv else None
        for action in list(bpy.data.actions):
            if only is None or action.name in only:bpy.data.actions.remove(action)
        animate(rig,abs(rig.data.bones['UpperArmL'].head_local.x),name=='ExplorerFemale',only=only)
        clips=[{'name':a.name,'seconds':(a.frame_range[1]-a.frame_range[0])/30,'loop':bool(a.get('loop',False))} for a in bpy.data.actions]
        rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
        for im in bpy.data.images:
            if im.source=='FILE':im.filepath='//../../Assets/RivetReach/Resources/Characters/'+Path(im.filepath).name
        activate(rig);model.select_set(True)
        bpy.ops.wm.save_as_mainfile(filepath=str(source))
        bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/RivetReach/Resources/Characters'/(name+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_armature_deform_only=False)
        model.data.calc_loop_triangles();report.append({'model':name,'triangles':len(model.data.loop_triangles),'bones':len(rig.data.bones),'deformBones':sum(b.use_deform for b in rig.data.bones),'materials':len(model.data.materials),'maxInfluences':max(sum(g.weight>1e-6 for g in v.groups) for v in model.data.vertices),'clips':clips,'sockets':['BlockSocket','ToolSocket']})
    (ROOT/'ArtSource/Characters/asset-report.json').write_text(json.dumps(report,indent=2)+'\n');print('AVATAR_REWORK_EXPORT_PASS',flush=True)
if __name__=='__main__':run()
