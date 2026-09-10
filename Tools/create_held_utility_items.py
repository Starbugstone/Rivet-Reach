"""Original held torch and palm-supported bucket. Copyright (c) 2026 Starbugstone."""
import bpy,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/RivetReach/Resources/Characters';SOURCE=ROOT/'ArtSource/Characters'
for name in ['GripTorch','PalmBucket']:
 bpy.ops.wm.read_factory_settings(use_empty=True);parts=[]
 def mesh(label,vs,fs,tile):
  data=bpy.data.meshes.new(label);data.from_pydata(vs,[],fs);data.update();ob=bpy.data.objects.new(label,data);bpy.context.collection.objects.link(ob)
  uv=data.uv_layers.new(name='SkinUV')
  for p in data.polygons:
   for li in p.loop_indices:uv.data[li].uv=((tile%4+.5)/4,(tile//4+.5)/4)
  parts.append(ob);return ob
 def rings(label,profile,tile,sides=24):
  vs=[(r*math.cos(i*math.tau/sides),r*math.sin(i*math.tau/sides),z) for z,r in profile for i in range(sides)]
  fs=[(j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i) for j in range(len(profile)-1) for i in range(sides)]
  return mesh(label,vs,fs,tile)
 if name=='GripTorch':
  rings('Carved hardwood torch',[(-.16,0),(-.16,.017),(.16,.017),(.17,0)],8)
  for j in range(10):rings('Cloth binding',[(.10+j*.010,.018),(.106+j*.010,.021),(.11+j*.010,.018)],1)
  rings('Flame silhouette',[(.18,.006),(.215,.031),(.253,.019),(.315,.001)],12,7)
 else:
  # Unit dimensions are scaled by the shared 14 cm palm-item presentation.
  rings('Double-wall pail',[(-.5,0),(-.5,.29),(-.45,.34),(.37,.45),(.41,.45),(.41,.40),(-.43,.29),(-.43,0)],15)
  rings('Rolled rim',[(.35,.449),(.38,.47),(.415,.46),(.43,.425),(.405,.40)],12)
  rings('Foot ring',[(-.5,.28),(-.48,.35),(-.43,.35)],15)
  # A folded wire bail at the side leaves the palm support unobstructed.
  vs=[];n=32;sides=8
  for j in range(n):
   a=j/(n-1)*math.pi;center=(.44*math.cos(a),.28*math.sin(a),.27-.25*math.sin(a))
   for k in range(sides):
    b=k*math.tau/sides;vs.append((center[0],center[1]+.018*math.cos(b),center[2]+.018*math.sin(b)))
  fs=[(j*sides+k,j*sides+(k+1)%sides,(j+1)*sides+(k+1)%sides,(j+1)*sides+k) for j in range(n-1) for k in range(sides)]
  mesh('Folded bail handle',vs,fs,15)
 bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();model=bpy.context.object;model.name=name
 mat=bpy.data.materials.new('Shared tool palette');mat.use_nodes=True;tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'SkinField.png'));tex.image.filepath='//../../Assets/RivetReach/Resources/Characters/SkinField.png';mat.node_tree.links.new(tex.outputs['Color'],mat.node_tree.nodes['Principled BSDF'].inputs['Base Color']);model.data.materials.clear();model.data.materials.append(mat)
 bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(name+'.blend')))
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
print('UTILITY_ITEM_EXPORT_PASS')
