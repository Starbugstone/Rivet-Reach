"""Check source weights, portable textures and evaluated animation geometry in Blender."""
import bpy,json,math
from pathlib import Path
root=Path(__file__).resolve().parents[1];report=[]
for name in ['ExplorerMale','ExplorerFemale']:
 bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Characters'/(name+'.blend')))
 rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');model=next(o for o in bpy.data.objects if o.type=='MESH')
 for v in model.data.vertices:assert abs(sum(g.weight for g in v.groups)-1)<.001
 assert len(model.data.uv_layers)==1 and len(model.data.materials)==1
 for im in bpy.data.images:
  if im.source=='FILE':assert im.filepath.startswith('//') and Path(bpy.path.abspath(im.filepath)).exists()
 actions=[]
 for action in bpy.data.actions:
  rig.animation_data.action=action
  peak=0
  for frame in [1,5,9,15,23]:
   bpy.context.scene.frame_set(min(frame,int(action.frame_range[1])))
   obj=model.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=obj.to_mesh()
   assert all(math.isfinite(c) for v in mesh.vertices for c in v.co)
   peak=max(peak,max((mesh.vertices[e.vertices[0]].co-mesh.vertices[e.vertices[1]].co).length for e in mesh.edges))
   obj.to_mesh_clear()
  assert peak<.55,(name,action.name,peak)
  actions.append({'clip':action.name,'maxEdgeMetres':round(peak,4)})
 report.append({'model':name,'normalizedWeights':True,'relativeTextures':True,'singleUV':True,'clips':actions})
(root/'Logs/player-source-checks.json').write_text(json.dumps(report,indent=2)+'\n');print('SOURCE_CHECKS_PASS')
