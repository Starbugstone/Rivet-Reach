"""Original Rivet Reach industrial kit. Run with Blender --background --python.
Metric one-cell kit; +Y is front in Blender / -Z in Unity. Named moving pivots.
"""
import bpy, math, os, json, random
from mathutils import Vector
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/RivetReach/Resources/Industry'; SOURCE=ROOT/'ArtSource/Industry'
OUT.mkdir(parents=True,exist_ok=True);(OUT/'Icons').mkdir(exist_ok=True);SOURCE.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
palette=[(.055,.085,.105),(.14,.20,.23),(.52,.28,.09),(.82,.57,.22),(.20,.105,.045),(.015,.47,.70),(.10,.92,1),(.84,.76,.52),(.018,.027,.03),(.72,.24,.045),(.75,.81,.78),(.24,.36,.32),(.10,.14,.18),(.12,.34,.53),(.34,.16,.06),(.55,.62,.63)]
# Original shared PBR atlas: small mottling and edge wear, no downloaded textures.
for name,surface in [('Atlas',False),('Surface',True),('Emission',False)]:
    im=bpy.data.images.new(name,width=256,height=256);pixels=[];rng=random.Random(414)
    for y in range(256):
        for x in range(256):
            slot=x//64+(y//64)*4;c=palette[slot];noise=rng.uniform(.94,1.04)
            if name=='Surface':c=(.78 if slot in [2,3,4,10,14,15] else .25,)*3;alpha=.48 if slot in [2,3,10] else .27
            elif name=='Emission':c=tuple(v*(.5 if slot==6 else .18 if slot==5 else 0) for v in c);alpha=1
            else:c=tuple(min(1,v*noise) for v in c);alpha=1
            pixels.extend((*c,alpha))
    im.pixels[:]=pixels;im.filepath_raw=str(OUT/(name+'.png'));im.file_format='PNG';im.save()
mat=bpy.data.materials.new('WorkshopAtlas');mat.use_nodes=True
nodes=mat.node_tree.nodes;bs=nodes.get('Principled BSDF');bs.inputs['Metallic'].default_value=.65;bs.inputs['Roughness'].default_value=.32
tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images['Atlas'];mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
em=nodes.new('ShaderNodeTexImage');em.image=bpy.data.images['Emission'];mat.node_tree.links.new(em.outputs['Color'],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=.6
parts=[];motion={};root=None

def finish(o,name,color,bevel=0,moving=None):
    o.name=name
    bpy.context.view_layer.objects.active=o;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Machined edge radius','BEVEL');mod.width=bevel;mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if o.type=='MESH':
        o.data.materials.clear();o.data.materials.append(mat)
        if not o.data.uv_layers:o.data.uv_layers.new(name='UVMap')
        uv=o.data.uv_layers.active;uv.name='UVMap'
        # Coherent atlas UV per face with stable texture density.
        for poly in o.data.polygons:
            for li in poly.loop_indices:
                co=o.data.vertices[o.data.loops[li].vertex_index].co
                uv.data[li].uv=((color%4+.15+(co.x*3+.5)%1*.7)/4,(color//4+.15+(co.z*3+.5)%1*.7)/4)
        for poly in o.data.polygons:poly.use_smooth=len(poly.vertices)==4 and name.startswith(('Shell','Pipe','Rim','Axle'))
    parts.append(o)
    if moving:motion.setdefault(moving,[]).append(o)
    return o

def box(name,loc,scale,c=1,b=.015,moving=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=scale;return finish(o,name,c,b,moving)
def cyl(name,loc,r,depth,c=2,axis='Z',verts=20,moving=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=depth,location=loc);o=bpy.context.object
    if axis=='X':o.rotation_euler[1]=math.pi/2
    if axis=='Y':o.rotation_euler[0]=math.pi/2
    return finish(o,name,c,.004,moving)
def ring(name,loc,r,minor,c=3,axis='Z',moving=None):
    bpy.ops.mesh.primitive_torus_add(major_radius=r,minor_radius=minor,major_segments=16,minor_segments=5,location=loc);o=bpy.context.object
    if axis=='X':o.rotation_euler[1]=math.pi/2
    if axis=='Y':o.rotation_euler[0]=math.pi/2
    return finish(o,name,c,0,moving)
def bolt(loc,axis='Z',r=.016):return cyl('Hex fastener',loc,r,.018,3,axis,6)
def tube(name,points,r,c=2):
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.bevel_depth=r;curve.bevel_resolution=1;curve.resolution_u=1
    spline=curve.splines.new('POLY');spline.points.add(len(points)-1)
    for p,co in zip(spline.points,points):p.co=(*co,1)
    o=bpy.data.objects.new(name,curve);scene.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');return finish(bpy.context.object,name,c)
def feet():
    box('Cast plinth',(.5,.5,.075),(.90,.88,.13),0,.025)
    box('Brass foundation reveal',(.5,.5,.147),(.85,.83,.025),2,.005)
    for x in [.13,.87]:
        for y in [.15,.85]:bolt((x,y,.16),r=.025)
def gauge(x,y,z,r=.067):
    cyl('Gauge rim',(x,y,z),r,.03,3,'Y');cyl('Ivory dial',(x,y+.019,z),r*.79,.008,7,'Y')
    for a in [-120,-60,0,60,120]:
        a=math.radians(a);box('Dial graduation',(x+math.sin(a)*r*.60,y+.027,z+math.cos(a)*r*.60),(.008,.004,.013),0,0)
    o=box('Dial needle',(x+.012,y+.03,z+.012),(.008,.006,r*.9),9,.001);o.rotation_euler[1]=-.6

def port(kind,face):
    pos={'power':(.5,.012,.47),'signal':(.70,.984,.32),'itemin':(.013,.50,.48),'itemout':(.987,.50,.48),'fluidin':(.012,.5,.48),'fluidout':(.987,.5,.48)}[kind]
    x,y,z=pos
    if kind=='power':tube('Power lead support',[(x,y+.02,z),(x,.25,z)],.067,0)
    elif kind=='signal':tube('Control lead',[(x,y-.02,z),(x,.69,z)],.023,5)
    elif kind.startswith('item'):box('Item transfer sleeve',(.13 if kind=='itemin' else .87,y,z),(.25,.16,.16),1,.009)
    else:tube('Nozzle neck',[(x,y,z),(.22 if kind=='fluidin' else .78,y,z)],.047,2)
    axis='Y' if kind in ['power','signal'] else 'X'
    if kind=='power':
        cyl('Power socket',(x,y,z),.119,.032,8,axis,12);ring('Power socket hex rim',(x,y-.012,z),.093,.018,3,axis)
        # Three brass contacts: recognisable without colour.
        for dx,dz in [(-.035,.02),(.035,.02),(0,-.04)]:cyl('Power contact',(x+dx,y-.025,z+dz),.012,.012,3,axis,8)
    elif kind=='signal':
        tube('Signal riser',[(.5,.985,.045),(.5,.985,.48),(.70,.985,.32)],.012,5)
        box('Square signal terminal',(x,y-.013,z),(.13,.04,.13),0,.012)
        cyl('Blue control key',(x,y+.006,z),.04,.015,6,axis,8)
    elif kind.startswith('item'):
        box('Square item flange',(x,y,z),(.025,.25,.25),2,.016);box('Item throat',(x,y,z),(.032,.17,.17),8,.01)
        for dy in [-.105,.105]:
            for dz in [-.105,.105]:bolt((x,y+dy,z+dz),'X',.012)
    else: ring('Nozzle union',(x,y,z),.067,.022,3,axis);cyl('Water nozzle',(x,y,z),.052,.035,13,axis)

def flywheel(x,y,z,r=.22,axis='X',name='MotionSpin'):
    ring('Flywheel rim',(x,y,z),r,.025,3,axis,name);cyl('Axle hub',(x,y,z),.055,.11,2,axis,moving=name)
    for a in range(0,180,45):
        o=box('Flywheel spoke',(x,y,z),(.025,r*1.8,.023) if axis=='X' else (r*1.8,.025,.023),3,.003,name)
        if axis=='X':o.rotation_euler[0]=math.radians(a)
        else:o.rotation_euler[1]=math.radians(a)

def machine(id,key):
    global parts,motion,root
    bpy.ops.object.select_all(action='DESELECT');parts=[];motion={}
    root=bpy.data.objects.new(key,None);scene.collection.objects.link(root)
    if id in [120,121,122,123,124,125,126,127,128,150,151,152]:
        if id in [120,121]:
            if id==120:
                box('Veined stone',(.5,.5,.5),(.9,.9,.9),12,.03)
                # Embedded crystals reach the visible stone faces instead of hiding inside its volume.
                for a,b in [(.22,.35),(.40,.48),(.57,.62),(.74,.76),(.62,.25)]:
                    box('Top cyan vein',(a,b,.953),(.13,.055,.012),6,.008)
                    box('Front cyan vein',(a,.953,b),(.11,.012,.065),5,.008)
                    box('Side cyan vein',(.953,a,b),(.012,.07,.12),6,.008)
            for x,y,z,r in [(.40,.46,.40,.18),(.63,.50,.33,.12),(.34,.66,.25,.09)]:
                bpy.ops.mesh.primitive_cone_add(vertices=5,radius1=r,radius2=0,depth=.50,location=(x,y,z+.23));finish(bpy.context.object,'Azure crystal tip',6,.004)
                cyl('Crystal prism',(x,y,z),r,.35,5,verts=5)
        elif id==122:
            cyl('Spool core',(.5,.5,.4),.15,.52,4)
            for z in [.15,.65]:cyl('Spool flange',(.5,.5,z),.32,.05,1)
            points=[]
            for i in range(129):
                a=i/128*math.pi*16;points.append((.5+math.cos(a)*.24,.5+math.sin(a)*.24,.21+i/128*.36))
            tube('Copper wire winding',points,.026,2)
        elif id in [123,124]:
            for z in [.10,.15,.20]:box('Sheet metal',(.5,.5,z),(.70,.57,.033),2 if id==123 else 10,.009)
        elif id==125:
            ring('Cog ring',(.5,.5,.18),.24,.065,10)
            for i in range(12):
                a=i/12*math.pi*2;o=box('Gear tooth',(.5+math.cos(a)*.30,.5+math.sin(a)*.30,.18),(.12,.07,.10),10,.007);o.rotation_euler[2]=a
        elif id==126:
            for x,y in [(.35,.4),(.6,.45),(.4,.65)]:cyl('Rivet shank',(x,y,.24),.045,.28,10,verts=10);cyl('Rivet head',(x,y,.4),.085,.05,10,verts=10)
        elif id==127:
            for x in [.15,.85]:
                for y in [.15,.85]:box('Frame post',(x,y,.45),(.09,.09,.73),1,.009)
            for z in [.1,.8]:
                for x in [.15,.85]:box('Frame beam',(x,.5,z),(.09,.79,.09),2,.007)
                for y in [.15,.85]:box('Frame beam',(.5,y,z),(.79,.09,.09),2,.007)
        elif id==128:box('Glass panel',(.5,.5,.25),(.69,.065,.44),13,.012);box('Glass gleam',(.38,.54,.30),(.018,.01,.25),10,.002)
        else:
            for x,y,z in [(.3,.4,.15),(.6,.5,.2),(.42,.65,.15),(.65,.3,.11)]:
                bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=.17,location=(x,y,z));finish(bpy.context.object,'Crushed mineral',2 if id==150 else 15 if id==151 else 3)
    elif id==130:
        feet();box('Workbench top',(.5,.5,.69),(.94,.88,.11),4,.022)
        for x in [.16,.84]:box('Forged trestle',(x,.5,.4),(.11,.73,.52),1,.01)
        box('Tool drawer',(.5,.74,.54),(.50,.19,.15),1);box('Drawer brass pull',(.5,.855,.54),(.20,.03,.025),3)
        box('Anvil waist',(.31,.43,.80),(.21,.27,.16),1);box('Anvil face',(.31,.43,.91),(.35,.32,.07),10)
        cyl('Wire spool',(.75,.4,.81),.11,.14,2);ring('Copper coil',(.75,.4,.90),.10,.02,3)
        for x in [.57,.63,.69]:box('Bench tool',(x,.70,.765),(.028,.22,.025),3,.003)
    elif id in [140,141,142,143,144,145]:
        feet()
        if id==140:
            cyl('Shell boiler',(.42,.46,.51),.28,.66,2)
            for z in [.23,.76]:ring('Rim boiler band',(.42,.46,z),.282,.025,3)
            cyl('Shell crown',(.42,.46,.86),.20,.06,1)
            cyl('Chimney',(.32,.38,.935),.057,.12,0);ring('Chimney lip',(.32,.38,.982),.059,.009,3)
            box('Firebox',(.43,.78,.32),(.33,.22,.27),0,.035)
            for x in [.32,.375,.43,.485,.54]:box('Fire grate',(x,.903,.31),(.025,.025,.14),3,.002)
            box('Furnace glow',(.43,.888,.31),(.24,.009,.13),9,.003)
            gauge(.43,.755,.60);flywheel(.82,.48,.48,.24)
            cyl('Steam piston',(.74,.66,.63),.06,.22,10,moving='MotionPiston')
            tube('Steam return',[(.56,.43,.82),(.73,.43,.82),(.73,.62,.82),(.73,.62,.70)],.025,3)
            cyl('Shaft coupling',(.958,.48,.48),.068,.075,3,'X');tube('Water inlet neck',[(.50,.012,.47),(.50,.25,.47)],.052,2)
            ring('Rear water union',(.50,.025,.47),.065,.02,3,'Y');cyl('Water inlet bore',(.50,.012,.47),.05,.012,13,'Y')
        elif id==141:
            cyl('Shell alternator',(.49,.47,.48),.29,.65,0,'X',24)
            for x in [.22,.36,.50,.64,.78]:ring('Rim winding band',(x,.47,.48),.277,.022,2,'X')
            cyl('Axle intake',(.065,.47,.48),.062,.12,10,'X');ring('Shaft cog',(.11,.47,.48),.085,.019,3,'X')
            cyl('Rotor end',(.84,.47,.48),.20,.04,1,'X',24,moving='MotionSpin')
            for a in range(0,360,60):
                a=math.radians(a);box('Cooling slot',(.875,.47+math.sin(a)*.13,.48+math.cos(a)*.13),(.009,.04,.04),8,.002)
            box('Terminal crown',(.52,.48,.84),(.33,.35,.16),1,.018);gauge(.51,.67,.84,.057)
            port('power',0)
        elif id==142:
            box('Crusher body',(.5,.48,.44),(.71,.63,.55),1,.035)
            for x in [.18,.82]:box('Brass corner armor',(x,.79,.47),(.06,.04,.47),3,.008)
            # Tapered hollow feed hopper, constructed as continuous wall strips.
            verts=[];faces=[]
            for z,r in [(.70,.24),(.94,.38),(.94,.33),(.71,.19)]:
                for x,y in [(-1,-1),(1,-1),(1,1),(-1,1)]:verts.append((.5+x*r,.45+y*r,z))
            for j in range(3):
                for k in range(4):faces.append((j*4+k,j*4+(k+1)%4,(j+1)*4+(k+1)%4,(j+1)*4+k))
            mesh=bpy.data.meshes.new('Hopper shell');mesh.from_pydata(verts,[],faces);o=bpy.data.objects.new('Tapered hopper',mesh);scene.collection.objects.link(o);finish(o,'Tapered hopper',2,.007)
            for x in [.4,.6]:
                cyl('Crushing roller',(x,.45,.72),.075,.37,10,'Y',12,moving='MotionSpin'+('B' if x>.5 else ''))
                for y in [.31,.4,.49,.58]:ring('Roller teeth',(x,y,.72),.075,.012,0,'Y',moving='MotionSpin'+('B' if x>.5 else ''))
            gauge(.38,.817,.53);box('Service plate',(.63,.80,.48),(.17,.025,.22),0)
            for z in [.43,.48,.53]:box('Cooling louvre',(.63,.82,z),(.12,.009,.013),3,.002)
            port('itemin',0);port('itemout',0);port('power',0);port('signal',0)
        elif id==143:
            cyl('Shell pump volute',(.45,.50,.43),.24,.25,2,'Y')
            ring('Volute rim',(.45,.66,.43),.21,.023,3,'Y')
            flywheel(.45,.67,.43,.13,'Y')
            cyl('Motor body',(.46,.27,.48),.18,.28,1,'Y')
            tube('Outlet elbow',[(.48,.50,.68),(.72,.50,.68),(.80,.50,.50),(.97,.50,.48)],.053,2)
            tube('Source intake',[(.45,.5,.30),(.5,.5,.08)],.062,1)
            gauge(.73,.73,.62,.055);port('power',0);port('signal',0);port('fluidout',0)
        elif id==144:
            for x in [.19,.81]:box('Guide rail',(x,.48,.57),(.08,.15,.78),10,.009)
            box('Motor carriage',(.5,.48,.80),(.58,.43,.29),1,.025)
            for z in [.74,.81,.88]:box('Motor cooling fin',(.5,.48,z),(.63,.48,.025),2,.005)
            cyl('Auger core',(.5,.48,.43),.065,.46,10,moving='MotionSpin')
            points=[]
            for i in range(73):
                a=i/72*math.pi*6;points.append((.5+math.cos(a)*.13,.48+math.sin(a)*.13,.20+i/72*.43))
            o=tube('Helical cutting flight',points,.027,3);motion.setdefault('MotionSpin',[]).append(o)
            gauge(.5,.718,.81,.061);port('power',0);port('signal',0);port('itemout',0)
        else:
            cyl('Shell riveted tank',(.5,.5,.54),.32,.69,1,verts=24)
            for z in [.23,.56,.87]:ring('Tank strap',(.5,.5,z),.321,.021,3)
            cyl('Tank lid',(.5,.5,.91),.23,.045,2);ring('Fill hatch',(.5,.5,.947),.105,.02,3)
            box('Water sight surround',(.5,.826,.52),(.12,.04,.42),0)
            box('Water sight tube',(.5,.851,.52),(.065,.015,.34),13,.006)
            for z in [.38,.45,.52,.59,.66]:box('Sight graduation',(.56,.851,z),(.025,.01,.009),7,.001)
            port('fluidin',0);port('fluidout',0)
    elif id in [131,132,138,146,147]:
        height=.045 if id==131 else .48
        c=6 if id==131 else 5 if id==132 else 0 if id==138 else 1 if id==146 else 2
        box('Junction foot',(.5,.5,.027),(.24,.24,.045),0,.01)
        if id==131:
            box('Wire junction',(.5,.5,height),(.16,.16,.038),c,.01)
        else:
            cyl('Central union',(.5,.5,height),.115,.23,c,'X',12)
            for x in [.40,.60]:ring('Union collar',(x,.5,height),.115,.015,3,'X')
        # arms are separate named meshes, toggled from connection masks. +X, -X, +Z, -Z, -Y, +Y
        for f,(dx,dy,dz) in enumerate([(1,0,0),(-1,0,0),(0,0,1),(0,0,-1),(0,-1,0),(0,1,0)]):
            start=len(parts)
            if id==131:
                if dz:continue
                box('Exposed conductor',(.5+dx*.25,.5+dy*.25,height),(.48 if dx else .065,.48 if dy else .065,.025),6,.005)
                box('Insulator pin',(.5+dx*.42,.5+dy*.42,.018),(.09,.09,.025),2,.004)
            else:
                axis='X' if dx else 'Y' if dy else 'Z';r=.085 if id==138 else .063
                cyl('Pipe span',(.5+dx*.27,.5+dy*.27,height+dz*.25),r,.45,c,axis,12)
                ring('End union',(.5+dx*.46,.5+dy*.46,height+dz*.44),r+.008,.014,3,axis)
                if id==132:
                    box('Cyan inset',(.5+dx*.24,.5+dy*.24,height+.057+dz*.20),(.24 if dx else .035,.24 if dy else .035,.026 if not dz else .25),6,.003)
            motion['Arm'+str(f)]=parts[start:]
    elif id in [133,134,135,136,148,149]:
        box('Control foot',(.5,.5,.065),(.52,.48,.12),0,.018);box('Brass mounting frame',(.5,.5,.135),(.46,.42,.025),3,.006)
        for x in [.31,.69]:
            for y in [.33,.67]:bolt((x,y,.16))
        if id==134:
            cyl('Lever pivot',(.5,.5,.24),.075,.25,10,'X')
            o=box('Lever handle',(.5,.5,.40),(.065,.065,.32),4,.013,'MotionLever')
            box('Lever grip',(.5,.5,.56),(.105,.11,.11),2,.02,'MotionLever')
        elif id==135:
            box('Button bezel',(.5,.5,.20),(.30,.28,.12),1,.013)
            box('Azure button',(.5,.5,.28),(.22,.20,.07),6,.015,'MotionPiston')
        elif id==136:
            cyl('Pilot housing',(.5,.5,.24),.11,.19,2,verts=12);cyl('Blue pilot lens',(.5,.5,.35),.085,.04,6,verts=12)
            ring('Lens guard',(.5,.5,.375),.085,.009,3)
        elif id==133:
            box('Relay case',(.5,.5,.25),(.35,.33,.23),1,.018)
            box('Relay blue cap',(.5,.5,.39),(.22,.22,.055),6,.011)
            for y in [.23,.77]:cyl('Relay connector',(.5,y,.18),.045,.16,3,'Y',12)
            box('Direction arrow stem',(.5,.6,.42),(.018,.13,.015),7,.001)
        elif id==148:
            box('Extractor housing',(.5,.5,.27),(.44,.35,.23),1,.02);flywheel(.5,.69,.28,.09,'Y');port('itemin',0);port('itemout',0);port('signal',0)
        else:
            box('Sensor shell',(.5,.5,.25),(.30,.28,.20),1,.015);cyl('Sensor lens',(.5,.66,.25),.07,.07,6,'Y');port('signal',0)
    elif id==137:
        for x in [.10,.90]:box('Hatch jamb',(x,.52,.50),(.10,.15,.98),1,.012)
        box('Hatch lintel',(.5,.52,.95),(.86,.15,.09),3,.01)
        box('Armored hatch',(.5,.52,.47),(.67,.07,.84),0,.02,'MotionHatch')
        for z in [.22,.71]:box('Hatch cross brace',(.5,.573,z),(.64,.025,.05),3,.005,'MotionHatch')
        box('Inset window',(.5,.565,.59),(.28,.014,.19),13,.014,'MotionHatch');port('signal',0)
    elif id==139:
        feet();cyl('Lamp column',(.5,.5,.39),.057,.47,2)
        cyl('Lantern base',(.5,.5,.61),.19,.06,3,verts=8)
        cyl('Warm lamp core',(.5,.5,.77),.12,.28,7,verts=8)
        cyl('Lantern crown',(.5,.5,.94),.19,.06,1,verts=8)
        for a in range(0,360,90):
            a=math.radians(a);box('Lantern cage',(.5+math.cos(a)*.14,.5+math.sin(a)*.14,.78),(.025,.025,.29),3,.005)
        port('power',0);port('signal',0)
    if id in [139,140,141,142,143,144,145,148]:
        box('Status bezel',(.28,.85,.28),(.09,.03,.055),0,.005)
        box('Status pilot',(.28,.87,.28),(.055,.009,.024),6,.004,'StatusLight')
    # Merge static geometry and each independently moving assembly: shared atlas, one draw per part.
    used={o for group in motion.values() for o in group};groups={'Body':[o for o in parts if o not in used],**motion}
    merged=[]
    for name,objects in groups.items():
        if not objects:continue
        bpy.ops.object.select_all(action='DESELECT')
        for o in objects:o.select_set(True)
        bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
        # Pivot at assembly center, hatch retracts vertically within its footprint.
        scene.cursor.location=(.5,.52,.47) if name=='MotionHatch' else (.5,.5,.24) if name=='MotionLever' else sum((v.co for v in o.data.vertices),Vector())/len(o.data.vertices) if False else o.location
        if name.startswith('MotionSpin'):
            scene.cursor.location=(.82,.48,.48) if id==140 else (.84,.47,.48) if id==141 else ((.4 if name=='MotionSpin' else .6),.45,.72) if id==142 else (.45,.67,.43) if id==143 else (.5,.48,.43)
        bpy.ops.object.origin_set(type='ORIGIN_CURSOR');o.parent=root;merged.append(o)
        if name.startswith('Motion'):
            o.rotation_mode='XYZ';o.keyframe_insert(data_path='rotation_euler',frame=1);o.keyframe_insert(data_path='location',frame=1);o.keyframe_insert(data_path='scale',frame=1)
            if name.startswith('MotionSpin'):o.rotation_euler[0 if id in [140,141] else 1 if id in [142,143] else 2]=2*math.pi
            elif name=='MotionLever':o.rotation_euler[0]=.55
            elif name=='MotionPiston':o.location.z+=.06
            elif name=='MotionHatch':o.scale.z=.08;o.keyframe_insert(data_path='scale',frame=61)
            o.keyframe_insert(data_path='rotation_euler',frame=61);o.keyframe_insert(data_path='location',frame=61)
    scene.frame_set(1)
    for o in merged:o.select_set(True)
    root.select_set(True);bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(OUT/(key+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
    tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in merged)
    return root,merged,tris

assets=[(120,'azure_ore'),(121,'azure_crystal'),(122,'copper_wire'),(123,'copper_plate'),(124,'iron_plate'),(125,'cog'),(126,'rivets'),(127,'machine_casing'),(128,'glass'),(150,'crushed_copper'),(151,'crushed_iron'),(152,'crushed_gold'),(130,'machinist_bench'),(131,'signal_wire'),(132,'signal_conduit'),(133,'signal_relay'),(134,'lever'),(135,'button'),(136,'signal_indicator'),(137,'workshop_hatch'),(138,'power_cable'),(139,'workshop_lamp'),(140,'boiler_engine'),(141,'alternator'),(142,'crusher'),(143,'pump'),(144,'drill'),(145,'water_tank'),(146,'item_pipe'),(147,'fluid_pipe'),(148,'extractor'),(149,'inventory_sensor')]
all_assets=[];report={}
for id,key in assets:
    r,objects,tris=machine(id,key);all_assets.append((id,r,objects));report[key]={'triangles':tris,'meshParts':len(objects),'materials':1}
    r.hide_render=True
    for o in objects:o.hide_render=True
# Studio for evidence and icons.
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.world.color=(.16,.16,.16);scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
bpy.ops.object.camera_add(location=(2.0,2.5,1.8));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.6
camera.rotation_euler=(Vector((.5,.5,.48))-camera.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((1,3,4),450,4),((-3,1,2),300,3),((1,-3,3),550,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((.5,.5,.4))-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100
for id,r,objects in all_assets:
    r.hide_render=False
    for o in objects:o.hide_render=False
    # Unconnected routes display the straight pair for their inventory icons.
    for o in objects:
        if o.name.startswith('Arm') and o.name not in ['Arm0','Arm1']:o.hide_render=True
    scene.render.filepath=str(OUT/'Icons'/(str(id)+'.png'));bpy.ops.render.render(write_still=True)
    r.hide_render=True
    for o in objects:o.hide_render=True
# Arrange real mesh review, no concepts or painted substitutes.
for i,(id,r,objects) in enumerate(all_assets):
    r.hide_render=False;r.location=((i%5)*1.4,(i//5)*1.5,0)
    for o in objects:o.hide_render=False
    for o in objects:
        if o.name.startswith('Arm') and o.name.split('.')[0] not in ['Arm0','Arm1']:o.hide_render=True
camera.location=(12,18,16);target=Vector((3.3,4.8,.2));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=14
scene.render.film_transparent=False;scene.world.color=(.07,.09,.11);scene.render.resolution_x=1800;scene.render.resolution_y=1500
scene.render.filepath=str(SOURCE/'industry-kit-review.png');bpy.ops.render.render(write_still=True)
scene.frame_end=61
# Store relative textures and the complete editable workshop collection.
for im in bpy.data.images:
    if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=str(SOURCE))
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'WorkshopKit.blend'))
(SOURCE/'geometry-report.json').write_text(json.dumps(report,indent=2))
print('INDUSTRY_ASSETS_COMPLETE',json.dumps(report))
