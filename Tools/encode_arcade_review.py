"""Encode the actual Unity arcade capture sequence using Blender's bundled video encoder.
Usage: blender --background --python <this script> -- <capture directory>
"""
import bpy,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
capture=Path(sys.argv[sys.argv.index('--')+1])
frames=sorted(capture.glob('arcade-motion-*.png'))
assert len(frames)==150, 'Expected the complete 150-frame runtime capture'
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;editor=scene.sequence_editor_create()
strip=editor.strips.new_image('Actual Unity gameplay',filepath=str(frames[0]),channel=1,frame_start=1)
for frame in frames[1:]:strip.elements.append(frame.name)
scene.frame_start=1;scene.frame_end=len(frames);scene.render.fps=60
scene.render.resolution_x=1280;scene.render.resolution_y=720;scene.render.resolution_percentage=100
scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1
scene.render.image_settings.media_type='VIDEO';scene.render.image_settings.file_format='FFMPEG'
scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264';scene.render.ffmpeg.constant_rate_factor='PERC_LOSSLESS'
scene.render.filepath=str(root/'.docs/verification/arcade-action-preview.mp4')
bpy.ops.render.render(animation=True)
print('ARCADE_VIDEO_PASS',flush=True)
