"""Check source weights, portable textures and evaluated animation geometry in Blender."""
import bpy,json,math
from pathlib import Path
root=Path(__file__).resolve().parents[1];report=[]
for name in ['ExplorerMale','ExplorerFemale']:
 bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Characters'/(name+'.blend')))
 rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');model=next(o for o in bpy.data.objects if o.type=='MESH')
 for v in model.data.vertices:assert abs(sum(g.weight for g in v.groups)-1)<.001
 assert max(sum(g.weight>1e-6 for g in v.groups) for v in model.data.vertices)<=4
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
   if action.name=='CrouchIdle':
    assert max(v.co.z for v in mesh.vertices)<1.30,(name,'crouch exceeds headroom')
   obj.to_mesh_clear()
  assert peak<.55,(name,action.name,peak)
  loop_error=None
  if action.get('loop',False):
   endpoints=[]
   for frame in [int(action.frame_range[0]),int(action.frame_range[1])]:
    bpy.context.scene.frame_set(frame)
    obj=model.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=obj.to_mesh()
    endpoints.append([v.co.copy() for v in mesh.vertices]);obj.to_mesh_clear()
   loop_error=max((a-b).length for a,b in zip(*endpoints));assert loop_error<.0001,(name,action.name,'loop seam',loop_error)
  actions.append({'clip':action.name,'maxEdgeMetres':round(peak,4),'loopSeamMetres':loop_error})
 report.append({'model':name,'normalizedWeights':True,'relativeTextures':True,'singleUV':True,'clips':actions})
(root/'Logs/player-source-checks.json').write_text(json.dumps(report,indent=2)+'\n');print('SOURCE_CHECKS_PASS')
