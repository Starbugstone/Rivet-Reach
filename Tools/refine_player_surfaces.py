"""Continuous skinned hands, anatomical detail and material coordinates.

Original Rivet Reach artwork, copyright (c) 2026 Starbugstone. See LICENSE.md.
Run in Blender 5.2 background mode. Preserves the body, rig, actions and GUIDs.
The base generator also calls refine() so a clean regeneration retains this pass.
"""
import bpy, bmesh, math, json, sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parents[1]

def activate(obj):
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
    bpy.context.view_layer.objects.active=obj

def retain(obj, indices):
    bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index not in indices],context='VERTS')
    bm.to_mesh(obj.data);bm.free()

def refine(rig, model):
    if model.get('continuous_hands_version')==1:return
    pose=rig.data.pose_position;rig.data.pose_position='REST'
    names={g.index:g.name for g in model.vertex_groups}
    hands=[];remove=set();measurements=[]
    for side in ['L','R']:
        allowed={i for i,n in names.items() if n.endswith(side) and n.startswith(('Forearm','Hand','Finger','Thumb'))}
        # Skin semantics exclude the linen sleeve even where it shares forearm weights.
        skin_faces=[p for p in model.data.polygons if int(model.data.uv_layers.active.data[p.loop_start].uv.y*4)==1
                    and int(model.data.uv_layers.active.data[p.loop_start].uv.x*4)==(0 if side=='L' else 1)]
        indices={i for p in skin_faces for i in p.vertices if any(g.group in allowed for g in model.data.vertices[i].groups)}
        remove.update(indices)
        source=model.copy();source.data=model.data.copy();source.modifiers.clear();source.parent=None
        bpy.context.collection.objects.link(source);retain(source,indices)
        hand=source.copy();hand.data=source.data.copy();bpy.context.collection.objects.link(hand);activate(hand)
        hand.name='Continuous forearm palm and five fingers '+side
        # Existing root caps merely touch. Extend the roots into the palm before union.
        for v in hand.data.vertices:
            if .859<v.co.z<.878 and abs(v.co.y+.039)<.03:
                v.co.z+=.0035*max(0,(v.co.z-.859)/.019)
        mod=hand.modifiers.new('Watertight skin union','REMESH');mod.mode='VOXEL';mod.voxel_size=.0011
        mod.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=hand.modifiers.new('Relax anatomical joins','SMOOTH');mod.factor=.65;mod.iterations=5
        bpy.ops.object.modifier_apply(modifier=mod.name)
        # Retopology reduces the voxel sculpt to a continuous quad cage before subdivision.
        bpy.ops.object.quadriflow_remesh(target_faces=2600,use_mesh_symmetry=False,use_preserve_sharp=False,use_preserve_boundary=False,seed=17)
        bm=bmesh.new();bm.from_mesh(hand.data)
        boundary=[e for e in bm.edges if e.is_boundary]
        if boundary:bmesh.ops.holes_fill(bm,edges=boundary,sides=0)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(hand.data);bm.free()
        mod=hand.modifiers.new('Silhouette subdivision','SUBSURF');mod.levels=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
        hand.vertex_groups.clear()
        mod=hand.modifiers.new('Transfer anatomical skinning','DATA_TRANSFER');mod.object=source
        mod.use_vert_data=True;mod.data_types_verts={'VGROUP_WEIGHTS'};mod.vert_mapping='POLYINTERP_NEAREST'
        bpy.ops.object.datalayout_transfer(modifier=mod.name)
        bpy.ops.object.modifier_apply(modifier=mod.name)
        bpy.ops.object.mode_set(mode='WEIGHT_PAINT')
        bpy.ops.object.vertex_group_smooth(group_select_mode='ALL',factor=.6,repeat=6)
        bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL',limit=4)
        bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL',lock_active=False)
        bpy.ops.object.mode_set(mode='OBJECT')
        # Cylindrical forearm coordinates continue into the hand without per-face palette jumps.
        for layer in list(hand.data.uv_layers):hand.data.uv_layers.remove(layer)
        uv=hand.data.uv_layers.new(name='SkinUV');wx=rig.data.bones['Hand'+side].head_local.x
        for p in hand.data.polygons:
            p.use_smooth=True
            for li in p.loop_indices:
                v=hand.data.vertices[hand.data.loops[li].vertex_index].co
                u=.5+math.atan2(v.y+.035,v.x-wx)/math.tau*.88
                uv.data[li].uv=((int(side=='R')+u)/4,(1+.13+(v.z-.79)/.4*.75)/4)
        bm=bmesh.new();bm.from_mesh(hand.data)
        components=[];unseen=set(bm.verts)
        while unseen:
            stack=[unseen.pop()];size=0
            while stack:
                v=stack.pop();size+=1
                for e in v.link_edges:
                    other=e.other_vert(v)
                    if other in unseen:unseen.remove(other);stack.append(other)
            components.append(size)
        assert len(components)==1,(side,'skin is disconnected',components)
        assert all(e.is_manifold for e in bm.edges),(side,'open skin boundary')
        bm.free()
        hand.data.calc_loop_triangles()
        measurements.append({'side':side,'skinTriangles':len(hand.data.loop_triangles),'connectedComponents':len(components),'closedManifold':True})
        hands.append(hand);bpy.data.objects.remove(source,do_unlink=True)
    retain(model,set(range(len(model.data.vertices)))-remove)
    activate(model)
    for hand in hands:hand.select_set(True)
    bpy.ops.object.join()
    model['continuous_hands_version']=1
    model['hand_topology_report']=json.dumps(measurements)
    rig.data.pose_position=pose
    print('CONTINUOUS_HANDS '+json.dumps(measurements),flush=True)

def run():
    report=json.loads((ROOT/'ArtSource/Characters/asset-report.json').read_text())
    for entry in report:
        name=entry['model']
        if '--male-only' in sys.argv and name!='ExplorerMale':continue
        source=ROOT/'ArtSource/Characters'/(name+'.blend')
        bpy.ops.wm.open_mainfile(filepath=str(source))
        rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
        model=next(o for o in bpy.data.objects if o.type=='MESH')
        refine(rig,model)
        model.data.calc_loop_triangles();entry['triangles']=len(model.data.loop_triangles)
        entry['hands']=json.loads(model['hand_topology_report'])
        activate(rig);model.select_set(True)
        rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
        bpy.ops.wm.save_as_mainfile(filepath=str(source))
        bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/RivetReach/Resources/Characters'/(name+'.fbx')),
            use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,
            bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,
            axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_armature_deform_only=False)
    (ROOT/'ArtSource/Characters/asset-report.json').write_text(json.dumps(report,indent=2)+'\n')

if __name__=='__main__':run()
