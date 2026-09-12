"""Original starter stations, matched to the Machinist's Bench atlas and metre grid.
Blender --background --python Tools/create_starter_station_assets.py.
Only writes the three starter exports/icons and ArtSource/Stations; shared art is read-only.
"""
import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/RivetReach/Resources/Industry'
SOURCE = ROOT / 'ArtSource/Stations'
SOURCE.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'; scene.unit_settings.scale_length = 1
mat = bpy.data.materials.new('WorkshopAtlas'); mat.use_nodes = True
bs = mat.node_tree.nodes.get('Principled BSDF')
bs.inputs['Metallic'].default_value = .45; bs.inputs['Roughness'].default_value = .42
tex = mat.node_tree.nodes.new('ShaderNodeTexImage'); tex.image = bpy.data.images.load(str(OUT / 'Atlas.png'))
mat.node_tree.links.new(tex.outputs['Color'], bs.inputs['Base Color'])
parts = []
def finish(o, name, slot, bevel=.006):
    o.name = name; bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bm=bmesh.new();bm.from_mesh(o.data);volume=bm.calc_volume(signed=True);bm.free()
    assert volume>0, (name, 'Inward or degenerate source surface', volume)
    if bevel:
        mod = o.modifiers.new('Soft worked edges', 'BEVEL'); mod.width = bevel; mod.segments = 1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    o.data.materials.clear(); o.data.materials.append(mat)
    if not o.data.uv_layers: o.data.uv_layers.new(name='UVMap')
    o.data.uv_layers.active.name = 'UVMap'
    for p in o.data.polygons:
        for li in p.loop_indices:
            co = o.data.vertices[o.data.loops[li].vertex_index].co
            o.data.uv_layers.active.data[li].uv = ((slot%4+.2+(co.x*2+.5)%1*.6)/4, (slot//4+.2+(co.z*2+.5)%1*.6)/4)
    parts.append(o); return o

def box(name, pos, size, slot=4, bevel=.006):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos); o=bpy.context.object; o.dimensions=size
    return finish(o,name,slot,bevel)
def cyl(name,pos,r,depth,slot=3,axis='Z',n=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=n, radius=r, depth=depth, location=pos); o=bpy.context.object
    if axis=='Y': o.rotation_euler[0]=math.pi/2
    if axis=='X': o.rotation_euler[1]=math.pi/2
    return finish(o,name,slot,.002)
def beam(name,a,b,width,slot=4):
    mid=(Vector(a)+Vector(b))/2; delta=Vector(b)-Vector(a)
    o=box(name,mid,(width,width,delta.length),slot,.004)
    o.rotation_euler=delta.to_track_quat('Z','Y').to_euler(); return o
def bolt(pos,axis='Y'): return cyl('Brass pin',pos,.012,.012,3,axis,6)
def handle(x,y,z,axis='Y'):
    # Folded forged pull, attached at both ends; negative space is real geometry.
    if axis=='Y':
        for dx in [-.075,.075]: cyl('Pull mounting boss',(x+dx,y,z),.027,.018,2,'Y'); beam('Pull standoff',(x+dx,y,z),(x+dx,y+.04,z-.04),.022,1)
        beam('Forged pull',(x-.075,y+.04,z-.04),(x+.075,y+.04,z-.04),.025,1)
    else:
        for dy in [-.075,.075]: cyl('Pull mounting boss',(x,y+dy,z),.027,.018,2,'X'); beam('Pull standoff',(x,y+dy,z),(x+.04,y+dy,z-.04),.022,1)
        beam('Forged pull',(x+.04,y-.075,z-.04),(x+.04,y+.075,z-.04),.025,1)

def chest():
    for x in [.14,.86]:
        for y in [.17,.83]: box('Iron foot',(x,y,.07),(.14,.14,.14),0,.018)
    box('Inset carcass',(.5,.5,.39),(.80,.70,.56),4,.015)
    for z in [.23,.405,.58]:
        for y in [.125,.875]: box('Horizontal oak boards',(.5,y,z),(.84,.055,.168),4 if z==.405 else 14,.008)
        for x in [.075,.925]: box('End grain boards',(x,.5,z),(.055,.73,.168),14 if z==.405 else 4,.007)
    for x in [.105,.895]:
        for y in [.145,.855]: box('Corner ironwork',(x,y,.4),(.075,.075,.57),1,.008)
    for z in [.135,.65]: box('Continuous dark rim',(.5,.5,z),(.92,.82,.048),0,.009)
    # Low pitched lid, individually laid boards with a chamfered edge.
    for i in range(5):
        y=.18+i*.16; z=.738+.045*(1-abs(y-.5)/.4)
        box('Lid plank',(.5,y,z),(.91,.154,.12),4 if i%2 else 14,.018)
    for x in [.23,.77]:
        for y,z in [(.18,.814),(.34,.832),(.5,.848),(.66,.832),(.82,.814)]:
            box('Lid strap',(x,y,z),(.062,.162,.018),1,.003)
        for y in [.104,.896]:
            box('Riveted iron strap',(x,y,.405),(.064,.024,.46),1,.004)
            for z in [.23,.42,.60]: bolt((x,y+(.015 if y>.5 else -.015),z))
        cyl('Rear hinge barrel',(x,.09,.69),.033,.115,2,'X')
    box('Latch plate',(.5,.922,.62),(.13,.032,.20),2,.012)
    box('Latch tongue',(.5,.944,.69),(.06,.022,.21),3,.006)
    cyl('Keyhole',(.5,.962,.585),.015,.008,8,'Y',10)
    handle(.5,.919,.40); handle(.940,.5,.45,'X')

def workbench():
    for x in [.16,.84]:
        for y in [.20,.80]:
            box('Oak trestle leg',(x,y,.38),(.12,.12,.70),4,.012)
            box('Iron boot',(x,y,.075),(.14,.14,.15),1,.012)
            for z in [.24,.62]: bolt((x,y+.069,z))
        beam('Diagonal trestle brace',(x,.22,.18),(x,.78,.61),.068,14)
        box('Trestle crossbar',(x,.5,.23),(.10,.70,.09),14)
    box('Lower stretcher',(.5,.5,.20),(.77,.09,.10),4)
    for y in [.29,.41,.53,.65]: box('Lower shelf plank',(.5,y,.275),(.71,.113,.055),14)
    for y in [.17,.83]: box('Apron',(.5,y,.645),(.85,.09,.15),4,.009)
    for i in range(5): box('Thick worktop plank',(.5,.16+i*.17,.755),(.94,.164,.11),14 if i%2 else 4,.01)
    for x in [.075,.925]:
        box('Bound end cap',(x,.5,.752),(.045,.90,.115),1,.006)
        for y in [.21,.79]: cyl('Countersunk pin',(x,y,.815),.015,.009,3)
    box('Joinery drawer',(.42,.88,.635),(.43,.06,.12),14,.008); handle(.42,.92,.64)
    # Small woodworker's vise, distinguishable from the industrial anvil.
    box('Vise jaw',(.79,.815,.835),(.22,.065,.075),1)
    box('Movable jaw',(.79,.947,.80),(.22,.045,.115),4)
    cyl('Vise screw',(.79,.913,.748),.019,.14,10,'Y')
    cyl('Vise hub',(.79,.981,.748),.028,.024,2,'Y')
    beam('Vise cross handle',(.72,.981,.72),(.86,.981,.776),.016,10)
    # Authored carpenter's mallet and square laid on the top.
    beam('Mallet shaft',(.26,.40,.835),(.47,.63,.835),.033,4)
    o=box('Mallet head',(.25,.39,.857),(.16,.07,.09),14,.013); o.rotation_euler[2]=-.72
    box('Try square stock',(.61,.30,.831),(.055,.26,.035),4)
    box('Try square blade',(.49,.195,.837),(.27,.026,.012),10,.002)
    for i in range(6): box('Rule tick',(.39+i*.032,.208,.845),(.003,.015,.003),0,0)

def furnace():
    box('Hearth foundation',(.5,.5,.065),(.94,.91,.13),0,.018)
    box('Hearth reveal',(.5,.5,.141),(.89,.86,.025),2,.005)
    # Seamed stone courses and solid rear; front opening remains recessed.
    for row in range(4):
        z=.245+row*.166
        for x in [.135,.865]:
            for j in range(3): box('Dressed stone side',(x,.24+j*.26,z),(.18,.25,.157),15 if (j+row)%3 else 12,.014)
        for j in range(3): box('Rear masonry',(.29+j*.21,.13,z),(.202,.14,.157),15 if (j+row)%3 else 12,.012)
    box('Sooty firebox',(.5,.34,.4),(.56,.065,.46),8,.01)
    box('Firebox floor',(.5,.62,.21),(.55,.57,.08),12,.008)
    # Radial arch stones: continuous wedge topology, not a painted opening.
    for i in range(7):
        a=i*math.pi/7+.014; b=(i+1)*math.pi/7-.014; verts=[]
        for y in [.75,.91]:
            for r,t in [(.215,a),(.34,a),(.34,b),(.215,b)]: verts.append((.5+math.cos(t)*r,y,.48+math.sin(t)*r))
        faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
        mesh=bpy.data.meshes.new('Arch wedge'); mesh.from_pydata(verts,[],[tuple(reversed(f)) for f in faces]);mesh.update()
        o=bpy.data.objects.new('Arch keystone' if i==3 else 'Arch voussoir',mesh);scene.collection.objects.link(o);finish(o,o.name,15 if i%2 else 12,.007)
    for x in [.22,.78]:
        box('Jamb',(x,.832,.35),(.12,.17,.27),15,.012)
        box('Iron corner tie',(x,.924,.32),(.10,.018,.18),1,.006)
        for z in [.27,.38]: bolt((x,.94,z))
    box('Ash drawer',(.5,.91,.186),(.43,.05,.085),1,.006); handle(.5,.944,.20)
    for x in [.32,.41,.5,.59,.68]:
        beam('Fire grate bar',(x,.858,.285),(x,.858,.425),.018,1)
    for x,y,z in [(.33,.72,.29),(.46,.69,.285),(.60,.73,.30),(.68,.66,.29),(.40,.60,.29),(.55,.59,.28)]:
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=.067,location=(x,y,z));finish(bpy.context.object,'Embers',9,0)
    for x in [.21,.50,.79]: box('Capstone',(x,.50,.882),(.28,.86,.105),15,.014)
    cyl('Short flue',(.65,.32,.939),.09,.084,0,'Z',12)
    cyl('Flue lip',(.65,.32,.978),.106,.026,1,'Z',12)
    cyl('Flue opening',(.65,.32,.993),.079,.004,8,'Z',12)

assets=[];report={}
for id,key,author in [(23,'workbench',workbench),(24,'furnace',furnace),(25,'chest',chest)]:
    parts=[]; author(); root=bpy.data.objects.new(key,None);scene.collection.objects.link(root);merged=[]
    for name,group in [('Body',[o for o in parts if not o.name.startswith('Embers')]),('Embers',[o for o in parts if o.name.startswith('Embers')])]:
        if not group: continue
        bpy.ops.object.select_all(action='DESELECT')
        for o in group:o.select_set(True)
        bpy.context.view_layer.objects.active=group[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
        scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');o.parent=root;merged.append(o)
    bpy.ops.object.select_all(action='DESELECT');root.select_set(True)
    for o in merged:o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(key+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    coords=[o.matrix_world@v.co for o in merged for v in o.data.vertices]
    report[key]={'triangles':sum(len(p.vertices)-2 for o in merged for p in o.data.polygons),'meshParts':len(merged),'materials':1,'min':[min(v[i] for v in coords) for i in range(3)],'max':[max(v[i] for v in coords) for i in range(3)]}
    assert all(-.001<=v[i]<=1.001 for v in coords for i in range(3)),(key,report[key])
    assets.append((id,root,merged))
    for o in merged:o.hide_render=True
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.world.color=(.16,.16,.16);scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
scene.view_settings.view_transform='AgX'
bpy.ops.object.camera_add(location=(2,2.6,1.9));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.48
camera.rotation_euler=(Vector((.5,.5,.46))-camera.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((1,3,4),450,4),((-3,1,2),300,3),((1,-3,3),550,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((.5,.5,.4))-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100
for id,r,objects in assets:
    for o in objects:o.hide_render=False
    scene.render.filepath=str(OUT/'Icons'/(str(id)+'.png'));bpy.ops.render.render(write_still=True)
    for o in objects:o.hide_render=True
for i,(id,r,objects) in enumerate(assets):
    r.location.x=i*1.35
    for o in objects:o.hide_render=False
camera.location=(4.9,5.3,3.4);camera.rotation_euler=(Vector((1.85,.5,.4))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=4.7
scene.render.resolution_x=1800;scene.render.resolution_y=1000;scene.render.film_transparent=False
scene.render.filepath=str(SOURCE/'starter-stations-review.png');bpy.ops.render.render(write_still=True)
for im in bpy.data.images:
    if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=str(SOURCE))
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'StarterStations.blend'))
(SOURCE/'geometry-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('STARTER_STATIONS_COMPLETE',json.dumps(report))
