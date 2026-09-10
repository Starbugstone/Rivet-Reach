"""Original Rivet Reach battery kit; reuses only this project's Workshop geometry helpers."""
import ast, bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/RivetReach/Resources/Industry'; SOURCE=ROOT/'ArtSource/Batteries'
SOURCE.mkdir(exist_ok=True,parents=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
mat=bpy.data.materials.new('WorkshopAtlas');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Metallic'].default_value=.65;bs.inputs['Roughness'].default_value=.34
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'Atlas.png'));mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
parts=[];motion={}
for n in ast.parse((ROOT/'Tools/create_industry_assets.py').read_text()).body:
    if isinstance(n,ast.FunctionDef) and n.name in ['finish','box','cyl','ring','bolt','tube','gauge']:
        exec(compile(ast.Module(body=[n],type_ignores=[]),'<original workshop helpers>','exec'))
def merge(objects,name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');return o
assets=[];report={}
for id,key in [(168,'battery_block'),(169,'battery_controller')]:
    parts=[];motion={}
    box('Cast base',(.5,.5,.065),(.98,.98,.13),0,.02)
    box('Top bus cover',(.5,.5,.94),(.98,.98,.12),1,.02)
    for x in [.06,.94]:
        for y in [.06,.94]:
            box('Corner rail',(x,y,.5),(.1,.1,.8),1,.012);bolt((x,y,.999),r=.023)
    if id==168:
        for x in [.27,.5,.73]:
            cyl('Accumulator can',(x,.53,.49),.103,.65,11,'Z',16)
            ring('Rolled can lip',(x,.53,.8),.096,.014,10)
            cyl('Insulated terminal',(x,.53,.85),.04,.07,8)
            cyl('Copper terminal',(x,.53,.884),.021,.03,3)
        box('Copper bus bar',(.5,.53,.9),(.56,.065,.024),3,.006)
        for y in [.12,.88]:
            for z in [.23,.74]:box('Protective grille',(.5,y,z),(.82,.025,.035),2,.008)
        for y,axis in [(0,'Y'),(1,'Y')]:
            cyl('Keyed power socket',(.5,y,.49),.11,.026,8,axis,12)
            ring('Copper power rim',(.5,y,.49),.09,.014,3,axis)
        for x in [0,1]:
            cyl('Keyed side socket',(x,.5,.49),.11,.026,8,'X',12);ring('Copper side rim',(x,.5,.49),.09,.014,3,'X')
        for z in [.003,.995]:
            cyl('Keyed vertical socket',(.5,.5,z),.11,.026,8,'Z',12);ring('Copper vertical rim',(.5,.5,z),.09,.014,3)
        box('Capacity plate',(.5,.96,.69),(.28,.024,.1),7,.006)
        box('Plus horizontal',(.5,.978,.69),(.1,.005,.014),0,0);box('Plus vertical',(.5,.978,.69),(.014,.005,.065),0,0)
    else:
        box('Controller shell',(.5,.5,.5),(.81,.81,.77),0,.03)
        box('Brushed front panel',(.5,.958,.5),(.80,.04,.72),2,.015)
        gauge(.5,1.005,.68,.17)
        cyl('Bank power socket',(.5,1.012,.31),.12,.024,8,'Y',12);ring('Bank power rim',(.5,1.018,.31),.10,.015,3,'Y')
        for x,z in [(.465,.33),(.535,.33),(.5,.27)]:cyl('Bank contact',(x,1.03,z),.012,.015,3,'Y',8)
        for x in [.12,.88]:
            for z in [.2,.8]:bolt((x,.993,z),'Y',.024)
        for z in [.3,.38,.46,.54,.62]:box('Cooling louvre',(.919,.5,z),(.02,.48,.025),8,.003)
    body=merge(parts,'Battery housing');parts=[]
    box('Status pilot',(.77,.99,.30),(.07,.018,.07),6,.005);pilot=merge(parts,'StatusLight');objects=[body,pilot]
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=body
    bpy.ops.export_scene.fbx(filepath=str(OUT/(key+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    report[key]={'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects),'parts':len(objects)}
    assets.append((id,key,objects))
    for o in objects:o.hide_render=True
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.world.color=(.20,.20,.20);scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
bpy.ops.object.camera_add(location=(2.3,3.1,2.1));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.9
camera.rotation_euler=(Vector((.5,.5,.5))-camera.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((1,3,5),650,4),((-3,2,2),400,3),((1,-3,3),700,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((.5,.5,.5))-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100
for id,key,objects in assets:
    for o in objects:o.hide_render=False
    scene.render.filepath=str(OUT/'Icons'/f'{id}.png');bpy.ops.render.render(write_still=True)
    for o in objects:o.hide_render=True
for i,(_,_,objects) in enumerate(assets):
    for o in objects:o.location.x+=i*1.4;o.hide_render=False
camera.location=(4,6,4);camera.rotation_euler=(Vector((1.2,.5,.5))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=3.5
scene.render.film_transparent=False;scene.world.color=(.055,.075,.09);scene.render.resolution_x=1280;scene.render.resolution_y=800
scene.render.filepath=str(SOURCE/'battery-kit-review.png');bpy.ops.render.render(write_still=True)
for im in bpy.data.images:
    if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=str(SOURCE))
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'BatteryKit.blend'))
(SOURCE/'geometry-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('BATTERY_ASSETS_COMPLETE')
