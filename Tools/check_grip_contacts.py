"""Measure evaluated hand meshes against authored contact frames in every grip clip."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1];report=[]
for name in ['ExplorerMale','ExplorerFemale']:
 bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Characters'/(name+'.blend')))
 rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');model=next(o for o in bpy.data.objects if o.type=='MESH')
 groups={g.index:g.name for g in model.vertex_groups}
 fingers=[v.index for v in model.data.vertices if sum(g.weight for g in v.groups if groups[g.group].endswith('R') and ('Finger' in groups[g.group] or 'Thumb' in groups[g.group]))>.9]
 palm=[v.index for v in model.data.vertices if sum(g.weight for g in v.groups if groups[g.group]=='HandR')>.9]
 for action in bpy.data.actions:
  if not any(g in action.name for g in ['Block','Tool','Axe','Shovel','Hoe']):continue
  rig.animation_data.action=action;peak=0;contact=None;supportError=0;supportBend=0;maxForward=-1;minUp=1;joints=[]
  for frame in range(1,int(action.frame_range[1])+1):
   bpy.context.scene.frame_set(frame);h=rig.pose.bones['HandR'];f=rig.pose.bones['ForearmR'];peak=max(peak,math.degrees((h.tail-h.head).angle(f.tail-f.head)))
   joints.append((f.head.z,h.head.z))
   axis=rig.pose.bones['ToolSocket'].matrix.to_3x3().col[1].normalized();maxForward=max(maxForward,-axis.y);minUp=min(minUp,axis.z)
   if 'TwoHandTool' in action.name:
    lh=rig.pose.bones['HandL'];lf=rig.pose.bones['ForearmL'];supportBend=max(supportBend,math.degrees((lh.tail-lh.head).angle(lf.tail-lf.head)))
    socket=rig.pose.bones['ToolSocket'].matrix;support=rig.pose.bones['HandL'].matrix @ Vector((0,.082,.047));expected=socket @ Vector((0,.12,0));supportError=max(supportError,(support-expected).length)
   if frame==1:
    obj=model.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=obj.to_mesh();inverse=h.matrix.inverted()
    if 'Block' in action.name:
     points=[inverse@mesh.vertices[i].co for i in palm+fingers]
     underneath=[v.z for v in points if abs(v.x)<.069 and -.024<v.y<.114]
     contact={'blockBottom':.0335,'highestHandSurface':max(underneath),'penetration':max(0,max(underneath)-.0335)}
    else:
     points=[inverse@mesh.vertices[i].co for i in fingers]
     distances=[math.hypot(v.y-.082,v.z-.047) for v in points]
     contact={'shaftEnvelopeRadius':.0192,'closestFingerRadius':min(distances),'penetration':max(0,.0192-min(distances))}
    obj.to_mesh_clear()
  descent=None
  if 'Mine' in action.name and 'Block' not in action.name:
   assert maxForward>.65 and minUp<.65,(name,action.name,'missing forward/down angular stroke',maxForward,minUp)
   windup=round((len(joints)-1)*(.22 if action.name.endswith(('MineTool','MineShovel')) else .28));contactFrame=round((len(joints)-1)*.54)
   descent={'elbow':joints[windup][0]-joints[contactFrame][0],'hand':joints[windup][1]-joints[contactFrame][1]}
   assert descent['elbow']>.04 and descent['hand']>.15,(name,action.name,'whole arm must descend',descent)
  assert supportError<.002,(name,action.name,'support contact',supportError)
  assert supportBend<30,(name,action.name,'support wrist',supportBend)
  assert peak<35,(name,action.name,'wrist flexion',peak)
  assert contact['penetration']<.001,(name,action.name,'mesh penetrates prop',contact)
  report.append({'model':name,'clip':action.name,'maxWristDegrees':peak,'windupToContactDescentMetres':descent,'maxShaftForward':maxForward,'minShaftUp':minUp,'contact':contact,'supportContactErrorMetres':supportError,'supportWristDegrees':supportBend})
(root/'Logs/grip-contact-checks.json').write_text(json.dumps(report,indent=2)+'\n');print('GRIP_CONTACT_MEASUREMENTS')
