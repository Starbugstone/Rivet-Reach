"""Original Rivet Reach chickens and provisions. Copyright 2026 Starbugstone.
Blender --background --python Tools/create_chicken_assets.py [-- --output-root PATH].
"""
import bpy,bmesh,math,json,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
if '--project-root' in sys.argv: ROOT=Path(sys.argv[sys.argv.index('--project-root')+1])
sys.path.insert(0,str(ROOT/'Tools'))
import create_mob_assets as art
BASE=Path(sys.argv[sys.argv.index('--output-root')+1]) if '--output-root' in sys.argv else ROOT
OUT=BASE/'Assets/RivetReach/Resources/Chickens';SRC=BASE/'ArtSource/Chickens'
for p in (OUT,SRC):p.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1;scene.render.fps=30
palette=[(.78,.54,.24),(.94,.83,.61),(.34,.16,.07),(.66,.31,.10),(.92,.63,.15),(.83,.14,.09),(.055,.045,.025),(.99,.96,.83),(.94,.76,.18),(.87,.57,.42),(.50,.23,.10),(.17,.30,.10),(.72,.37,.12),(.18,.16,.12),(.52,.37,.20),(.96,.88,.66)]
image=bpy.data.images.new('ChickenPalette',width=64,height=64)
image.pixels.foreach_set([v for y in range(64) for x in range(64) for v in (*palette[(y//16)*4+x//16],1)])
image.filepath_raw=str(OUT/'Palette.png');image.file_format='PNG';image.save()
material=bpy.data.materials.new('ChickenPalette');material.use_nodes=True
tex=material.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Closest'
material.node_tree.links.new(tex.outputs['Color'],material.node_tree.nodes.get('Principled BSDF').inputs['Base Color']);material.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.78
art.material=material;art.parts=[];assets=[];report={}
def ell(name,p,s,c,bone='Body'):
 rings=[]
 for j in range(9):
  a=-math.pi/2+j*math.pi/8;r=max(.012,math.cos(a));rings.append((p[1]+s[1]*math.sin(a),p[2],s[0]*r,s[2]*r))
 o=art.loft(name,rings,c,bone,12)
 for v in o.data.vertices:v.co.x+=p[0]
 return o
def join(name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in art.parts:o.select_set(True)
 bpy.context.view_layer.objects.active=art.parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
 scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');art.parts=[];return o
def export_item(key):
 o=join(key);o.vertex_groups.clear();bpy.ops.export_scene.fbx(filepath=str(OUT/(key+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False);assets.append(o);report[key]={'triangles':len(o.data.polygons),'materials':len(o.data.materials)};o.hide_render=True;return o
for chick in (False,True):
 name='Chick' if chick else 'Chicken';scale=.56 if chick else 1;art.parts=[]
 art.loft('Shaped feathered torso',[(-.31,.43,.08,.1),(-.21,.48,.22,.23),(0,.51,.27,.28),(.19,.56,.21,.27),(.28,.62,.1,.15)],8 if chick else 0,'Body',16)
 art.loft('Rising neck',[(.10,.63,.15,.17),(.24,.73,.115,.18),(.29,.84,.10,.14),(.36,.89,.08,.09)],8 if chick else 1,'Head',12)
 ell('Rounded head',(0,.34,.88),(.155 if chick else .14,.185,.17),8 if chick else 1,'Head')
 art.mesh_part('Upper beak',[(-.075,.47,.9),(.075,.47,.9),(0,.64,.855),(0,.48,.96)],[(0,1,2),(0,3,1),(0,2,3),(1,3,2)],4,'Head')
 art.mesh_part('Lower beak',[(-.065,.48,.88),(.065,.48,.88),(0,.62,.852),(0,.48,.83)],[(0,2,1),(0,1,3),(0,3,2),(1,2,3)],12,'Head')
 for side,label in [(-1,'L'),(1,'R')]:
  ell('Eye rim',(side*.128,.39,.935),(.025,.057,.056),7,'Head');ell('Bright black eye',(side*.148,.402,.94),(.012,.037,.039),6,'Head');ell('Eye glint',(side*.158,.417,.955),(.004,.01,.01),7,'Head')
  wing=ell('Layered wing',(side*.235,-.045,.52),(.067,.255,.145),8 if chick else 3,'Wing'+label)
  if not chick:
   for j in range(4):art.tube('Flight feather',[(side*.245,.045-j*.068,.57),(side*.294,-.08-j*.065,.46),(side*.235,-.21-j*.055,.40)],[.039,.042,.012],1 if j%2 else 2,'Wing'+label,6)
  art.tube('Shank',[(side*.12,.01,.36),(side*.12,.03,.13),(side*.12,.10,.06)],[.035,.025,.028],4,'Leg'+label,7)
  for dx in (-.075,0,.075):art.tube('Separated toe',[(side*.12,.10,.06),(side*.12+dx*.6,.18,.037),(side*.12+dx,.26,.035)],[.021,.016,.007],4,'Leg'+label,6)
  art.tube('Back toe',[(side*.12,.10,.06),(side*.12,-.04,.028)],[.018,.009],12,'Leg'+label,6)
 if not chick:
  comb=[(-.025,.24,.99),(-.025,.27,1.105),(-.025,.32,1.045),(-.025,.38,1.15),(-.025,.425,1.075),(-.025,.47,1.115),(-.025,.49,.995)]
  verts=comb+[(.025,y,z) for _,y,z in comb];n=len(comb);faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
  art.mesh_part('Sculpted comb',verts,faces,5,'Head');ell('Wattle',(0,.445,.76),(.055,.05,.09),5,'Head')
 for j in range(3 if chick else 5):
  x=(j-(1 if chick else 2))*.06
  art.tube('Tail fan',[(x,-.19,.55),(x*1.3,-.38,.67),(x*1.5,-.48,.83 if not chick else .68)],[.063,.073,.017],8 if chick else (2 if j%2 else 1),'Tail',7)
 mesh=join(name+'Mesh')
 # The chick has its own feather/head proportions and no comb; scale once in source geometry.
 for v in mesh.data.vertices:v.co*=scale
 bones=[('Root',(0,0,0),(0,0,.2),None),('Body',(0,0,.48),(0,0,.68),'Root'),('Head',(0,.20,.69),(0,.41,.88),'Body'),('WingL',(-.2,0,.57),(-.29,-.12,.47),'Body'),('WingR',(.2,0,.57),(.29,-.12,.47),'Body'),('LegL',(-.12,.015,.35),(-.12,.08,.06),'Body'),('LegR',(.12,.015,.35),(.12,.08,.06),'Body'),('Tail',(0,-.19,.53),(0,-.4,.7),'Body')]
 arm=bpy.data.armatures.new(name+'Skeleton');rig=bpy.data.objects.new(name,arm);scene.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True);mesh.select_set(False);bpy.ops.object.mode_set(mode='EDIT')
 for n,h,t,parent in bones:
  b=arm.edit_bones.new(n);b.head=Vector(h)*scale;b.tail=Vector(t)*scale
  if parent:b.parent=arm.edit_bones[parent]
 bpy.ops.object.mode_set(mode='OBJECT');mesh.parent=rig;mod=mesh.modifiers.new('Chicken rig','ARMATURE');mod.object=rig;rig.animation_data_create()
 for action_name,length in [('Idle',60),('Walk',30),('Peck',45),('Death',36)]:
  action=bpy.data.actions.new(action_name);action.use_fake_user=True;rig.animation_data.action=action
  for frame in range(1,length+2):
   t=(frame-1)/length;wave=math.sin(t*math.tau)
   for b in rig.pose.bones:
    b.rotation_mode='XYZ';b.rotation_euler=(0,0,0);b.location=(0,0,0)
    if b.name=='Body':b.location.y=(.018*abs(wave) if action_name=='Walk' else .008*wave)*scale
    if b.name.startswith('Leg') and action_name=='Walk':b.rotation_euler.x=wave*.48*(1 if b.name.endswith('L') else -1)
    if b.name=='Head':b.rotation_euler.x=.09*wave if action_name=='Idle' else .7*math.sin(math.pi*t) if action_name=='Peck' else .10*wave
    if b.name.startswith('Wing'):b.rotation_euler.y=.07*wave*(1 if b.name.endswith('L') else -1)
    if b.name=='Body' and action_name=='Death':b.rotation_euler.z=math.pi*.48*min(1,t*1.3);b.location.y=-.22*scale*min(1,t*1.3)
    b.keyframe_insert(data_path='rotation_euler',frame=frame,group=b.name);b.keyframe_insert(data_path='location',frame=frame,group=b.name)
  action.frame_end=length+1
 rig.animation_data.action=bpy.data.actions.get('Idle' if not chick else 'Idle.001');scene.frame_set(1)
 bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=rig
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
 report[name]={'triangles':len(mesh.data.polygons),'bones':len(bones),'materials':len(mesh.data.materials)};assets.append(rig);rig.hide_render=True;mesh.hide_render=True
 # Keep all source actions; Unity selects this rig's four named takes.
# Item silhouettes share this original palette.
art.parts=[];ell('Egg',(0,0,.16),(.15,.14,.22),7);export_item('248')
for key,c in [('249',9),('250',10)]:
 art.parts=[];art.loft('Chicken portion',[(-.22,.10,.07,.065),(-.12,.15,.20,.13),(.10,.13,.23,.12),(.24,.10,.12,.07)],c,'Body',14)
 art.tube('Bone',[(0,-.12,.12),(0,-.37,.12)],[.045,.028],7,'Body',8)
 for x in (-.032,.032):ell('Bone end',(x,-.38,.12),(.046,.036,.038),7)
 if key=='250':
  for y in (-.08,.02,.12):art.tube('Roasting mark',[(-.11,y,.235),(.1,y+.025,.235)],[.009,.008],2,'Body',5)
 export_item(key)
art.parts=[]
art.tube('Feather shaft',[(0,0,-.3),(0,0,.10),(0,0,.45)],[.008,.012,.005],0,'Body',6)
for side in (-1,1):
 for j in range(12):
  z=-.15+j*.046;width=.13*math.sin((j+1)*math.pi/14)
  art.tube('Feather vane',[(0,0,z),(side*width*.7,0,z+.06),(side*width,0,z+.11)],[.011,.019,.006],1 if j%3 else 7,'Body',5)
export_item('251')
art.parts=[];ell('Cooked egg white',(0,0,.025),(.26,.22,.035),7);ell('Golden yolk',(0,0,.065),(.10,.10,.06),8);export_item('252')
art.parts=[]
# Closed bowl with contrasting soup surface.
art.loft('Clay bowl',[(-.27,.10,.04,.04),(-.20,.10,.20,.10),(0,.10,.29,.13),(.20,.10,.20,.10),(.27,.10,.04,.04)],14,'Body',16)
ell('Broth',(0,0,.195),(.25,.22,.015),12)
for x,y in [(-.11,-.05),(.07,.09),(.13,-.08)]:ell('Chicken in stew',(x,y,.222),(.07,.035,.018),1)
for x,y in [(-.1,.12),(.01,-.14),(.15,.02)]:ell('Vegetable',(x,y,.225),(.035,.035,.025),11)
export_item('253')
# Save complete editable scene; studio renders are derived from the same source meshes.
image.filepath='//../../Assets/RivetReach/Resources/Chickens/Palette.png'
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Chickens.blend'))
scene.render.engine='CYCLES';scene.cycles.samples=20;scene.world.color=(.23,.23,.23);scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO'
for p,power in [((3,4,5),550),((-3,1,3),400),((0,-4,4),500)]:
 bpy.ops.object.light_add(type='AREA',location=p);o=bpy.context.object;o.data.energy=power;o.data.size=4;o.rotation_euler=(Vector((0,0,.45))-o.location).to_track_quat('-Z','Y').to_euler()
for o in assets:
 if o.type=='ARMATURE':continue
 o.hide_render=False;center=sum((o.matrix_world@Vector(v) for v in o.bound_box),Vector())/8;cam.location=center+Vector((1.6,2.5,1.8));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=.95;scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.filepath=str(OUT/(o.name+'Icon.png'));bpy.ops.render.render(write_still=True);o.hide_render=True
for rig in assets:
 if rig.type=='ARMATURE':
  rig.hide_render=False
  for child in rig.children:child.hide_render=False
  rig.location.x=-.65 if rig.name=='Chicken' else .5
scene.render.resolution_x=1100;scene.render.resolution_y=750;scene.render.film_transparent=False;cam.data.ortho_scale=2.45
for name,p in [('front',(2.8,5,2.6)),('back',(-2.8,-5,2.6))]:
 cam.location=p;cam.rotation_euler=(Vector((0,0,.5))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(SRC/('chickens-'+name+'.png'));bpy.ops.render.render(write_still=True)
(SRC/'geometry.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
