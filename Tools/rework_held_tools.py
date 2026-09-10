"""Original close-view equipment: shaped steel, turned collars and bound handles.
Copyright (c) 2026 Starbugstone. No external asset inputs. Blender background CLI.
"""
import bpy, math, json
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/RivetReach/Resources/Characters';SOURCE=ROOT/'ArtSource/Characters'
def build(name):
    bpy.ops.wm.read_factory_settings(use_empty=True);parts=[]
    def finish(o,tile,bevel=0):
        bpy.context.view_layer.objects.active=o;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        if bevel:
            m=o.modifiers.new('Manufactured edge bevel','BEVEL');m.width=bevel;m.segments=3;bpy.ops.object.modifier_apply(modifier=m.name)
        uv=o.data.uv_layers.active or o.data.uv_layers.new();uv.name='SkinUV'
        for p in o.data.polygons:
            p.use_smooth=len(p.vertices)==4
            for li in p.loop_indices:uv.data[li].uv=((tile%4+.48)/4,(tile//4+.60)/4)
        parts.append(o);return o
    def cylinder(label,z,length,radius,tile):
        bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=radius,depth=length,location=(0,0,z));o=bpy.context.object;o.name=label;return finish(o,tile,.001)
    def shaped(label,profile,tile,bevel=.001):
        # Four-point diamond sections create convex blades with readable edge planes.
        vs=[]
        for x,z,width,depth in profile:vs.extend([(x-width,0,z),(x,-depth,z),(x+width,0,z),(x,depth,z)])
        fs=[(3,2,1,0)]
        for j in range(len(profile)-1):
            for i in range(4):fs.append((j*4+i,j*4+(i+1)%4,(j+1)*4+(i+1)%4,(j+1)*4+i))
        fs.append(tuple(range(len(vs)-4,len(vs))))
        data=bpy.data.meshes.new(label);data.from_pydata(vs,[],fs);data.update();o=bpy.data.objects.new(label,data);bpy.context.collection.objects.link(o);return finish(o,tile,bevel)
    def band(z):return cylinder('Bound leather grip',z,.012,.019,9)
    if name=='GripSword':
        cylinder('Leather grip core',0,.178,.0176,9)
        for j in range(13):band(-.078+j*.013)
        cylinder('Pommel collar',-.092,.018,.021,12);cylinder('Faceted pommel',-.111,.028,.026,15)
        cylinder('Guard seat',.092,.014,.023,12)
        cylinder('Blade tang and collar',.119,.060,.020,15)
        shaped('Swept crossguard',[(-.085,.101,.005,.005),(-.069,.112,.012,.012),(0,.119,.025,.018),(.069,.112,.012,.012),(.085,.101,.005,.005)],12)
        shaped('Forged blade',[(0,.13,.029,.010),(0,.18,.030,.009),(0,.40,.024,.007),(0,.49,.014,.004),(0,.545,.0005,.0005)],15)
        # Recessed fuller flanked by two raised polished edges.
        for s in [-1,1]:
            shaped('Blade fuller ridge',[(s*.010,.185,.0013,.0092),(s*.009,.38,.0011,.0077),(s*.005,.445,.0005,.005)],15,0)
    else:
        length=.53 if name=='GripPickaxe' else .59
        cylinder('Shaped ash shaft',.065,length,.0178,8)
        for j in range(23):band(-.12+j*.014)
        cylinder('Brass butt ring',.065-length/2,.023,.020,12)
        cylinder('Steel head ferrule',.335 if name=='GripPickaxe' else .285,.095 if name=='GripPickaxe' else .065,.024,15)
        cylinder('Ferrule rolled lip',.373 if name=='GripPickaxe' else .312,.010,.026,12)
        if name=='GripPickaxe':
            # Curved pointed pick and a broader chisel end. Profile runs along X.
            profile=[(-.238,.270,.0015,.002),(-.19,.322,.009,.009),(-.11,.356,.017,.021),(0,.37,.027,.032),(.11,.356,.019,.024),(.19,.322,.011,.012),(.235,.294,.003,.009)]
            vs=[]
            for x,z,d,h in profile:vs.extend([(x,-d,z-h),(x,d,z-h),(x,d,z+h),(x,-d,z+h)])
            fs=[(3,2,1,0)]+[(j*4+i,j*4+(i+1)%4,(j+1)*4+(i+1)%4,(j+1)*4+i) for j in range(len(profile)-1) for i in range(4)]+[tuple(range(len(vs)-4,len(vs)))]
            data=bpy.data.meshes.new('Forged pick head');data.from_pydata(vs,[],fs);data.update();o=bpy.data.objects.new('Forged pick head',data);bpy.context.collection.objects.link(o);finish(o,15,.002)
            cylinder('Head pin',.393,.012,.010,12)
        elif name=='GripShovel':
            shaped('Dished shovel blade',[(0,.30,.026,.011),(0,.34,.062,.020),(0,.415,.084,.026),(0,.495,.068,.017),(0,.53,.026,.007)],15,.003)
            shaped('Blade spine',[(0,.31,.008,.014),(0,.43,.011,.029),(0,.50,.001,.019)],15,.001)
        else:
            shaped('Socket and bent hoe neck',[(0,.28,.021,.020),(0,.33,.024,.020),(.04,.355,.026,.022),(.12,.355,.026,.02)],15,.002)
            shaped('Broad forged hoe blade',[(.12,.30,.064,.004),(.12,.355,.069,.013),(.12,.405,.055,.015)],15,.002)
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();model=bpy.context.object;model.name=name
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    if name=='GripHoe':model.data.transform(Matrix.Rotation(math.pi,4,'Z'));model.data.update()
    mat=bpy.data.materials.new('Explorer tool palette');mat.use_nodes=True;bsdf=mat.node_tree.nodes['Principled BSDF'];bsdf.inputs['Roughness'].default_value=.42
    tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'SkinField.png'));tex.image.filepath='//../../Assets/RivetReach/Resources/Characters/SkinField.png';mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
    model.data.materials.clear();model.data.materials.append(mat)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(name+'.blend')))
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
    model.data.calc_loop_triangles();return {'model':name,'triangles':len(model.data.loop_triangles),'materials':1,'shaftRadius':.019}
report=[build(n) for n in ['GripSword','GripPickaxe','GripShovel','GripHoe']]
(SOURCE/'held-tool-report.json').write_text(json.dumps(report,indent=2)+'\n');print('HELD_TOOL_EXPORT_PASS')
