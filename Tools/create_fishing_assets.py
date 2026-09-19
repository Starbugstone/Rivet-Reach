"""Original rod, float and fish provisions; run in Blender background mode."""
import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/RivetReach/Resources/Fishing';SRC=ROOT/'ArtSource/Fishing'
OUT.mkdir(parents=True,exist_ok=True);SRC.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
colors=[(.19,.1,.045,1),(.48,.28,.11,1),(.8,.65,.38,1),(.87,.81,.64,1),(.14,.36,.38,1),(.37,.61,.6,1),(.76,.83,.73,1),(.025,.035,.025,1),(.72,.24,.12,1),(.97,.85,.58,1),(.48,.22,.065,1),(.77,.43,.14,1),(.23,.35,.06,1),(.91,.36,.09,1),(.4,.24,.14,1),(.67,.5,.28,1)]
im=bpy.data.images.new('Fishing palette',width=4,height=4);im.pixels=[v for c in colors for v in c];im.filepath_raw=str(OUT/'Palette.png');im.file_format='PNG';im.save()
mat=bpy.data.materials.new('FishingPalette');mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.65
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=im;tex.interpolation='Closest';mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
parts=[];assets=[];report={}
def finish(o,c):
 o.data.materials.clear();o.data.materials.append(mat);uv=o.data.uv_layers.active or o.data.uv_layers.new(name='FishingUV');uv.name='FishingUV'
 for loop in uv.data:loop.uv=((c%4+.5)/4,(c//4+.5)/4)
 parts.append(o);return o
def ell(name,p,scale,c):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=1,location=p);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,c)
def rod(name,a,b,r,c):
 d=Vector(b)-Vector(a);bpy.ops.mesh.primitive_cylinder_add(vertices=10,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2);o=bpy.context.object;o.name=name;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,c)
def fin(name,vertices,c):
 normal=(Vector(vertices[1])-Vector(vertices[0])).cross(Vector(vertices[2])-Vector(vertices[0])).normalized()*.004;points=[Vector(v)+normal for v in vertices]+[Vector(v)-normal for v in vertices];mesh=bpy.data.meshes.new(name);mesh.from_pydata(points,[],[(0,1,2),(5,4,3),(0,3,4,1),(1,4,5,2),(2,5,3,0)]);mesh.update();o=bpy.data.objects.new(name,mesh);scene.collection.objects.link(o);return finish(o,c)
def ring(name,p,major,minor,c,rotation=(0,0,0)):
 bpy.ops.mesh.primitive_torus_add(major_segments=16,minor_segments=6,major_radius=major,minor_radius=minor,location=p,rotation=rotation);o=bpy.context.object;o.name=name;return finish(o,c)
def export(key):
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.name=key;scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');parts.clear();assets.append(o)
 bpy.ops.export_scene.fbx(filepath=str(OUT/(key+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
 report[key]={'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'materials':len(o.data.materials)};return o
# Tapered rod, wrapped grip, carved spool and line guides. Pivot stays at the hand grip.
for i in range(7):rod('Tapered wooden shaft',(0,0,-.18+i*.225),(0,0,-.18+(i+1)*.225),.029-i*.0033,1)
for i in range(9):ring('Cord-wrapped grip',(0,0,-.12+i*.024),.032,.005,2)
rod('Reel axle',(-.085,0,-.04),(.065,0,-.04),.018,2)
rod('Reel spool',(-.065,0,-.04),(-.025,0,-.04),.067,0)
for x in [-.07,-.02]:rod('Reel rim',(x-.004,0,-.04),(x+.004,0,-.04),.079,2)
rod('Crank arm',(-.09,0,-.04),(-.09,.07,-.04),.012,2);rod('Crank grip',(-.11,.07,-.04),(-.08,.07,-.04),.017,0)
for z in [.28,.66,1.02,1.38]:ring('Line guide',(0,-.035,z),.025,.006,2,(math.pi/2,0,0))
rod('Resting line',(0,-.06,.0),(0,-.06,1.38),.003,3)
export('244')
for key,c in [('245',4),('246',10)]:
 # Ring-defined body: tapered nose, shoulders, narrow tail root. Coherent fish silhouette.
 rings=[(-.4,.013,.018),(-.33,.065,.075),(-.16,.105,.16),(.08,.09,.15),(.25,.045,.08),(.33,.025,.035)]
 verts=[]
 for x,w,h in rings:
  for i in range(12):a=i*math.tau/12;verts.append((x,math.sin(a)*w,.17+math.cos(a)*h))
 faces=[]
 for r in range(len(rings)-1):
  for i in range(12):faces.append((r*12+i,r*12+(i+1)%12,(r+1)*12+(i+1)%12,(r+1)*12+i))
 faces.extend([tuple(reversed(range(12))),tuple(range(60,72))]);mesh=bpy.data.meshes.new('Fish body');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Fish body',mesh);scene.collection.objects.link(o);finish(o,c)
 # Tail is visibly forked, with contrasting dorsal and paired side fins.
 fin('Upper tail',[(.28,0,.17),(.52,.015,.34),(.46,0,.17)],c+1);fin('Lower tail',[(.28,0,.17),(.46,0,.17),(.52,.015,0)],c+1)
 fin('Dorsal fin',[(-.1,0,.31),(.09,0,.43),(.17,0,.26)],c+1)
 for side in [-1,1]:
  fin('Side fin',[(-.14,side*.09,.17),(.03,side*.20,.075),(.02,side*.08,.15)],c+1)
  ell('Pale eye',(-.28,side*.06,.2),(.025,.014,.025),9);ell('Dark pupil',(-.285,side*.072,.202),(.012,.008,.014),7)
 if key=='246':
  for x in [-.12,.01,.14]:rod('Charred score',(x,-.075,.245),(x+.025,.075,.245),.009,0)
 export(key)
ell('Earthen bowl',(0,0,.10),(.32,.28,.14),14);ring('Thick bowl rim',(0,0,.18),.27,.032,15);ell('Broth',(0,0,.18),(.26,.24,.026),11)
for i in range(10):ell('Fish flakes and vegetables',(math.cos(i*2.4)*(.11+i%2*.06),math.sin(i*2.4)*.16,.211),(.043,.029,.018),[6,12,13][i%3])
export('247')
ell('Ivory float',(0,0,0),(.075,.075,.10),9);ell('Orange cap',(0,0,.055),(.068,.068,.065),8);rod('Float antenna',(0,0,.1),(0,0,.22),.009,8);export('Bobber')
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True;scene.world.color=(.18,.18,.18);scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
bpy.ops.object.camera_add(location=(1.4,-2.2,1.6));cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO'
for p,power in [((1,-3,5),650),((-3,1,3),450)]:
 bpy.ops.object.light_add(type='AREA',location=p);o=bpy.context.object;o.data.energy=power;o.data.size=4;o.rotation_euler=(Vector((0,0,.3))-o.location).to_track_quat('-Z','Y').to_euler()
for o in assets:o.hide_render=True
scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100
for o in assets:
 o.hide_render=False;centre=sum((o.matrix_world@Vector(v) for v in o.bound_box),Vector())/8;cam.data.ortho_scale=1.8 if o.name=='244' else 1.1;cam.rotation_euler=(centre-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/(o.name+'Icon.png'));bpy.ops.render.render(write_still=True);o.hide_render=True
for i,o in enumerate(assets):o.hide_render=False;o.location=((i-2)*1.2,0,0)
cam.location=(2,-7,5);cam.rotation_euler=(Vector((0,0,.3))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=6.6;scene.render.resolution_x=1400;scene.render.resolution_y=700;scene.render.film_transparent=False;scene.render.filepath=str(SRC/'fishing-review.png');bpy.ops.render.render(write_still=True)
for image in bpy.data.images:
 if image.filepath:image.filepath=bpy.path.relpath(image.filepath,start=str(SRC))
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Fishing.blend'));(SRC/'geometry.json').write_text(json.dumps(report,indent=2)+'\n');print('FISHING_ASSETS_COMPLETE')
