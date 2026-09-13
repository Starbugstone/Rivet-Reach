"""Original farm plants, provisions and two cookers; Blender exports and source review renders."""
import bpy, math, json, ast
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/RivetReach/Resources/Farming'; SRC=ROOT/'ArtSource/Farming'
OUT.mkdir(parents=True,exist_ok=True); SRC.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
colors=[(.16,.29,.07,1),(.30,.48,.10,1),(.48,.62,.17,1),(.79,.57,.17,1),(.93,.73,.31,1),(.18,.42,.82,1),(.57,.12,.26,1),(.84,.24,.38,1),(.86,.34,.07,1),(.95,.63,.22,1),(.45,.25,.12,1),(.86,.77,.56,1),(.74,.58,.36,1),(.95,.88,.69,1),(.26,.14,.08,1),(.64,.39,.19,1)]
palette=bpy.data.images.new('Farm palette',width=4,height=4);palette.pixels=[v for c in colors for v in c];palette.filepath_raw=str(OUT/'Palette.png');palette.file_format='PNG';palette.save()
mat=bpy.data.materials.new('FarmPalette');mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.75
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=palette;tex.interpolation='Closest';mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
parts=[];assets=[];report={}
def finish(o,color):
    o.data.materials.clear();o.data.materials.append(mat)
    uv=o.data.uv_layers.active or o.data.uv_layers.new(name='FarmUV');uv.name='FarmUV'
    for loop in uv.data:loop.uv=((color%4+.5)/4,(color//4+.5)/4)
    parts.append(o);return o
def ell(name,p,scale,color,segments=8,rings=4):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=p);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,color)
def rod(name,a,b,r,color):
    d=Vector(b)-Vector(a);bpy.ops.mesh.primitive_cylinder_add(vertices=6,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2);o=bpy.context.object;o.name=name;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();return finish(o,color)
def leaf(p,a,length,width,color):
    v=Vector(p);d=Vector((math.cos(a),math.sin(a),.32))*length;s=Vector((-math.sin(a),math.cos(a),0))*width
    mesh=bpy.data.meshes.new('Folded blade');mesh.from_pydata([v,v+d*.45-s,v+d,v+d*.45+s,v+d*.48+Vector((0,0,.025))],[],[(0,1,4),(1,2,4),(2,3,4),(3,0,4),(4,1,0),(4,2,1),(4,3,2),(4,0,3)]);mesh.update();o=bpy.data.objects.new('Leaf',mesh);scene.collection.objects.link(o);finish(o,color)
def merge(key):
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=key;scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');parts.clear();assets.append(o);return o
def export(o,key):
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.export_scene.fbx(filepath=str(OUT/(key+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    report[key]={'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'materials':len(o.data.materials)}
for base,species in [(200,'wheat'),(204,'flax'),(208,'carrot'),(212,'berry'),(216,'mushroom')]:
    for stage in range(1 if base==216 else 4):
        h=.17+.18*stage
        if species=='mushroom':
            for x,y,h in [(-.14,.04,.19),(.12,.1,.28),(.03,-.16,.15)]:
                rod('Ivory stem',(x,y,0),(x,y,h),.04,11);ell('Chestnut cap',(x,y,h),(.14,.14,.06),10)
        elif species=='carrot':
            for n in range(7):leaf((0,0,.035),n*2.4,.13+.045*stage,.022+stage*.005,1+n%2)
            if stage==3:ell('Carrot shoulder',(0,0,.015),(.12,.1,.07),8)
        else:
            stalks=5 if species=='wheat' else 3
            for n in range(stalks):
                a=n*2.4;x=math.cos(a)*.1;y=math.sin(a)*.1;top=h*(.8+.04*n)
                rod('Growing stem',(x,y,0),(x,y,top),.011,3 if species=='wheat' and stage==3 else 0)
                for l in range(2 if species=='wheat' else 3):leaf((x,y,top*(.25+l*.18)),a+l*2.2,.12+stage*.025,.018 if species=='wheat' else .035,1+l%2)
                if stage>=2 and species=='wheat':
                    for g in range(4):
                        for side in [-1,1]:ell('Grain', (x+side*.025,y,top+g*.026),(.025,.017,.036),4 if stage==3 else 2,6,3)
                if stage==3 and species=='flax':
                    for petal in range(5):ell('Flax blue petal',(x+.04*math.cos(petal*math.tau/5),y+.04*math.sin(petal*math.tau/5),top),(.032,.025,.012),5,6,3)
                    ell('Flower heart',(x,y,top+.007),(.018,.018,.012),4,6,3)
                if stage==3 and species=='berry':
                    for b in range(3):ell('Ripe berry',(x+.07*math.cos(b*2),y+.07*math.sin(b*2),top*.65+b*.025),(.046,.046,.045),6+b%2)
        o=merge(str(base+stage));export(o,o.name)
for item in range(220,236):
    if item in (220,222,226,228,221):
        for n in range(9):ell('Seeds' if item!=221 else 'Grain',(math.cos(n*2.4)*.19,math.sin(n*2.4)*.13,.04+n%3*.025),(.09,.035,.04),4 if item in (220,221) else 10)
    elif item==223:
        for n in range(8):rod('Flax fibre',(-.38,n*.025-.09,.035),(.36,n*.018-.07,.055),.014,11+n%2)
        rod('Fibre tie',(0,-.12,.055),(0,.13,.055),.025,10)
    elif item==224:
        for n in range(4):
            bpy.ops.mesh.primitive_torus_add(major_segments=16,minor_segments=6,location=(0,0,.035+n*.04),major_radius=.26-n*.025,minor_radius=.022);finish(bpy.context.object,11)
        rod('Loose cord',(.23,0,.045),(.39,-.12,.03),.022,11)
    elif item==225:
        for n in range(3):
            bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,.05+n*.05));o=bpy.context.object;o.scale=(.7-n*.04,.48,.05);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);finish(o,11+n%2)
        for n in range(9):rod('Woven fringe',(-.32+n*.075,-.26,.03),(-.32+n*.075,-.32,.02),.008,11)
    elif item in (227,232):
        bpy.ops.mesh.primitive_cone_add(vertices=10,radius1=.025,radius2=.15,depth=.57,location=(0,0,.29));finish(bpy.context.object,8 if item==227 else 15)
        for n in range(5):leaf((0,0,.58),n*2.4,.16,.025,1 if item==227 else 10)
    elif item==229:
        for n in range(7):ell('Berry',(math.cos(n*2.4)*.16,math.sin(n*2.4)*.13,.1+n%2*.1),(.1,.09,.1),6+n%2)
        leaf((0,0,.22),.5,.25,.07,1)
    elif item in (230,234):
        for x,y,h in [(-.17,0,.15),(.13,.05,.2)]:rod('Stem',(x,y,0),(x,y,h),.065,11 if item==230 else 12);ell('Cap',(x,y,h),(.2,.18,.09),10 if item==230 else 15)
    elif item==231:
        ell('Rustic loaf',(0,0,.17),(.43,.23,.18),3,12,6)
        for x in [-.22,-.07,.09,.24]:rod('Scored crust',(x,-.12,.30),(x+.045,.12,.30),.023,13)
    else:
        # Solid soup surface and a thick ceramic rim: no hidden liquid simulation.
        ell('Earthen bowl',(0,0,.1),(.34,.29,.16),10,12,6)
        bpy.ops.mesh.primitive_torus_add(major_segments=16,minor_segments=6,major_radius=.28,minor_radius=.035,location=(0,0,.18));finish(bpy.context.object,12)
        ell('Prepared meal',(0,0,.18),(.265,.245,.035),15 if item==233 else 13,12,4)
        for n in range(9):ell('Vegetables' if item==233 else 'Fruit pieces',(math.cos(n*2.4)*(.09+n%2*.08),math.sin(n*2.4)*.16,.214),(.033,.029,.021),[8,1,11][n%3] if item==233 else 6+n%2,6,3)
    o=merge(str(item));export(o,o.name)
scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True;scene.world.color=(.18,.18,.18);scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
bpy.ops.object.camera_add(location=(1.7,2.6,1.8));cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=1.1
for p,power in [((1,3,5),650),((-3,1,3),400)]:
    bpy.ops.object.light_add(type='AREA',location=p);l=bpy.context.object;l.data.energy=power;l.data.size=4;l.rotation_euler=(Vector((0,0,.3))-l.location).to_track_quat('-Z','Y').to_euler()
for o in assets:o.hide_render=True
scene.render.resolution_x=192;scene.render.resolution_y=192;scene.render.resolution_percentage=100
for o in assets:
    o.hide_render=False;centre=sum((o.matrix_world@Vector(v) for v in o.bound_box),Vector())/8
    cam.rotation_euler=(centre-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/(o.name+'Icon.png'));bpy.ops.render.render(write_still=True);o.hide_render=True
# Source review shows the complete progression with real models.
for n,o in enumerate(assets):o.hide_render=False;o.location=(n%9*1.05,n//9*1.05,0)
cam.location=(6,-10,12);cam.rotation_euler=(Vector((4,1.7,.1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=11
scene.render.resolution_x=1500;scene.render.resolution_y=850;scene.render.film_transparent=False;scene.render.filepath=str(SRC/'farm-review.png');bpy.ops.render.render(write_still=True)
for im in bpy.data.images:
    if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=str(SRC))
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Farming.blend'));(SRC/'geometry-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('FARM_ASSETS_COMPLETE')
