"""Original Blender pipe shapes for all six-face connection masks.
Owns item/fluid pipe exports and their independent power/signal leads.
Straight runs have no repeating unions; only real branches receive fittings.
"""
import bpy, bmesh, math, json, ast
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/RivetReach/Resources/Industry'; SOURCE=ROOT/'ArtSource/Pipes'
SOURCE.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
mat=bpy.data.materials.new('WorkshopAtlas');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Metallic'].default_value=.65;bs.inputs['Roughness'].default_value=.32
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'Atlas.png'));mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
parts=[];motion={}
for n in ast.parse((ROOT/'Tools/create_industry_assets.py').read_text()).body:
    if isinstance(n,ast.FunctionDef) and n.name in ['finish','box','ring']:
        exec(compile(ast.Module(body=[n],type_ignores=[]),'<original workshop helpers>','exec'))
# Unity faces +X,-X,+Y,-Y,+Z,-Z expressed in Blender's metric coordinates.
D=[Vector(v) for v in [(1,0,0),(-1,0,0),(0,0,1),(0,0,-1),(0,-1,0),(0,1,0)]]
C=Vector((.5,.5,.5))

def merge(objects,name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    if len(objects)>1:bpy.ops.object.join()
    o=bpy.context.object;o.name=name
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    return o

def sweep(points,r,color,caps=(False,False),name='Pipe surface'):
    points=[Vector(p) for p in points];vertices=[];faces=[];sides=16;normal=None
    for i,p in enumerate(points):
        tangent=(points[min(i+1,len(points)-1)]-points[max(0,i-1)]).normalized()
        if normal is None:
            normal=tangent.cross(Vector((0,0,1)) if abs(tangent.z)<.9 else Vector((0,1,0))).normalized()
        else:normal=(normal-tangent*normal.dot(tangent)).normalized()
        binormal=tangent.cross(normal).normalized()
        for j in range(sides):
            a=2*math.pi*j/sides;vertices.append(p+r*(math.cos(a)*normal+math.sin(a)*binormal))
    for i in range(len(points)-1):
        for j in range(sides):
            k=(j+1)%sides;faces.append((i*sides+j,i*sides+k,(i+1)*sides+k,(i+1)*sides+j))
    if caps[0]:faces.append(tuple(reversed(range(sides))))
    if caps[1]:faces.append(tuple((len(points)-1)*sides+j for j in range(sides)))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);scene.collection.objects.link(o);finish(o,name,color)
    for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
    return o

def rounded(points,r=.12):
    a,c,b=map(Vector,points);u=(a-c).normalized();v=(b-c).normalized()
    if abs(u.dot(v))>.999:return [a,b]
    r=min(r,(a-c).length*.45,(b-c).length*.45);p=c+u*r;q=c+v*r
    return [a,p]+[(1-t)**2*p+2*(1-t)*t*c+t*t*q for t in [i/10 for i in range(1,10)]]+[q,b]

def body(mask,color,r):
    global parts
    parts=[];faces=[f for f in range(6) if mask&(1<<f)]
    if len(faces)<=1 or (len(faces)==2 and faces[0]//2==faces[1]//2):
        axis=faces[0]//2 if faces else 0;a=D[axis*2]
        sweep([C-a*.5,C+a*.5],r,color,(not(mask&(1<<(axis*2+1))),not(mask&(1<<(axis*2)))))
    elif len(faces)==2:
        a,b=[D[f] for f in faces];R=.27
        points=[C+a*.5,C+a*R]
        points += [C+R*(a+b)-R*(b*math.cos(t)+a*math.sin(t)) for t in [i*math.pi/24 for i in range(1,12)]]
        points += [C+b*R,C+b*.5]
        sweep(points,r,color)
    else:
        # Exact Boolean fusion keeps the visible branch saddle coherent and manifold.
        bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=r*1.06,location=C)
        core=finish(bpy.context.object,'Pipe branch casting',color)
        for f in faces:
            arm=sweep([C-D[f]*r*.3,C+D[f]*.5],r,color,(True,True))
            bpy.context.view_layer.objects.active=core
            mod=core.modifiers.new('Fused branch','BOOLEAN');mod.operation='UNION';mod.solver='EXACT';mod.object=arm
            bpy.ops.object.modifier_apply(modifier=mod.name)
            if not core.data.polygons:raise RuntimeError(f"Empty fused pipe mask {mask} face {f}")
            parts.remove(arm);bpy.data.objects.remove(arm,do_unlink=True)
        # Open connected ends after fusion. Internal caps must not bend the boundary
        # normals toward the pipe axis and produce false bright seams between cells.
        bm=bmesh.new();bm.from_mesh(core.data);caps=[]
        for polygon in bm.faces:
            points=[core.matrix_world@v.co for v in polygon.verts]
            if any(all(abs((p-C).dot(D[f])-.5)<.00001 for p in points) for f in faces):caps.append(polygon)
        bmesh.ops.delete(bm,geom=caps,context='FACES_ONLY');bm.normal_update();bm.to_mesh(core.data);bm.free()
        # Small brass collars identify actual T/cross/multi-axis branch fittings only.
        for f in faces:
            d=D[f];axis='X' if d.x else 'Y' if d.y else 'Z'
            ring('Branch ferrule',C+d*.17,r+.003,.008,3,axis)
        for p in core.data.polygons:p.use_smooth=True
    return merge(parts,'Mask'+str(mask).zfill(2))

def lead(mask,power):
    global parts
    parts=[];color=8 if power else 5;r=.024 if power else .017
    offset=Vector((.15,.15,.15))*(1 if power else -1);hub=C+offset
    faces=[f for f in range(6) if mask&(1<<f)]
    anchors=[C+D[f]*.5+offset-D[f]*offset.dot(D[f]) for f in faces]
    if not faces:box('Installed channel terminal',hub,(.065,.065,.065),color,.008)
    elif len(faces)==1:
        sweep([hub,anchors[0]],r,color,(True,False))
    elif len(faces)==2:sweep(rounded([anchors[0],hub,anchors[1]]),r,color)
    else:
        box('Channel branch terminal',hub,(r*2.4,)*3,color,.008)
        for a in anchors:sweep([hub,a],r,color,(False,False))
    return merge(parts,'Mask'+str(mask).zfill(2))

families={};report={}
for key,color,r,power in [('item_pipe',1,.075,None),('fluid_pipe',2,.063,None),('power_cable',8,.085,None),('signal_conduit',5,.045,None),('pipe_signal_addition',0,0,False),('pipe_power_addition',0,0,True)]:
    groups=[]
    for mask in range(64):groups.append(body(mask,color,r) if power is None else lead(mask,power))
    bpy.ops.object.select_all(action='DESELECT')
    for o in groups:o.select_set(True)
    bpy.context.view_layer.objects.active=groups[0]
    bpy.ops.export_scene.fbx(filepath=str(OUT/(key+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    report[key]={'masks':64,'trianglesByMask':[sum(len(p.vertices)-2 for p in o.data.polygons) for o in groups],'materials':1}
    families[key]=groups
    # Names must remain Mask00..63 in each export; isolate the editable source families afterwards.
    collection=bpy.data.collections.new(key);scene.collection.children.link(collection)
    for o in groups:
        for old in list(o.users_collection):old.objects.unlink(o)
        collection.objects.link(o);o.name=key+'_'+o.name;o.hide_render=True

scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.world.color=(.18,.18,.18);scene.render.image_settings.file_format='PNG'
bpy.ops.object.camera_add(location=(2.3,3.1,2.0));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.45
camera.rotation_euler=(C-camera.location).to_track_quat('-Z','Y').to_euler()
for p,power,size in [((1,3,5),600,4),((-3,2,2),450,3),((1,-3,3),700,3)]:
    bpy.ops.object.light_add(type='AREA',location=p);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(C-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100;scene.render.film_transparent=True
for key,id in [('item_pipe',146),('fluid_pipe',147),('power_cable',138),('signal_conduit',132)]:
    o=families[key][0];o.hide_render=False;scene.render.filepath=str(OUT/'Icons'/(str(id)+'.png'));bpy.ops.render.render(write_still=True);o.hide_render=True
# Source evidence: two parallel material rows, 3-cell straight, elbow, T, planar cross,
# vertical bend and full six-way branch. All are actual exported meshes.
for row,key in enumerate(['fluid_pipe','item_pipe']):
    for col,mask in enumerate([3,17,19,51,5,63]):
        o=families[key][mask];o.hide_render=False;o.location=(col*1.55+(1.4 if col else 0),row*1.8,0)
        if mask==3:
            for side in [-1,1]:
                copy=o.copy();copy.data=o.data;scene.collection.objects.link(copy);copy.location=o.location+Vector((side,0,0));copy.hide_render=False
camera.location=(11,15,13);target=Vector((5.2,1.3,.4));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=14
scene.render.film_transparent=False;scene.world.color=(.08,.10,.13);scene.render.resolution_x=2000;scene.render.resolution_y=1100
scene.render.filepath=str(SOURCE/'connected-pipes-review.png');bpy.ops.render.render(write_still=True)
for im in bpy.data.images:
    if im.source=='FILE':im.filepath=bpy.path.relpath(im.filepath,start=str(SOURCE))
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'ConnectedPipes.blend'))
(SOURCE/'geometry-report.json').write_text(json.dumps(report,indent=2))
