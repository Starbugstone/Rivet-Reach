"""Inspect the actual skinned first-person pose at the runtime camera offset."""
import bpy, bmesh
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Characters/ExplorerMale.blend'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
model=next(o for o in bpy.data.objects if o.type=='MESH')
allowed=[g.index for g in model.vertex_groups if any(n in g.name for n in ['Arm','Forearm','Hand','Finger','Thumb'])]
remove=[v.index for v in model.data.vertices if not any(g.group in allowed and g.weight>.5 for g in v.groups)]
bm=bmesh.new();bm.from_mesh(model.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[bm.verts[i] for i in remove],context='VERTS');bm.to_mesh(model.data);bm.free()
world=bpy.data.worlds.new('Neutral environment');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.22,.3,.36,1);world.node_tree.nodes['Background'].inputs[1].default_value=.7;bpy.context.scene.world=world
for loc,power,size in [((-2,-3,4),450,3),((3,0,3),220,3)]:
 l=bpy.data.lights.new('Softbox','AREA');l.energy=power;l.size=size;o=bpy.data.objects.new('Softbox',l);bpy.context.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,-.3,1.2))-o.location).to_track_quat('-Z','Y').to_euler()
c=bpy.data.cameras.new('Player camera');o=bpy.data.objects.new('Player camera',c);bpy.context.collection.objects.link(o);o.location=(0,.02,1.5);o.rotation_euler=(Vector((0,-1,0))).to_track_quat('-Z','Y').to_euler();c.lens=24;c.sensor_fit='VERTICAL';c.sensor_height=36
scene=bpy.context.scene;scene.camera=o;scene.render.engine='CYCLES';scene.cycles.samples=20;scene.cycles.use_denoising=True;scene.render.resolution_x=1280;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX'
for action,frame,label in [('FP_Idle',1,'idle'),('FP_Mine',5,'strike')]:
 rig.animation_data.action=bpy.data.actions[action];scene.frame_set(frame)
 scene.render.filepath=str(root/('Logs/player-v3-hands-'+label+'.png'));bpy.ops.render.render(write_still=True)
