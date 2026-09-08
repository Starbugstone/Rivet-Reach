"""Rebake the current animation library while preserving existing player mesh/skin sources.
Run with Blender --background --python <this file>. Copyright (c) 2026 Starbugstone.
"""
import bpy, json, sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.dont_write_bytecode=True
sys.path.insert(0,str(root/'Tools'))
from create_player_assets import animate
report=json.loads((root/'ArtSource/Characters/asset-report.json').read_text())
for entry in report:
    name=entry['model'];source=root/'ArtSource/Characters'/(name+'.blend')
    bpy.ops.wm.open_mainfile(filepath=str(source))
    rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
    model=next(o for o in bpy.data.objects if o.type=='MESH')
    rig.animation_data_clear()
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    entry['clips']=animate(rig,abs(rig.data.bones['UpperArmL'].head_local.x),name=='ExplorerFemale')
    entry['bones']=len(rig.data.bones)
    entry['deformBones']=sum(b.use_deform for b in rig.data.bones)
    entry['sockets']=['BlockSocket','ToolSocket']
    rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);model.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    bpy.ops.export_scene.fbx(filepath=str(root/'Assets/RivetReach/Resources/Characters'/(name+'.fbx')),
        use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,
        axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_armature_deform_only=False)
(root/'ArtSource/Characters/asset-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('INTERACTION_ANIMATION_EXPORT_PASS')
