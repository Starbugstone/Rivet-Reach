"""Original Rivet Reach warehouse assets. Copyright 2026 Starbugstone."""
import ast,bpy,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/RivetReach/Resources/Industry';SOURCE=ROOT/'ArtSource/Crates'
SOURCE.mkdir(parents=True,exist_ok=True)
helpers=ast.parse((ROOT/'Tools/create_industry_assets.py').read_text())
for key,runtime in [('bulk_crate',176),('crate_controller',177)]:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
 mat=bpy.data.materials.new('WorkshopAtlas');mat.use_nodes=True
 bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Metallic'].default_value=.15;bs.inputs['Roughness'].default_value=.75
 tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'Atlas.png'));mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
 parts=[];motion={}
 for n in helpers.body:
  if isinstance(n,ast.FunctionDef) and n.name in ['finish','box','cyl','ring','bolt']:exec(compile(ast.Module(body=[n],type_ignores=[]),'<original workshop helpers>','exec'))
 for x in (.08,.92):
  for y in (.08,.92):
   box('Timber corner',(x,y,.48),(.13,.13,.96),4,.017)
   for z in (.14,.81):box('Iron corner strap',(x,y,z),(.155,.155,.065),1,.008)
 for x in (.095,.905):
  for z in (.29,.48,.67):box('Side plank',(x,.5,z),(.06,.80,.17),14,.008)
 for y in (.095,.905):
  for z in (.29,.48,.67):box('End plank',(.5,y,z),(.80,.06,.17),14,.008)
 for z in (.08,.88):
  box('Floor or lid',(.5,.5,z),(.8,.8,.10),4,.015)
  for x in (.24,.5,.76):box('Lid join',(x,.5,z+.052),(.007,.76,.004),1,.001)
 for y in (.12,.88):
  box('Steel lid band',(.5,y,.948),(.78,.055,.032),1,.006)
  for x in (.15,.85):bolt((x,y,.968),'Z',.02)
 box('Front label surround',(.5,.032,.55),(.54,.028,.44),1,.01)
 box('Label inset',(.5,.015,.56),(.48,.018,.37),7,.006)
 if key=='crate_controller':
  box('Brass control housing',(.5,.015,.48),(.73,.022,.67),3,.012)
  box('Bank faceplate',(.5,-.003,.57),(.49,.014,.39),1,.009)
  for x in (.24,.76):
   for z in (.27,.76):bolt((x,-.014,z),'Y',.025)
  for x in (.36,.5,.64):
   cyl('Routing dial',(x,-.008,.24),.042,.025,6,'Y',12)
  for x in (.25,.75):box('Bracket',(x,.5,.977),(.055,.70,.035),3,.006)
 else:
  box('Recessed handle',(.5,.011,.25),(.26,.045,.06),1,.014)
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.data.uv_layers.active.name='UVMap';o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();model=bpy.context.object;model.name=key
 # Keep every authored surface inside its placement cell.
 scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 for v in model.data.vertices:v.co.y=max(.005,v.co.y)
 bpy.ops.export_scene.fbx(filepath=str(OUT/(key+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False)
 scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.world.color=(.18,.2,.22)
 scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
 bpy.ops.object.camera_add(location=(2.4,-3,2.4));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.65;target=Vector((.5,.5,.5));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
 for pos,power,size in [((1,-3,5),800,4),((-3,2,3),450,3),((2,4,4),650,3)]:
  bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
 scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100;scene.render.filepath=str(OUT/'Icons'/f'{runtime}.png');bpy.ops.render.render(write_still=True)
 scene.render.resolution_x=800;scene.render.resolution_y=800;scene.render.film_transparent=False
 for side,pos in [('front',(2.4,-3,2.4)),('back',(-3,3,2.4))]:
  camera.location=pos;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(SOURCE/(key+'-'+side+'.png'));bpy.ops.render.render(write_still=True)
 for im in bpy.data.images:
  if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=str(SOURCE))
 bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(key+'.blend')))
 (SOURCE/(key+'-geometry.json')).write_text(json.dumps({'triangles':sum(len(p.vertices)-2 for p in model.data.polygons),'renderers':1,'materials':1},indent=2)+'\n')
print('CRATE_ASSETS_COMPLETE')
