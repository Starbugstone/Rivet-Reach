"""Render the actual exported-source models together for art-direction review in Blender."""
import bpy, math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for name,x in [('ExplorerMale',-.48),('ExplorerFemale',.48)]:
    source=root/'ArtSource/Characters'/(name+'.blend')
    with bpy.data.libraries.load(str(source),link=False) as (a,b):b.objects=a.objects
    for o in b.objects:
        if o is not None:
            bpy.context.collection.objects.link(o)
            if o.parent is None:o.location.x+=x
    for image in bpy.data.images:
        if image.source=='FILE':image.filepath=str(root/'Assets/RivetReach/Resources/Characters/SkinField.png')
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.005))
mat=bpy.data.materials.new('Warm studio floor');mat.diffuse_color=(.34,.36,.34,1);bpy.context.object.data.materials.append(mat)
world=bpy.data.worlds.new('Review daylight');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.64,.71,.8,1);world.node_tree.nodes['Background'].inputs[1].default_value=.4;bpy.context.scene.world=world
for name,loc,power,size in [('Key',(-3,-4,5),500,4),('Fill',(3,-2,3),180,3),('Rim',(1,3,4),400,3)]:
    light=bpy.data.lights.new(name,'AREA');light.energy=power;light.shape='DISK';light.size=size;o=bpy.data.objects.new(name,light);bpy.context.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
cam=bpy.data.cameras.new('Review camera');o=bpy.data.objects.new('Review camera',cam);bpy.context.collection.objects.link(o);o.location=(.55,-6,2.35);o.rotation_euler=(Vector((0,0,.96))-o.location).to_track_quat('-Z','Y').to_euler();cam.type='ORTHO';cam.ortho_scale=2.3;bpy.context.scene.camera=o
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True;scene.render.resolution_x=1400;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(root/'Logs/player-v4-front.png');bpy.ops.render.render(write_still=True)

for rig in [obj for obj in bpy.data.objects if obj.type=='ARMATURE']:rig.location.x*=-1
o.location=(.55,6,2.35);o.rotation_euler=(Vector((0,0,.96))-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(root/'Logs/player-v4-back.png');bpy.ops.render.render(write_still=True)

# Face and joint detail from the same source scene, not a concept illustration.

for rig in [obj for obj in bpy.data.objects if obj.type=='ARMATURE']:rig.location.x*=-1
o.location=(.30,-5,1.95);o.rotation_euler=(Vector((0,0,1.60))-o.location).to_track_quat('-Z','Y').to_euler();cam.ortho_scale=1.38
scene.render.resolution_x=1600;scene.render.resolution_y=850
scene.render.filepath=str(root/'Logs/player-v4-detail.png');bpy.ops.render.render(write_still=True)
