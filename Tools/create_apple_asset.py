"""Original Rivet Reach apple, shared by icons, held food and dropped stacks.
Copyright (c) 2026 Starbugstone. All rights reserved.
Run with Blender --background --python Tools/create_apple_asset.py.
"""
import bpy, math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/RivetReach/Resources/Orchard'; SOURCE=ROOT/'ArtSource/Orchard'
OUT.mkdir(parents=True,exist_ok=True);SOURCE.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC'
colors=['8D2522','AA3028','C33B2C','DB5035','BD3328','D5462F','E86740','EF8650',
        '51351E','72512A','3C6226','628638','628638','81A342','A4B957','A4B957']
atlas=bpy.data.images.new('ApplePalette',width=128,height=128)
pixels=[]
for y in range(128):
 for x in range(128):
  c=colors[x//32+y//32*4];grain=.87 if (x*11+y*31)%137<3 else 1
  pixels.extend([int(c[i:i+2],16)/255*grain for i in (0,2,4)]+[1])
atlas.pixels[:]=pixels;atlas.filepath_raw=str(OUT/'Palette.png');atlas.file_format='PNG';atlas.save()
mat=bpy.data.materials.new('ApplePalette');mat.use_nodes=True
bsdf=mat.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.48
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=atlas;tex.interpolation='Closest';mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
verts=[];faces=[];slots=[]
def face(indices,slot):faces.append(indices);slots.append(slot)
# Rounded shoulders, a recessed stem well, taper and five subtle lobes at the base.
segments=24;rings=15
for j in range(rings):
 t=(j+.2)/(rings-.6)*math.pi
 radius=.44*math.sin(t)*(1+.10*math.cos(t))
 for k in range(segments):
  a=k*math.tau/segments;lobe=1+.035*math.cos(5*a)*(abs(math.cos(t))**2)
  z=.40*math.cos(t)-.082*math.exp(-(t/.35)**2)+.043*math.exp(-((math.pi-t)/.32)**2)
  verts.append((radius*math.cos(a)*lobe,radius*math.sin(a)*lobe,z))
for j in range(rings-1):
 for k in range(segments):
  a=j*segments+k;b=j*segments+(k+1)%segments;c=(j+1)*segments+(k+1)%segments;d=(j+1)*segments+k
  tone=2+round(1.5*math.sin(k*.5)+.6*math.sin(k*2+j))
  face((a,b,c),max(0,min(7,tone)));face((a,c,d),max(0,min(7,tone+(j%3==0))))
face(tuple(reversed(range(segments))),1);face(tuple((rings-1)*segments+k for k in range(segments)),0)
# Curved woody stem with shaped tapered rings.
base=len(verts)
for j in range(4):
 for k in range(8):
  a=k*math.tau/8;r=.025-j*.003
  verts.append((j*j*.004+r*math.cos(a),r*math.sin(a),.30+j*.06))
for j in range(3):
 for k in range(8):face((base+j*8+k,base+j*8+(k+1)%8,base+(j+1)*8+(k+1)%8,base+(j+1)*8+k),8+k%2)
face(tuple(base+24+k for k in range(8)),9)
# A folded pointed leaf, with a raised midrib and broad serrated edges.
base=len(verts)
verts +=[(.025,0,.435),(.10,-.075,.46),(.22,-.06,.49),(.33,.005,.50),(.22,.09,.485),(.10,.075,.455),(.17,.008,.493)]
for k in range(6):
 face((base+k,base+(k+1)%6,base+6),10+k%2)
 face((base+6,base+(k+1)%6,base+k),13)
mesh=bpy.data.meshes.new('Apple');mesh.from_pydata(verts,[],faces);mesh.update();mesh.materials.append(mat)
obj=bpy.data.objects.new('Apple',mesh);scene.collection.objects.link(obj);obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
uv=mesh.uv_layers.new(name='OrchardUV')
for p,slot in zip(mesh.polygons,slots):
 for li in p.loop_indices:
  v=mesh.vertices[mesh.loops[li].vertex_index].co
  uv.data[li].uv=((slot%4+.15+(v.x*3+v.y*2)%1*.7)/4,(slot//4+.15+(v.z*4)%1*.7)/4)
bpy.ops.export_scene.fbx(filepath=str(OUT/'Apple.fbx'),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
mesh.calc_loop_triangles();print('APPLE_TRIANGLES',len(mesh.loop_triangles),flush=True)
scene.render.engine='CYCLES';scene.cycles.samples=32
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.render.film_transparent=True
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.25,.29,.35,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
scene.view_settings.view_transform='AgX'
def aim(o,at):o.rotation_euler=(Vector(at)-o.location).to_track_quat('-Z','Y').to_euler()
for name,pos,power in [('Key',(-2,-3,4),320),('Fill',(2,1,3),180)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=3;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=pos;aim(o,(0,0,0))
d=bpy.data.cameras.new('Review');camera=bpy.data.objects.new('Review',d);scene.collection.objects.link(camera);scene.camera=camera;d.type='ORTHO';d.ortho_scale=1.18;camera.location=(1,-2,1.2);aim(camera,(0,0,.06))
scene.render.resolution_x=scene.render.resolution_y=256;scene.render.resolution_percentage=100
scene.render.filepath=str(OUT/'AppleIcon.png');bpy.ops.render.render(write_still=True)
scene.render.resolution_x=scene.render.resolution_y=800;scene.render.filepath=str(SOURCE/'apple-review.png');bpy.ops.render.render(write_still=True)
atlas.filepath='//../../Assets/RivetReach/Resources/Orchard/Palette.png';bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Apple.blend'))
print('APPLE_ASSET_COMPLETE',flush=True)
