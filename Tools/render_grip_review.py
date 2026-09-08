"""Render real skinned grip contacts and record wrist alignment, without changing assets."""
import bpy,bmesh,math,json,sys
from pathlib import Path
from mathutils import Vector,Matrix
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Characters/ExplorerMale.blend'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');model=next(o for o in bpy.data.objects if o.type=='MESH')
if '--draft' in sys.argv:
 sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'Tools'))
 from create_player_assets import animate
 rig.animation_data.action=None;bpy.data.actions.remove(bpy.data.actions['FP_HoldTwoHandTool'])
 animate(rig,.254,False,only=['FP_HoldTwoHandTool'])
left=model.copy();left.data=model.data.copy();bpy.context.collection.objects.link(left)
for arm,side in [(model,'R'),(left,'L')]:
 allowed=[g.index for g in arm.vertex_groups if g.name.endswith(side) and any(n in g.name for n in ['Arm','Forearm','Hand','Finger','Thumb'])]
 remove=[v.index for v in arm.data.vertices if sum(g.weight for g in v.groups if g.group in allowed)<.5]
 bm=bmesh.new();bm.from_mesh(arm.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[bm.verts[i] for i in remove],context='VERTS');bm.to_mesh(arm.data);bm.free()
props={}
for name in ['GripSword','GripPickaxe']:
 with bpy.data.libraries.load(str(root/'ArtSource/Characters'/(name+'.blend'))) as (src,dst):dst.objects=[name]
 ob=dst.objects[0];bpy.context.collection.objects.link(ob);props[name]=ob
bpy.ops.mesh.primitive_cube_add(size=.14);cube=bpy.context.object;cube.name='Palm supported cube'
mat=bpy.data.materials.new('Clay block');mat.diffuse_color=(.22,.37,.14,1);cube.data.materials.append(mat)
world=bpy.data.worlds.new('Neutral grip studio');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.20,.27,.31,1);world.node_tree.nodes['Background'].inputs[1].default_value=.8;bpy.context.scene.world=world
for loc,power,size in [((-2,-3,4),450,3),((3,0,3),220,3)]:
 l=bpy.data.lights.new('Softbox','AREA');l.energy=power;l.size=size;o=bpy.data.objects.new('Softbox',l);bpy.context.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,-.3,1.2))-o.location).to_track_quat('-Z','Y').to_euler()
c=bpy.data.cameras.new('Review camera');cam=bpy.data.objects.new('Review camera',c);bpy.context.collection.objects.link(cam)
scene=bpy.context.scene;scene.camera=cam;scene.render.engine='CYCLES';scene.cycles.samples=20;scene.cycles.use_denoising=True;scene.render.resolution_x=1280;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX'
if '--low-memory' in sys.argv:
 scene.render.threads_mode='FIXED';scene.render.threads=2;scene.cycles.use_denoising=False;scene.cycles.samples=48
if '--denoise' in sys.argv:scene.cycles.use_denoising=True
results=[]
for action,prop in [('FP_Idle',None),('FP_HoldBlock',cube),('FP_HoldTool',props['GripSword']),('FP_HoldTwoHandTool',props['GripPickaxe'])]:
 if '--idle-only' in sys.argv and action!='FP_Idle':continue
 if '--twohand' in sys.argv and 'TwoHandTool' not in action:continue
 rig.animation_data.action=bpy.data.actions[action];scene.frame_set(1);left.hide_render='TwoHandTool' not in action
 for o in [cube,*props.values()]:o.hide_render=o!=prop
 if prop:
  socket=rig.pose.bones['BlockSocket' if prop==cube else 'ToolSocket'].matrix
  prop.matrix_world=socket @ Matrix.Rotation(-math.pi/2,4,'X')
 h=rig.pose.bones['HandR'];f=rig.pose.bones['ForearmR']
 results.append({'clip':action,'wristDegrees':math.degrees((h.tail-h.head).angle(f.tail-f.head)),'palmUp':h.matrix.to_3x3().col[2].z})
 for close in [False,True]:
  if '--close-only' in sys.argv and not close:continue
  if close:
   target=h.head+h.matrix.to_3x3() @ Vector((0,.06,.02));cam.location=target+Vector((-.34,-.35,.20));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();c.lens=55;c.sensor_fit='AUTO'
  else:
   cam.location=(.12 if 'TwoHandTool' in action else 0,.02,1.5);cam.rotation_euler=Vector((0,-1,0)).to_track_quat('-Z','Y').to_euler();c.lens=24;c.sensor_fit='VERTICAL';c.sensor_height=36
  scene.render.filepath=str(root/'Logs'/('grip-'+action+('-contact' if close else '-view')+'.png'));bpy.ops.render.render(write_still=True)
(root/'Logs/grip-source-measurements.json').write_text(json.dumps(results,indent=2))
