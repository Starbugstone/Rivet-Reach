"""Original Rivet Reach axe; authored mesh, pixel palette and reproducible FBX.
Copyright (c) 2026 Starbugstone. All rights reserved.
Run with Blender 5.2 --background --python Tools/create_starter_axe.py.
"""
import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector
from mathutils.geometry import tessellate_polygon
root=Path(__file__).resolve().parents[1]
out=root/'Assets/RivetReach/Resources/Tools';source=root/'ArtSource/Tools'
out.mkdir(parents=True,exist_ok=True);source.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC'
# A deliberately limited original palette, with clustered pixels rather than photo noise.
size=128;pixels=[]
def h(x,y):return ((x*73856093)^(y*19349663))%997/996
for y in range(size):
 for x in range(size):
  if x<32:
   grain=(math.sin(x*.72+math.sin(y*.12)*.5)+1)*.5
   base=(.29,.145,.060);light=(.57,.335,.145);t=.25+grain*.50+h(x//3,y//9)*.16
   c=[a+(b-a)*t for a,b in zip(base,light)]
   if x%11==3 and y%29<18:c=[v*.68 for v in c]
  elif x<96:
   u=(x-32)/64;v=y/128;facet=.17+.4*v+.18*h(x//9,y//13)
   if v>.65-u*.4:facet+=.13
   c=[a+(b-a)*facet for a,b in zip((.16,.215,.24),(.43,.51,.55))]
   if (x+y//2)%37==0 and 29<y%59<43:c=[v*1.25 for v in c]
   if x>=85:c=[a+(b-a)*(x-85)/10 for a,b in zip((.54,.63,.68),(.82,.87,.87))]
  elif x<112:
   shade=.8+h(x//2,y//5)*.28;c=[.235*shade,.12*shade,.072*shade]
   if y%13<2:c=[v*1.5 for v in c]
  else:
   shade=.76+h(x//3,y//7)*.25;c=[.60*shade,.40*shade,.17*shade]
  pixels.extend((*c,1))
image=bpy.data.images.new('StarterAxe',width=size,height=size,alpha=True);image.pixels.foreach_set(pixels)
image.filepath_raw=str(out/'StarterAxe.png');image.file_format='PNG';image.save()
mat=bpy.data.materials.new('Forged iron and ash');mat.use_nodes=True
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Closest'
bsdf=mat.node_tree.nodes.get('Principled BSDF');mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color']);bsdf.inputs['Roughness'].default_value=.72
parts=[]
def finish(obj,kind,bevel=0):
 bpy.context.view_layer.objects.active=obj;obj.select_set(True)
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 uv=obj.data.uv_layers.active or obj.data.uv_layers.new();uv.name='AxeUV'
 for face in obj.data.polygons:
  for li in face.loop_indices:
   p=obj.matrix_world@obj.data.vertices[obj.data.loops[li].vertex_index].co
   if kind=='wood':u=.035+((p.x+.025)/.05)*.18;v=(p.z+.15)/.52
   elif kind=='metal':u=.27+((p.x+.045)/.265)*.46;v=(p.z-.17)/.24
   elif kind=='leather':u=.765+((p.x+.024)/.048)*.09;v=(p.z+.15)*7
   else:u=.895+((p.x+.03)/.06)*.07;v=.45+(p.z%.03)*9
   uv.data[li].uv=(min(.99,max(.01,u)),min(.98,max(.02,v)))
 obj.data.materials.clear();obj.data.materials.append(mat)
 if bevel:
  mod=obj.modifiers.new('Single plane edge bevel','BEVEL');mod.width=bevel;mod.segments=1
  bpy.ops.object.modifier_apply(modifier=mod.name)
 for face in obj.data.polygons:face.use_smooth=False
 parts.append(obj);obj.select_set(False);return obj
# Continuous shaped octagonal shaft; the grip centre stays exactly at the existing socket.
verts=[]
for z,r,x in [(-.15,.021,-.006),(-.12,.019,-.003),(-.035,.018,0),(.055,.018,0),(.20,.019,.004),(.335,.022,.002),(.35,.021,.002)]:
 for i in range(8):
  a=math.pi/8+i*math.pi/4;verts.append((x+math.cos(a)*r,math.sin(a)*r,z))
faces=[tuple(range(7,-1,-1))]
for ring in range(6):
 for i in range(8):faces.append((ring*8+i,ring*8+(i+1)%8,(ring+1)*8+(i+1)%8,(ring+1)*8+i))
faces.append(tuple(range(48,56)))
mesh=bpy.data.meshes.new('Shaped ash handle');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Shaped ash handle',mesh);bpy.context.collection.objects.link(o);finish(o,'wood')
# A stepped, single-edged axe silhouette with real thickness and bevels.
profile=[(-.045,.255),(.045,.255),(.075,.235),(.075,.21),(.11,.21),(.11,.185),(.175,.185),(.205,.215),(.215,.285),(.205,.355),(.175,.385),(.115,.385),(.115,.365),(.075,.365),(.075,.345),(-.045,.345)]
verts=[(x,d,z) for d in [-.025,.025] for x,z in profile];n=len(profile);faces=[]
poly=[Vector((x,0,z)) for x,z in profile]
for tri in tessellate_polygon([poly]):
 ids=list(tri)
 faces.extend([tuple(reversed(ids)),tuple(i+n for i in ids)])
for i in range(n):j=(i+1)%n;faces.append((i,j,j+n,i+n))
mesh=bpy.data.meshes.new('Stepped forged head');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Stepped forged head',mesh);bpy.context.collection.objects.link(o);finish(o,'metal',.0025)
def ring(name,z,radius,depth,kind):
 bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=radius,depth=depth,location=(0,0,z));o=bpy.context.object;o.name=name;return finish(o,kind,.001)
ring('Lower grip binding',-.105,.0205,.058,'leather');ring('Grip butt',-.144,.022,.016,'brass');ring('Head ferrule',.239,.024,.028,'brass')
for side in [-1,1]:
 bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=.011,depth=.006,location=(.007,side*.028,.30),rotation=(math.pi/2,0,0));o=bpy.context.object;o.name='Head pin';finish(o,'brass',.001)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();model=bpy.context.object;model.name='StarterAxe'
scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
# Recalculate all closed mesh normals consistently before export.
bm=bmesh.new();bm.from_mesh(model.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(model.data);bm.free()
model.data.calc_loop_triangles()
report={'triangles':len(model.data.loop_triangles),'vertices':len(model.data.vertices),'materials':len(model.data.materials),'dimensions_m':list(model.dimensions),'texture_pixels':[size,size],'grip_diameter_m':.036}
(source/'starter-axe-report.json').write_text(json.dumps(report,indent=2))
# Explicit authoring axes survive FBX conversion; they define the cutting direction
# independently of import rotations and the character's grip socket orientation.
for name,position in [('BladeForward',(.1,0,0)),('HandleUp',(0,0,.1))]:
 marker=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(marker);marker.parent=model;marker.location=position;marker.select_set(True)
image.filepath='//../../Assets/RivetReach/Resources/Tools/StarterAxe.png'
bpy.ops.wm.save_as_mainfile(filepath=str(source/'StarterAxe.blend'))
bpy.ops.export_scene.fbx(filepath=str(out/'StarterAxe.fbx'),use_selection=True,object_types={'MESH','EMPTY'},bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
# Neutral studio render of the actual exported geometry.
scene.render.engine='CYCLES';scene.cycles.samples=32
scene.render.resolution_x=800;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.world.color=(.18,.18,.18)
bpy.ops.object.camera_add(location=(.72,-1.7,.78));camera=bpy.context.object;camera.rotation_euler=(Vector((.06,0,.12))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=.73;scene.camera=camera
for location,power,diameter in [((-.5,-1,1.3),130,1.2),((1,.5,.8),95,1.0)]:
 bpy.ops.object.light_add(type='AREA',location=location);light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=diameter;light.rotation_euler=(Vector((0,0,.1))-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(root/'Logs/starter-axe-source.png');bpy.ops.render.render(write_still=True)
print(json.dumps(report))

scene.render.film_transparent=True;scene.render.resolution_x=64;scene.render.resolution_y=64
camera.location=(.055,-2,.12);camera.rotation_euler=(Vector((.055,0,.12))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=.61
scene.render.filepath=str(out/'StarterAxeIcon.png');bpy.ops.render.render(write_still=True)
