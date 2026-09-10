"""Encode real Unity screenshot sequences using Blender's bundled H.264 encoder.
Blender --background --python this_file -- capture_directory prefix fps output.mp4
No generated/interpolated frames. Frame capture is excluded from runtime benchmarks.
"""
import bpy, sys
from pathlib import Path
capture, prefix, fps, destination = sys.argv[sys.argv.index('--')+1:]
frames=sorted(Path(capture).glob(prefix+'*.png'))
if not frames: raise RuntimeError('No captured runtime frames')
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;editor=scene.sequence_editor_create()
strip=editor.strips.new_image('Actual Unity gameplay',filepath=str(frames[0]),channel=1,frame_start=1)
for frame in frames[1:]:strip.elements.append(frame.name)
scene.frame_start=1;scene.frame_end=len(frames);scene.render.fps=int(fps)
scene.render.resolution_x=1280;scene.render.resolution_y=720;scene.render.resolution_percentage=100
scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1
scene.render.image_settings.media_type='VIDEO';scene.render.image_settings.file_format='FFMPEG'
scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264';scene.render.ffmpeg.constant_rate_factor='HIGH'
scene.render.filepath=str(Path(destination));bpy.ops.render.render(animation=True)
print('RUNTIME_VIDEO_COMPLETE',len(frames),fps,destination,flush=True)
