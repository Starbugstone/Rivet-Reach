"""Original Rivet Reach open-jaw wrench. Copyright (c) 2026 Starbugstone."""
import ast, bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/RivetReach/Resources'; SOURCE=ROOT/'ArtSource/Wrench'
SOURCE.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
mat=bpy.data.materials.new('WorkshopAtlas');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Metallic'].default_value=.8;bs.inputs['Roughness'].default_value=.32
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'Industry/Atlas.png'));mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
parts=[];motion={}
for n in ast.parse((ROOT/'Tools/create_industry_assets.py').read_text()).body:
    if isinstance(n,ast.FunctionDef) and n.name in ['finish','box','cyl','ring','bolt']:
        exec(compile(ast.Module(body=[n],type_ignores=[]),'<original workshop helpers>','exec'))

# One continuous forged silhouette, including the open mouth. No overlapping
# cubes stand in for the jaw; its shoulders and internal flats are real topology.
profile=[(-.020,-.135),(.020,-.135),(.030,-.122),(.024,.140),(.042,.210),(.079,.240),(.105,.294),(.105,.365),(.073,.405),(.037,.384),(.041,.326),(.027,.302),(-.027,.302),(-.041,.326),(-.037,.384),(-.073,.405),(-.105,.365),(-.105,.294),(-.079,.240),(-.042,.210),(-.024,.140),(-.030,-.122)]
vertices=[(x,y,z) for y in [-.014,.014] for x,z in profile];n=len(profile)
faces=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]
for i in range(n):faces.append((i,(i+1)%n,(i+1)%n+n,i+n))
mesh=bpy.data.meshes.new('Forged open-jaw steel');mesh.from_pydata(vertices,[],faces);mesh.update()
bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(mesh);bm.free()
body=bpy.data.objects.new('Forged wrench body',mesh);scene.collection.objects.link(body);finish(body,'Forged wrench body',15,.003)
# Inset dark-steel grip cheeks and small peened brass workshop fasteners.
for y in [-.016,.016]:
    box('Recessed grip cheek',(0,y,-.012),(.035,.004,.176),1,.004)
    for z in [-.079,.055]:cyl('Peened brass grip rivet',(0,y*1.16,z),.006,.006,3,'Y',10)
    box('Forged neck relief',(0,y,.170),(.018,.004,.09),10,.002)
    for x in [-.066,.066]:box('Polished jaw face',(x,y,.331),(.027,.004,.077),10,.002)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=body;bpy.ops.object.join();model=bpy.context.object;model.name='Wrench'
scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
model.data.calc_loop_triangles();report={'triangles':len(model.data.loop_triangles),'materials':len(model.data.materials),'lengthMetres':.54,'gripOrigin':[0,0,0]}
bpy.ops.export_scene.fbx(filepath=str(OUT/'Tools/Wrench.fbx'),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,add_leaf_bones=False)

scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.world.color=(.16,.19,.23);scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
bpy.ops.object.camera_add(location=(.42,-1.5,.60));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=.66
camera.rotation_euler=(Vector((0,0,.135))-camera.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((1,-2,3),500,3),((-2,-1,1),350,2),((0,2,2),500,2)]:
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((0,0,.14))-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100
scene.render.filepath=str(OUT/'Industry/Icons/173.png');bpy.ops.render.render(write_still=True)
scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.film_transparent=False
scene.render.filepath=str(SOURCE/'wrench-review.png');bpy.ops.render.render(write_still=True)
for im in bpy.data.images:
    if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=str(SOURCE))
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Wrench.blend'))
(SOURCE/'geometry-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('WRENCH_ASSETS_COMPLETE',report)
