"""Render original material variants from the approved, unchanged Azure ore mesh.
Blender --background --python Tools/create_ore_variants.py. No additional FBX files.
"""
import bpy
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/RivetReach/Resources/Ores';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Industry/AzureOre.blend'))
scene=bpy.context.scene;ore=bpy.data.objects['Body'];camera=scene.camera
original=ore.data.materials[0]
atlas=bpy.data.images.load(str(ROOT/'Assets/RivetReach/Resources/Industry/Atlas.png'))
base=list(atlas.pixels[:]);variants=[]
# Mineral dark/light facet colours; stone remains unchanged except lighter coal host rock.
for id,key,dark,bright in [(9,'Iron',(.30,.32,.34),(.73,.77,.79)),(10,'Copper',(.48,.16,.035),(.94,.47,.16)),(11,'Coal',(.006,.009,.014),(.036,.047,.060)),(12,'Gold',(.51,.28,.015),(1,.73,.11)),(13,'Diamond',(.005,.40,.31),(.18,1,.75))]:
    image=bpy.data.images.new(key+'OreAtlas',width=256,height=256);pixels=base.copy()
    for y in range(256):
        for x in range(256):
            slot=x//64+(y//64)*4;k=(y*256+x)*4
            if slot in (5,6):
                c=dark if slot==5 else bright
                noise=.97+((x*17+y*13)%19)/19*.06
                pixels[k:k+3]=[min(1,v*noise) for v in c]
            elif id==11 and slot in (0,1,12):
                pixels[k:k+3]=[min(1,v*1.7+.10) for v in pixels[k:k+3]]
    image.pixels[:]=pixels;image.filepath_raw=str(OUT/(str(id)+'Atlas.png'));image.file_format='PNG';image.save()
    material=original.copy();material.name=key+'Ore';bs=material.node_tree.nodes.get('Principled BSDF');bs.inputs['Emission Strength'].default_value=0
    for n in material.node_tree.nodes:
        if n.type=='TEX_IMAGE' and n.outputs['Color'].is_linked and n.outputs['Color'].links[0].to_socket.name=='Base Color':n.image=image
    ore.data.materials[0]=material
    scene.render.resolution_x=scene.render.resolution_y=256;scene.render.filepath=str(ROOT/'Assets/RivetReach/Resources/Industry/Icons'/f'{id}.png');bpy.ops.render.render(write_still=True)
    camera.location=(.5,.5,3);camera.rotation_euler=(0,0,0);camera.data.ortho_scale=.938
    scene.render.resolution_x=scene.render.resolution_y=64;scene.render.filepath=str(OUT/f'{id}Tile.png');bpy.ops.render.render(write_still=True)
    camera.location=(2,2.5,1.8);camera.rotation_euler=(Vector((.5,.5,.5))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=1.6
    variants.append((key,material))
ore.data.materials[0]=original
# Six linked objects, one mesh datablock, six distinct material assignments.
for i,(key,material) in enumerate([('Azure',original)]+variants):
    obj=ore.copy();obj.data=ore.data;scene.collection.objects.link(obj);obj.parent=None;obj.name=key+' ore';obj.location=((i%3)*1.35,(i//3)*1.4,0)
    obj.material_slots[0].link='OBJECT';obj.material_slots[0].material=material
ore.hide_render=True
camera.location=(6,8,7);camera.rotation_euler=(Vector((1.8,1.1,.4))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=5.7
scene.render.resolution_x=1500;scene.render.resolution_y=1000;scene.render.film_transparent=False;scene.render.filepath=str(ROOT/'ArtSource/Industry/ore-variants-review.png');bpy.ops.render.render(write_still=True)
for image in bpy.data.images:
    if image.filepath:image.filepath=bpy.path.relpath(image.filepath,start=str(ROOT/'ArtSource/Industry'))
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/Industry/OreVariants.blend'))
print('ORE_VARIANTS_COMPLETE: six linked objects; one unchanged shared mesh')
