"""Original Floater Rock bridge/loader kit, using the project's workshop atlas."""
import ast, bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/RivetReach/Resources/Industry'; SOURCE=ROOT/'ArtSource/Bridges'
SOURCE.mkdir(parents=True,exist_ok=True)
scene=bpy.context.scene
mat=bpy.data.materials.new('WorkshopAtlas');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Metallic'].default_value=.5;bs.inputs['Roughness'].default_value=.4
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'Atlas.png'));mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
parts=[];motion={}
for n in ast.parse((ROOT/'Tools/create_industry_assets.py').read_text()).body:
    if isinstance(n,ast.FunctionDef) and n.name in ['finish','box','cyl','ring','bolt']:
        exec(compile(ast.Module(body=[n],type_ignores=[]),'<original workshop helpers>','exec'))
reports={}
for item,key,mark in [(190,'item_bridge',3),(191,'liquid_bridge',5),(192,'power_bridge',9),(193,'chunk_loader',6)]:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);parts=[]
    scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
    box('Cast plinth',(.5,.5,.09),(.96,.96,.18),0,.025)
    box('Channel stripe',(.5,.5,.2),(.89,.89,.04),mark,.006)
    cyl('Stone cradle',(.5,.5,.29),.29,.14,2,'Z',12)
    # A suspended, faceted rocky core with cyan fissures; the actual Floater loot
    # shares this rocky/cyan material language. No third-party geometry.
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=.245,location=(.5,.5,.70))
    rock=bpy.context.object;rock.scale=(1,.88,1.1);finish(rock,'Suspended Floater Rock',12)
    for a in range(0,360,90):
        angle=math.radians(a);x=.5+math.cos(angle)*.30;y=.5+math.sin(angle)*.30
        cyl('Copper field upright',(x,y,.50),.024,.56,3,'Z',10)
        box('Core fissure',(.5+math.cos(angle)*.228,.5+math.sin(angle)*.20,.71),(.018,.018,.21),6,.002)
    ring('Containment hoop',(.5,.5,.80),.32,.024,3)
    if item==193:
        ring('Loader field meridian',(.5,.5,.60),.35,.014,6,'X')
        ring('Loader field parallel',(.5,.5,.60),.35,.014,3,'Y')
        box('Chunk grid plaque',(.5,.951,.125),(.25,.018,.11),6,.003)
        for x in [.44,.50,.56]:box('Grid line',(x,.965,.125),(.007,.006,.09),0,0)
    else:
        for axis in ['X','Y']:
            for side in [.017,.983]:
                loc=(side,.5,.20) if axis=='X' else (.5,side,.20)
                if item==190:
                    box('Square cargo socket',loc,(.03,.24,.21) if axis=='X' else (.24,.03,.21),3,.008)
                    box('Cargo throat',loc,(.036,.145,.135) if axis=='X' else (.145,.036,.135),8,.006)
                else:
                    cyl('Channel socket',loc,.12,.03,8,axis,16)
                    ring('Socket rim',loc,.095,.016,mark,axis)
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();body=bpy.context.object;body.name='Floater bridge assembly'
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.export_scene.fbx(filepath=str(OUT/(key+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    reports[key]={'triangles':sum(len(p.vertices)-2 for p in body.data.polygons),'materials':1}
    scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
    scene.world.color=(.16,.18,.20);scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
    bpy.ops.object.camera_add(location=(2.3,3.4,2.1));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.52
    camera.rotation_euler=(Vector((.5,.5,.44))-camera.location).to_track_quat('-Z','Y').to_euler()
    for pos,power,size in [((1,3,5),650,4),((-3,2,2),400,3),((1,-3,3),700,3)]:
        bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((.5,.5,.5))-o.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100
    scene.render.filepath=str(OUT/'Icons'/f'{item}.png');bpy.ops.render.render(write_still=True)
    scene.render.resolution_x=800;scene.render.resolution_y=800;scene.render.film_transparent=False
    scene.render.filepath=str(SOURCE/(key+'-review.png'));bpy.ops.render.render(write_still=True)
    tex.image.filepath=bpy.path.relpath(str(OUT/'Atlas.png'),start=str(SOURCE))
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(key+'.blend')))
(SOURCE/'geometry-report.json').write_text(json.dumps(reports,indent=2)+'\n')
print('BRIDGE_ASSETS_COMPLETE',reports)
