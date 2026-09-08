"""Distal finger joints, skinned nail plates and studio PBR material setup.
Original artwork, copyright (c) 2026 Starbugstone. See LICENSE.md.
"""
import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
ROOT=Path(__file__).resolve().parents[1]
sys.dont_write_bytecode=True;sys.path.insert(0,str(ROOT/'Tools'))
from refine_player_surfaces import activate,retain

def polish(rig,model):
    if model.get('distal_joints_version')==1:return
    rig.data.pose_position='REST';activate(rig)
    bpy.ops.object.mode_set(mode='EDIT')
    for side in ['L','R']:
        for finger in range(4):
            b=rig.data.edit_bones['Finger%dB%s'%(finger,side)];tip=b.tail.copy();joint=b.head.lerp(tip,.58)
            c=rig.data.edit_bones.new('Finger%dC%s'%(finger,side));c.head=joint;c.tail=tip;c.parent=b;c.use_connect=True;c.roll=b.roll;b.tail=joint
    bpy.ops.object.mode_set(mode='OBJECT')
    # Split existing distal weights smoothly across the newly articulated DIP joint.
    for side in ['L','R']:
        for finger in range(4):
            name='Finger%dB%s'%(finger,side);old=model.vertex_groups[name];tip=rig.data.bones[name.replace('B','C')]
            group=model.vertex_groups.new(name=tip.name);axis=(tip.tail_local-tip.head_local).normalized()
            for v in model.data.vertices:
                weight=next((g.weight for g in v.groups if g.group==old.index),0)
                if weight<=0:continue
                t=max(0,min(1,((v.co-tip.head_local).dot(axis)+.005)/.010));t=t*t*(3-2*t)
                old.add([v.index],weight*(1-t),'REPLACE');group.add([v.index],weight*t,'REPLACE')
    # Shallow dorsal creases and tendon contours are continuous with the skin mesh.
    names={g.index:g.name for g in model.vertex_groups}
    for v in model.data.vertices:
        if not v.groups:continue
        dominant=names[max(v.groups,key=lambda g:g.weight).group]
        if not dominant.startswith(('Finger','Hand')):continue
        side=dominant[-1];normal=-rig.data.bones['Hand'+side].matrix_local.to_3x3().col[2]
        if v.normal.dot(normal)<.35:continue
        if dominant.startswith('Finger'):
            finger=int(dominant[6]);b=rig.data.bones['Finger%dB%s'%(finger,side)]
            c=rig.data.bones['Finger%dC%s'%(finger,side)]
            distance=min(abs(v.co.z-b.head_local.z),abs(v.co.z-c.head_local.z))
            relief=-.00035*math.exp(-(distance/.0014)**2)+.0003*math.exp(-(distance/.006)**2)
        else:
            hand=rig.data.bones['Hand'+side];x=v.co.x-hand.head_local.x
            relief=.0005*math.exp(-((v.co.z-.905)/.025)**2)*sum(math.exp(-((x-offset)/.0025)**2) for offset in [-.023,0,.023])
        v.co+=normal*relief
    model.data.update()
    renew_nails(rig,model)
    model['distal_joints_version']=1;rig.data.pose_position='POSE'

def renew_nails(rig,model):
    rig.data.pose_position='REST'
    old={i for p in model.data.polygons for i in p.vertices
         if .258<model.data.uv_layers.active.data[p.loop_start].uv.y<.26
         and (model.data.uv_layers.active.data[p.loop_start].uv.x*4)%1<.05}
    retain(model,set(range(len(model.data.vertices)))-old)
    names={g.index:g.name for g in model.vertex_groups}
    model.data.calc_loop_triangles();triangles=[tuple(t.vertices) for t in model.data.loop_triangles]
    tree=BVHTree.FromPolygons([v.co for v in model.data.vertices],triangles,all_triangles=True)
    nails=[]
    for side in ['L','R']:
        for digit in range(5):
            bone=rig.data.bones['Finger%dC%s'%(digit,side) if digit<4 else 'ThumbB'+side]
            direction=(bone.tail_local-bone.head_local).normalized()
            normal=-bone.matrix_local.to_3x3().col[2];across=direction.cross(normal).normalized()
            centre=bone.head_local.lerp(bone.tail_local,.53 if digit<4 else .64)
            half_length=bone.length*(.37 if digit<4 else .20);width=.0058 if digit<4 else .006
            verts=[];faces=[];weights=[];rings=4;sides=20
            for ring in range(rings):
                radius=max(.001,ring/(rings-1))
                for i in range(sides):
                    angle=math.tau*i/sides
                    point=centre+direction*(math.sin(angle)*half_length*radius)+across*(math.cos(angle)*width*radius)
                    location,n,index,dist=tree.ray_cast(point+normal*.04,-normal,.08)
                    if location is None:location=point+normal*.007;n=normal
                    verts.append(location+n*(.0003+.00015*(1-radius*radius)))
                    # Match the underlying skin's weights, preserving nail contact through bends.
                    ws={}
                    if index is not None:
                        triangle=[model.data.vertices[j] for j in triangles[index]]
                        bary=barycentric_transform(location,*(v.co for v in triangle),Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1)))
                        for vertex,factor in zip(triangle,bary):
                            for g in vertex.groups:ws[names[g.group]]=ws.get(names[g.group],0)+g.weight*max(0,factor)
                    else:ws={bone.name:1}
                    weights.append(ws)
            faces.append(tuple(reversed(range(sides))))
            for j in range(rings-1):
                for i in range(sides):faces.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i))
            mesh=bpy.data.meshes.new('Nail plate');mesh.from_pydata(verts,[],[tuple(reversed(f)) for f in faces]);mesh.update()
            nail=bpy.data.objects.new('Nail '+str(digit)+side,mesh);bpy.context.collection.objects.link(nail)
            uv=mesh.uv_layers.new(name='SkinUV')
            for p in mesh.polygons:
                p.use_smooth=True
                for li in p.loop_indices:uv.data[li].uv=((int(side=='R')+.035)/4,1.035/4)
            for i,ws in enumerate(weights):
                for name,w in ws.items():
                    g=nail.vertex_groups.get(name) or nail.vertex_groups.new(name=name);g.add([i],w,'REPLACE')
            nails.append(nail)
    activate(model)
    for nail in nails:nail.select_set(True)
    bpy.ops.object.join()
    bpy.ops.object.mode_set(mode='WEIGHT_PAINT');bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL',limit=4)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL',lock_active=False);bpy.ops.object.mode_set(mode='OBJECT')
    rig.data.pose_position='POSE'

def material(model):
    # Shared coordinates remove repeated per-polygon colour bands across the face/neck.
    uv=model.data.uv_layers.active
    for p in model.data.polygons:
        tile=uv.data[p.loop_start].uv
        if int(tile.x*4)!=0 or int(tile.y*4)!=0:continue
        for li in p.loop_indices:
            v=model.data.vertices[model.data.loops[li].vertex_index].co
            uv.data[li].uv=((.5+math.atan2(v.y-.012,v.x)/math.tau*.88)/4,
                max(.08,min(.92,.12+(v.z-1.39)/.41*.76))/4)
    mat=model.data.materials[0];model.data.materials.clear();model.data.materials.append(mat)
    for p in model.data.polygons:p.material_index=0
    mat.use_nodes=True;nodes=mat.node_tree.nodes;links=mat.node_tree.links
    for node in list(nodes):
        if node.name in ['SkinSurface','SkinNormal'] or node.name.startswith('Hifi') or node.type in ['SEPARATE_COLOR','NORMAL_MAP']:nodes.remove(node)
    bsdf=nodes.get('Principled BSDF')
    for name in ['SkinSurface','SkinNormal']:
        texture=nodes.get(name) or nodes.new('ShaderNodeTexImage');texture.name=name
        texture.image=bpy.data.images.load(str(ROOT/'Assets/RivetReach/Resources/Characters'/(name+'.png')),check_existing=True)
        texture.image.colorspace_settings.name='Non-Color';texture.image.filepath='../../Assets/RivetReach/Resources/Characters/'+name+'.png'
        texture.image.filepath='//'+texture.image.filepath
        if name=='SkinSurface':
            split=nodes.new('ShaderNodeSeparateColor');links.new(texture.outputs['Color'],split.inputs[0]);links.new(split.outputs['Green'],bsdf.inputs['Roughness']);links.new(split.outputs['Red'],bsdf.inputs['Metallic'])
        else:
            coordinates=nodes.new('ShaderNodeTexCoord');coordinates.name='Hifi coordinates'
            separate=nodes.new('ShaderNodeSeparateXYZ');separate.name='Hifi UV';links.new(coordinates.outputs['UV'],separate.inputs[0])
            tests=[]
            for axis,operation,value in [('Y','GREATER_THAN',.25),('Y','LESS_THAN',.5),('X','LESS_THAN',.5)]:
                node=nodes.new('ShaderNodeMath');node.name='Hifi hand region';node.operation=operation;node.inputs[1].default_value=value;links.new(separate.outputs[axis],node.inputs[0]);tests.append(node)
            for a,b in [(tests[0],tests[1])]:
                mask=nodes.new('ShaderNodeMath');mask.name='Hifi vertical mask';mask.operation='MULTIPLY';links.new(a.outputs[0],mask.inputs[0]);links.new(b.outputs[0],mask.inputs[1])
            final=nodes.new('ShaderNodeMath');final.name='Hifi hand mask';final.operation='MULTIPLY';links.new(mask.outputs[0],final.inputs[0]);links.new(tests[2].outputs[0],final.inputs[1])
            mix=nodes.new('ShaderNodeMixRGB');mix.name='Hifi continuous normal';mix.inputs[1].default_value=(.5,.5,1,1);links.new(final.outputs[0],mix.inputs[0]);links.new(texture.outputs['Color'],mix.inputs[2])
            normal=nodes.new('ShaderNodeNormalMap');links.new(mix.outputs[0],normal.inputs['Color']);links.new(normal.outputs['Normal'],bsdf.inputs['Normal'])
    for node in nodes:
        if node.type=='TEX_IMAGE' and node.image and 'SkinField' in node.image.name:node.image.reload()

def run():
    from create_player_assets import animate
    report=json.loads((ROOT/'ArtSource/Characters/asset-report.json').read_text())
    for entry in report:
        name=entry['model'];source=ROOT/'ArtSource/Characters'/(name+'.blend')
        bpy.ops.wm.open_mainfile(filepath=str(source));rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');model=next(o for o in bpy.data.objects if o.type=='MESH')
        polish(rig,model)
        if '--renew-nails' in sys.argv:renew_nails(rig,model)
        material(model)
        if '--materials-only' not in sys.argv:
            rig.animation_data_clear()
            for action in list(bpy.data.actions):bpy.data.actions.remove(action)
            entry['clips']=animate(rig,abs(rig.data.bones['UpperArmL'].head_local.x),name=='ExplorerFemale')
        entry['bones']=len(rig.data.bones);entry['deformBones']=sum(b.use_deform for b in rig.data.bones)
        entry['materials']=len(model.data.materials)
        entry['maxInfluences']=max(sum(g.weight>1e-6 for g in v.groups) for v in model.data.vertices)
        model.data.calc_loop_triangles();entry['triangles']=len(model.data.loop_triangles)
        rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
        activate(rig);model.select_set(True);bpy.ops.wm.save_as_mainfile(filepath=str(source))
        bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/RivetReach/Resources/Characters'/(name+'.fbx')),
            use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,
            bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_armature_deform_only=False)
    (ROOT/'ArtSource/Characters/asset-report.json').write_text(json.dumps(report,indent=2)+'\n');print('RIG_POLISH_PASS',flush=True)

if __name__=='__main__':run()
