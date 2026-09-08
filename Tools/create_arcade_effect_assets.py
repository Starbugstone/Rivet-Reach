"""Original Rivet Reach arcade debris kit; Blender 5.2 background authoring.
Copyright (c) 2026 Starbugstone. All rights reserved. No third-party assets.
"""
import bpy, bmesh, json, math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'ArtSource/Effects';OUT=ROOT/'Assets/RivetReach/Resources/Effects'
SOURCE.mkdir(parents=True,exist_ok=True);OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
materials=[]
for name,colour in [('Stone',(.34,.46,.55,1)),('Wood',(.48,.24,.07,1)),('Leaf',(.21,.48,.10,1))]:
 m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=colour
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=colour;p.inputs['Roughness'].default_value=.62;materials.append(m)
objects=[]
for name,scale,index in [('StoneChip',(.40,.31,.30),0),('WoodSliver',(.13,.16,.46),1)]:
 bpy.ops.mesh.primitive_cube_add(size=2);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 for v in o.data.vertices:
  if v.co.z>0:v.co.x+=.08;v.co.y*=.83
 mod=o.modifiers.new('Rounded chip edges','BEVEL');mod.width=.055 if index==0 else .023;mod.segments=1
 bpy.ops.object.modifier_apply(modifier=mod.name);o.data.materials.append(materials[index]);objects.append(o)
vertices=[];faces=[]
for j in range(9):
 t=j/8;z=t-.5;width=math.sin(math.pi*t)*.25
 for k in [-1,0,1]:vertices.append((k*width,.13*math.sin(t*math.pi)+abs(k)*.09*math.sin(t*math.pi),z))
for j in range(8):
 for k in range(2):a=j*3+k;faces.append((a,a+1,a+4,a+3))
mesh=bpy.data.meshes.new('Curled leaf');mesh.from_pydata(vertices,[],faces);mesh.update()
o=bpy.data.objects.new('LeafChip',mesh);bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True)
mod=o.modifiers.new('Leaf thickness','SOLIDIFY');mod.thickness=.012;bpy.ops.object.modifier_apply(modifier=mod.name)
o.data.materials.append(materials[2]);objects.append(o)
report=[]
for index,o in enumerate(objects):
 o.location.x=(index-1)*1.1
 o.data.calc_loop_triangles();report.append({'mesh':o.name,'triangles':len(o.data.loop_triangles),'materials':len(o.data.materials)})
 # All asset pieces are ordinary meshes with explicit UVs and no rig/collision.
 if not o.data.uv_layers:o.data.uv_layers.new(name='UVMap')
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'ArcadeChips.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'ArcadeChips.fbx'),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
(SOURCE/'asset-report.json').write_text(json.dumps(report,indent=2)+'\n')
# Actual source review, separate from the editable asset scene.
for o in objects:o.rotation_euler=(math.radians(25),math.radians(-22),math.radians(-15))
bpy.ops.object.camera_add(location=(2.9,-4.7,2.5));camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,0))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=3.9;scene.camera=camera
for loc,power,size in [((0,-3,4),550,4),((-3,1,2),350,3)]:
 bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=size;light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
scene.world=bpy.data.worlds.new('Neutral review');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.19,.24,.28,1)
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True;scene.render.threads_mode='FIXED';scene.render.threads=2
scene.render.resolution_x=1200;scene.render.resolution_y=600;scene.render.resolution_percentage=100;scene.render.filepath=str(ROOT/'Logs/arcade-debris-blender.png')
bpy.ops.render.render(write_still=True)
print('ARCADE_DEBRIS_PASS',json.dumps(report),flush=True)
