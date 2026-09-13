"""Original iron/copper cooker pair with cooking pot, oven and distinct heat supplies."""
import bpy,math,ast,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/RivetReach/Resources/Industry';SRC=ROOT/'ArtSource/Farming';SRC.mkdir(parents=True,exist_ok=True)
for key,item,electric in [('cooker',240,False),('electric_cooker',241,True)]:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
    mat=bpy.data.materials.new('WorkshopAtlas');mat.use_nodes=True
    bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Metallic'].default_value=.65;bs.inputs['Roughness'].default_value=.4
    tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'Atlas.png'));mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    parts=[];motion={}
    for n in ast.parse((ROOT/'Tools/create_industry_assets.py').read_text()).body:
        if isinstance(n,ast.FunctionDef) and n.name in ['finish','box','cyl','ring','bolt']:exec(compile(ast.Module(body=[n],type_ignores=[]),'<workshop helpers>','exec'))
    box('Iron plinth',(.5,.5,.06),(.92,.87,.12),0,.02)
    box('Enamel stove body',(.5,.45,.38),(.82,.73,.56),7 if electric else 1,.02)
    box('Copper cooktop rim',(.5,.5,.69),(.89,.84,.07),3,.012)
    box('Black hob',(.5,.5,.73),(.79,.76,.03),0,.008)
    box('Oven opening',(.5,.839,.37),(.59,.055,.37),0,.012)
    box('Oven door',(.5,.87,.38),(.48,.03,.27),1 if electric else 2,.016)
    box('Oven handle',(.5,.932,.44),(.31,.035,.035),3,.008)
    for x in [.17,.83]:
        cyl('Heat control',(x,.862,.6),.045,.034,3,'Y',12);bolt((x,.849,.24),'Y',.025)
    if electric:
        for x in [.14,.86]:box('Insulated rail',(x,.45,.39),(.035,.63,.45),3,.008)
        cyl('Rear socket',(.5,.05,.35),.09,.04,3,'Y',12)
    else:
        box('Ash drawer',(.5,.87,.17),(.48,.04,.07),0,.009)
        for x in [.32,.4,.48,.56,.64]:box('Fire grate',(x,.89,.33),(.024,.012,.16),8,.003)
        cyl('Rear flue',(.81,.18,.61),.06,.42,0,'Z',12)
    cyl('Pot body',(.43,.5,.84),.23,.17,2,'Z',16)
    ring('Pot rim',(.43,.5,.925),.225,.018,3,'Z')
    cyl('Pot lid',(.43,.5,.938),.211,.025,0,'Z',16)
    cyl('Lid grip',(.43,.5,.975),.04,.043,3,'Z',12)
    for x in [.14,.72]:box('Pot handle',(x,.5,.87),(.12,.055,.045),0,.009)
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();body=bpy.context.object;body.name='Cooker cabinet';scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    parts=[];box('StatusLight',(.5,.855,.61),(.11,.012,.04),6 if electric else 5,.006);status=parts[0];objects=[body,status]
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=body
    bpy.ops.export_scene.fbx(filepath=str(OUT/(key+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True;scene.world.color=(.16,.18,.20);scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
    bpy.ops.object.camera_add(location=(2.3,3.4,2.3));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.5;camera.rotation_euler=(Vector((.5,.47,.5))-camera.location).to_track_quat('-Z','Y').to_euler()
    for pos,power in [((1,3,5),650),((-3,2,2),400),((1,-3,3),700)]:
        bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.size=4;o.rotation_euler=(Vector((.5,.5,.5))-o.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100;scene.render.filepath=str(OUT/('Icons/'+str(item)+'.png'));bpy.ops.render.render(write_still=True)
    scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.film_transparent=False;scene.render.filepath=str(SRC/(key+'-review.png'));bpy.ops.render.render(write_still=True)
    for im in bpy.data.images:
        if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=str(SRC))
    bpy.ops.wm.save_as_mainfile(filepath=str(SRC/(key+'.blend')))
print('COOKER_ASSETS_COMPLETE')
