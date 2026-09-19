"""Original compact renewable machines, sharing the Workshop atlas and metre-cell contract."""
import ast, bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/RivetReach/Resources/Industry'; SOURCE=ROOT/'ArtSource/Renewables'
SOURCE.mkdir(parents=True,exist_ok=True)
reports={}
for key,item in [('solar_panel',178),('wind_turbine',179)]:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
    mat=bpy.data.materials.new('WorkshopAtlas');mat.use_nodes=True
    bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Metallic'].default_value=.58;bs.inputs['Roughness'].default_value=.32
    tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'Atlas.png'));mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    parts=[];motion={}
    for n in ast.parse((ROOT/'Tools/create_industry_assets.py').read_text()).body:
        if isinstance(n,ast.FunctionDef) and n.name in ['finish','box','cyl','ring','bolt']:
            exec(compile(ast.Module(body=[n],type_ignores=[]),'<original workshop helpers>','exec'))
    def merge(name,pivot=(0,0,0)):
        bpy.ops.object.select_all(action='DESELECT')
        for o in parts:
            if o.data.uv_layers:o.data.uv_layers.active.name='UVMap'
            o.select_set(True)
        bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
        scene.cursor.location=pivot;bpy.ops.object.origin_set(type='ORIGIN_CURSOR');return o
    box('Cast plinth',(.5,.5,.07),(.86,.82,.14),0,.025)
    box('Copper reveal',(.5,.5,.15),(.8,.76,.025),3,.008)
    for x in [.14,.86]:
        for y in [.17,.83]:bolt((x,y,.155),'Z',.022)
    if item==178:
        # Tilted framed photovoltaic module: tessellated cells, bus bars and supported back.
        for x in [.23,.77]:
            box('Rear bracket',(x,.25,.43),(.055,.06,.54),10,.009)
            box('Front bracket',(x,.75,.28),(.055,.06,.24),10,.009)
        fixed=merge('Panel support');parts=[]
        tilt=math.radians(-25)
        def panelbox(name,x,y,z,size,color,bevel=.003):
            o=box(name,(x,.5+math.cos(tilt)*y-math.sin(tilt)*z,.57+math.sin(tilt)*y+math.cos(tilt)*z),size,color,bevel)
            # Helper returns object; rotate after bevel is applied.
            o.rotation_euler.x=tilt
        panelbox('Panel back',.5,0,0,(.92,.84,.06),0,.015)
        for x in [.058,.942]:panelbox('Edge frame',x,0,.035,(.024,.84,.025),3)
        for y in [-.408,.408]:panelbox('Edge frame',.5,y,.035,(.90,.024,.025),3)
        for ix in range(4):
            for iy in range(5):
                x=.177+ix*.215;y=-.322+iy*.161
                panelbox('PV cell',x,y,.038,(.204,.151,.012),13 if (ix+iy)%2 else 5)
                for dx in [-.05,.05]:panelbox('Cell bus',x+dx,y,.046,(.004,.144,.003),10,0)
        panel=merge('Photovoltaic module');parts=[];objects=[fixed,panel]
    else:
        # Three swept, solid airfoils around a vertical shaft; asymmetric camber is modeled topology.
        cyl('Generator housing',(.5,.5,.24),.21,.18,1,'Z',24)
        ring('Copper generator band',(.5,.5,.30),.215,.02,3,'Z')
        cyl('Shaft',(.5,.5,.62),.028,.65,10,'Z',12)
        fixed=merge('Turbine plinth');parts=[]
        for z in [.36,.93]:
            cyl('Rotor hub',(.5,.5,z),.085,.045,3,'Z',16)
            for blade in range(3):
                angle=blade*math.tau/3+(.65 if z>.5 else 0)
                arm=box('Rotor spoke',(.5+.13*math.cos(angle),.5+.13*math.sin(angle),z),(.3,.022,.018),10,.003);arm.rotation_euler.z=angle
        for blade in range(3):
            vertices=[];faces=[];steps=14
            for j in range(steps+1):
                t=j/steps;a=blade*math.tau/3+.65*t;z=.37+.55*t
                for r,offset in [(.275,-.37),(.36,0),(.32,.40),(.305,.40),(.345,0),(.26,-.37)]:
                    vertices.append((.5+r*math.cos(a+offset),.5+r*math.sin(a+offset),z))
            for j in range(steps):
                for k in range(6):faces.append((j*6+k,j*6+(k+1)%6,(j+1)*6+(k+1)%6,(j+1)*6+k))
            faces.extend([tuple(reversed(range(6))),tuple(steps*6+k for k in range(6))])
            mesh=bpy.data.meshes.new('Swept blade');mesh.from_pydata(vertices,[],faces);mesh.update();o=bpy.data.objects.new('Swept blade',mesh);bpy.context.collection.objects.link(o);finish(o,'Swept blade',10 if blade%2 else 3,.002)
        rotor=merge('MotionSpinRotor',(.5,.5,.62));parts=[];objects=[fixed,rotor]
    box('StatusLight',(.5,.916 if item==178 else .904,.105),(.08,.012,.035),6,.003)
    status=merge('StatusLight');parts=[];objects.append(status)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.fbx(filepath=str(OUT/(key+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    reports[key]={'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects),'renderers':len(objects),'materials':1}
    scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
    scene.world.color=(.16,.18,.20);scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
    bpy.ops.object.camera_add(location=(2.3,3.4,2.2));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.5
    camera.rotation_euler=(Vector((.5,.5,.48))-camera.location).to_track_quat('-Z','Y').to_euler()
    for pos,power,size in [((1,3,5),650,4),((-3,2,2),400,3),((1,-3,3),700,3)]:
        bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((.5,.5,.5))-o.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100
    scene.render.filepath=str(OUT/('Icons/'+str(item)+'.png'));bpy.ops.render.render(write_still=True)
    scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.film_transparent=False
    scene.render.filepath=str(SOURCE/(key+'-review.png'));bpy.ops.render.render(write_still=True)
    for im in bpy.data.images:
        if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=str(SOURCE))
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(key+'.blend')))
(SOURCE/'geometry-report.json').write_text(json.dumps(reports,indent=2)+'\n')
print('RENEWABLE_ASSETS_COMPLETE',reports)
