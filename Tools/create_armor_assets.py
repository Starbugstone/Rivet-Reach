"""Original fitted explorer armor and cast ingots. Copyright 2026 Starbugstone.
Blender 5.2 --background --python <this file>. Existing character sources are read only.
"""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/RivetReach/Resources/Equipment'; SRC=ROOT/'ArtSource/Equipment'
OUT.mkdir(parents=True,exist_ok=True);SRC.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
# Leather, dark recesses, bright edge metal and hammered body metal use one palette.
colours=['38271E','60432D','95653C','252C30','252C30','252C30','252C30','252C30','252C30','252C30','252C30','252C30','CE9059','252C30','252C30','AD6137']
def palette(tier,body,edge):
    cs=colours.copy();cs[12]=edge;cs[15]=body
    im=bpy.data.images.new(tier+'Palette',width=128,height=128);px=[]
    for y in range(128):
        for x in range(128):
            c=cs[x//32+y//32*4];shade=.92+.08*(y%32)/31+(((x*17+y*23)%13)-6)*.001
            px.extend([int(c[i:i+2],16)/255*shade for i in (0,2,4)]+[1])
    im.pixels[:]=px;im.filepath_raw=str(OUT/(tier+'Palette.png'));im.file_format='PNG';im.save()
    mat=bpy.data.materials.new(tier);mat.use_nodes=True;n=mat.node_tree.nodes;bs=n.get('Principled BSDF');tex=n.new('ShaderNodeTexImage');tex.image=im;tex.interpolation='Closest';mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);bs.inputs['Metallic'].default_value=.55;bs.inputs['Roughness'].default_value=.36
    return mat
mats={k:palette(k,b,e) for k,b,e in [('Copper','AE633C','E8AD70'),('Iron','697982','CAD8DC'),('Diamond','348E83','91E4CC'),('Gold','C89331','F8D979')]}
parts=[]
def mesh(name,vs,fs,tile,bone):
    me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);me.materials.append(mats['Copper'])
    uv=me.uv_layers.new(name='EquipmentUV')
    for p in me.polygons:
        for li in p.loop_indices:uv.data[li].uv=((tile%4+.35+(p.index%5)*.06)/4,(tile//4+.5)/4)
    for v in me.vertices:
        ws={bone:1} if isinstance(bone,str) else bone(v.co)
        for key,w in ws.items():
            if w>0:(o.vertex_groups.get(key) or o.vertex_groups.new(name=key)).add([v.index],w,'REPLACE')
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
    parts.append(o);return o

def rings(name,profile,tile,bone,n=24,arc=None):
    # Solid rolled shells, with open apertures rather than filled neck/arm holes.
    start,end=arc if arc else (0,math.tau);count=n+1 if arc else n
    vs=[]
    for inner in [False,True]:
        for x,y,z,rx,ry in profile:
            for i in range(count):
                a=start+(end-start)*i/n;d=.006 if inner else 0
                vs.append((x+(rx-d)*math.cos(a),y+(ry-d)*math.sin(a),z))
    fs=[];L=len(profile)*count
    for layer in range(2):
        for j in range(len(profile)-1):
            for i in range(n):
                a=layer*L+j*count+i;b=layer*L+j*count+(i+1)%count
                fs.append((a,b,b+count,a+count))
    for j in [0,len(profile)-1]:
        for i in range(n):
            a=j*count+i;b=j*count+(i+1)%count;fs.append((a,b,b+L,a+L))
    if arc:
        for i in [0,n]:
            for j in range(len(profile)-1):
                a=j*count+i;fs.append((a,a+count,a+count+L,a+L))
    return mesh(name,vs,fs,tile,bone)
def box(name,loc,size,tile,bone,bevel=.006):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.scale=size;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    mod=o.modifiers.new('Forged edge bevel','BEVEL');mod.width=bevel;mod.segments=2;bpy.ops.object.modifier_apply(modifier=mod.name)
    vs=[tuple(v.co) for v in o.data.vertices];fs=[tuple(p.vertices) for p in o.data.polygons];bpy.data.objects.remove(o,do_unlink=True);return mesh(name,vs,fs,tile,bone)
def rivet(x,y,z,bone):
    return box('Domed fastener',(x,y,z),(.013,.012,.013),12,bone,.004)
def torso(v):
    t=max(0,min(1,(v.z-1.08)/.17));return {'Spine':1-t,'Chest':t}
def join(name,objects):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    # All pieces deliberately use the same palette slot after joining.
    for p in o.data.polygons:p.material_index=0
    while len(o.data.materials)>1:o.data.materials.pop(index=1)
    return o
def export(path,objects,rig=None):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    if rig:rig.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_armature_deform_only=False)
report={};item_models={};review=[]
for female in [False,True]:
    key='Female' if female else 'Male'
    with bpy.data.libraries.load(str(ROOT/'ArtSource/Characters'/('Explorer'+key+'.blend')),link=False) as (a,b):b.objects=a.objects
    loaded=[o for o in b.objects if o]
    for o in loaded:bpy.context.collection.objects.link(o)
    rig=next(o for o in loaded if o.type=='ARMATURE');rig.animation_data_clear();rig.data.pose_position='REST'
    for im in bpy.data.images:
        if im.source=='FILE' and 'Skin' in im.name:im.filepath=str(ROOT/'Assets/RivetReach/Resources/Characters'/Path(im.filepath.replace('\\','/')).name)
    w=.153 if female else .175;c=.215 if female else .239;sets=[]
    for slot in ['Head','Chest','Legs','Feet']:
        parts=[]
        if slot=='Head':
            r=.125 if female else .137
            rings('Open face helmet',[(0,.017,1.685,r,.119),(0,.018,1.733,r+.005,.125),(0,.02,1.78,r*.83,.102),(0,.02,1.805,r*.48,.063),(0,.02,1.817,.008,.009)],15,'Head')
            rings('Rolled helmet brow',[(0,.017,1.682,r+.007,.126),(0,.017,1.697,r+.008,.127)],12,'Head')
            # Cheek/ear guards stop behind the eyes; open face retains the explorer identity.
            for s in [-1,1]:
                box('Temple guard',(s*(r-.004),.008,1.633),(.018,.119,.126),15,'Head',.014)
                for z in [1.611,1.662]:rivet(s*(r+.009),-.046,z,'Head')
            box('Brow badge',(0,-.115,1.712),(.044,.019,.036),12,'Head')
        elif slot=='Chest':
            rings('Tailored breastplate',[(0,0,1.058,w+.017,.145),(0,0,1.08,w+.024,.150),(0,0,1.16,w+.029,.150),(0,.004,1.27,c+.004,.160),(0,.004,1.345,c+.006,.157),(0,.004,1.398,c-.03,.135)],15,torso)
            rings('Rolled lower hem',[(0,0,1.058,w+.023,.151),(0,0,1.076,w+.029,.156)],12,torso)
            # Lower top edge at the sides and neck, with substantial straps over the shirt.
            for s in [-1,1]:
                box('Leather shoulder strap',(s*.132,.007,1.423),(.060,.225,.023),1,'Chest')
                box('Strap clasp',(s*.132,-.105,1.429),(.043,.02,.035),12,'Chest')
                for z,y in [(1.10,-.142),(1.23,-.16),(1.34,-.15)]:rivet(s*.095,y-.008,z,torso)
                shoulder=.225 if female else .254
                rings('Shoulder lame',[(s*shoulder,0,1.334,.094,.102),(s*shoulder,0,1.39,.106,.11),(s*shoulder,0,1.448,.075,.08),(s*shoulder,0,1.469,.035,.040),(s*shoulder,0,1.474,.007,.008)],15,'UpperArm'+('L' if s==1 else 'R'),16)
                wrist=shoulder+.072;side='L' if s==1 else 'R'
                rings('Leather bracer',[(s*wrist,-.031,.97,.047,.048),(s*(wrist-.012),-.025,1.07,.057,.058)],1,'Forearm'+side,16)
                rings('Bracer plate',[(s*wrist,-.031,.98,.054,.055),(s*(wrist-.009),-.027,1.055,.064,.066)],15,'Forearm'+side,16,(-math.pi,0))
        elif slot=='Legs':
            rings('Utility armor belt',[(0,0,.965,.204,.132),(0,0,1.01,.200,.132)],1,'Hips')
            box('Belt buckle',(0,-.139,.987),(.057,.022,.037),12,'Hips')
            for s in [-1,1]:
                side='L' if s==1 else 'R'
                # Front and lateral plates leave room for inward leg motion and cloth folds.
                for z,rx,ry,x in [(.86,.126,.127,.109),(.75,.119,.120,.115),(.64,.106,.112,.123)]:
                    rings('Overlapping thigh plate',[(s*x,0,z-.055,rx-.006,ry),(s*x,0,z+.050,rx,ry+.004)],15,'Thigh'+side,16,(-math.pi*.90,-math.pi*.10))
                    for dx in [-.055,.055]:rivet(s*x+dx,-ry-.002,z+.024,'Thigh'+side)
                rings('Knee guard',[(s*.126,-.018,.50,.091,.105),(s*.126,-.018,.55,.103,.117),(s*.126,-.018,.594,.090,.105)],12,lambda v,side=side:{'Thigh'+side:max(0,min(1,(v.z-.505)/.10)),'Shin'+side:1-max(0,min(1,(v.z-.505)/.10))},16,(-math.pi*.90,-math.pi*.10))
                rings('Shin plate',[(s*.139,0,.322,.096,.102),(s*.136,.003,.43,.101,.108),(s*.128,0,.50,.094,.108)],15,'Shin'+side,16,(-math.pi*.90,-math.pi*.10))
        else:
            for s in [-1,1]:
                side='L' if s==1 else 'R';x=s*.139
                rings('Armored toe',[(x,-.072,.045,.102,.17),(x,-.076,.087,.102,.166),(x,-.058,.142,.092,.131)],15,'Foot'+side,20)
                rings('Toe edge',[(x,-.072,.040,.105,.173),(x,-.072,.056,.105,.173)],12,'Foot'+side,20)
                rings('Ankle guard',[(x,0,.176,.09,.092),(x,0,.24,.094,.094)],15,'Shin'+side,16)
                box('Boot clasp',(x,-.099,.21),(.055,.022,.027),12,'Shin'+side)
        o=join(slot,parts);o.parent=rig;mod=o.modifiers.new('Explorer rig','ARMATURE');mod.object=rig;sets.append(o)
        o.data.calc_loop_triangles();report[key+slot]=len(o.data.loop_triangles)
        if not female:
            # Static inventory/held display is baked from precisely the worn mesh.
            item=o.copy();item.data=o.data.copy();bpy.context.collection.objects.link(item);item.parent=None;item.modifiers.clear();item.vertex_groups.clear();item.name=slot+'Item'
            pts=[v.co for v in item.data.vertices];lo=Vector(tuple(min(p[k] for p in pts) for k in range(3)));hi=Vector(tuple(max(p[k] for p in pts) for k in range(3)));centre=(lo+hi)/2;scale=.95/max(hi-lo)
            for v in item.data.vertices:v.co=(v.co-centre)*scale
            export(OUT/(slot+'Item.fbx'),[item]);item.hide_render=True;item_models[slot]=item
    export(OUT/(key+'Armor.fbx'),sets,rig)
    for o in loaded:
        if o.type=='MESH':
            # Helmet replaces hair only in this disposable source review copy.
            import bmesh
            bm=bmesh.new();bm.from_mesh(o.data);uv=bm.loops.layers.uv.active
            if uv:
                remove=[f for f in bm.faces if all(int(l[uv].uv.x*4)+int(l[uv].uv.y*4)*4==9 for l in f.loops) and any(v.co.z>1.678 for v in f.verts)]
                bmesh.ops.delete(bm,geom=remove,context='FACES');bm.to_mesh(o.data)
            bm.free()
    rig.data.pose_position='POSE';rig.location.x=-.44 if not female else .44
    for o in sets:o.data.materials[0]=mats['Copper' if not female else 'Iron']
    review.extend(loaded+sets)
    # Save fitted sources with the original character available as a fit reference.
    # Combined editable source saved below contains both fitted variants.
# A tapered cast bar, rolled edges and recessed maker's stamp.
parts=[]
vs=[]
for z,x,y in [(-.17,.49,.26),(.14,.42,.205)]:vs.extend([(-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)])
o=mesh('Cast ingot',vs,[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],15,'Root')
bpy.context.view_layer.objects.active=o;o.select_set(True);mod=o.modifiers.new('Soft cast edges','BEVEL');mod.width=.035;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
box('Inset hallmark',(0,0,.142),(.21,.115,.008),3,'Root',.015)
for x in [-.055,0,.055]:box('Foundry stamp',(x,0,.148),(.018,.071,.006),12,'Root',.004)
ingot=join('Ingot',parts);export(OUT/'Ingot.fbx',[ingot]);item_models['Ingot']=ingot
for o in review:o.hide_render=True
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.render.film_transparent=True;scene.view_settings.view_transform='AgX'
scene.world=bpy.data.worlds.new('Equipment studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.35,.40,.48,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.6
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((-3,-4,5),500,4),((3,-1,3),280,3),((1,3,4),450,3)]:
    d=bpy.data.lights.new('Softbox','AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new('Softbox',d);scene.collection.objects.link(o);o.location=pos;aim(o,(0,0,1))
d=bpy.data.cameras.new('Review');cam=bpy.data.objects.new('Review',d);scene.collection.objects.link(cam);scene.camera=cam;d.type='ORTHO';d.ortho_scale=1.3;cam.location=(1,-2,1.1);aim(cam,(0,0,0));scene.render.resolution_x=scene.render.resolution_y=256
for key,o in item_models.items():
    for other in item_models.values():other.hide_render=other!=o
    for tier in (['Copper','Iron','Gold'] if key=='Ingot' else ['Copper','Iron','Diamond']):
        o.data.materials[0]=mats[tier];scene.render.filepath=str(OUT/(tier+key+'Icon.png'));bpy.ops.render.render(write_still=True)
for o in item_models.values():o.hide_render=True
for o in review:o.hide_render=False
scene.render.resolution_x=1400;scene.render.resolution_y=1200;scene.render.film_transparent=False;d.ortho_scale=2.25
for label,pos in [('front',(.5,-6,2.3)),('back',(.5,6,2.3))]:
    cam.location=pos;aim(cam,(0,0,.94));scene.render.filepath=str(SRC/('armor-'+label+'.png'));bpy.ops.render.render(write_still=True)
for im in bpy.data.images:
    if im.source=='FILE':im.filepath=bpy.path.relpath(im.filepath,start=str(SRC))
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Equipment.blend'))
(SRC/'geometry.json').write_text(json.dumps(report,indent=2));print('ARMOR_ART_COMPLETE',report,flush=True)
