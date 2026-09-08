"""Build a short, labelled-in-docs sound-bank audition from the original WAV masters.
This is an arranged preview, not a recording of runtime playback.
Copyright (c) 2026 Starbugstone. No external audio inputs.
"""
from pathlib import Path
import wave,math
ROOT=Path(__file__).resolve().parents[1];RATE=48000;FRAMES=RATE*8
mix=[0.0]*(FRAMES*2)

def layer(name,at,gain):
    with wave.open(str(ROOT/'Assets/RivetReach/Resources/Audio'/(name+'.wav')),'rb') as sound:
        assert sound.getsampwidth()==3 and sound.getframerate()==RATE
        channels=sound.getnchannels();data=sound.readframes(min(FRAMES-int(at*RATE),sound.getnframes()))
    for frame in range(len(data)//(3*channels)):
        for channel in range(2):
            offset=(frame*channels+min(channel,channels-1))*3
            value=int.from_bytes(data[offset:offset+3],'little',signed=True)/8388608
            mix[(frame+int(at*RATE))*2+channel]+=value*gain

layer('WindCanopy',0,.18)
for name,at,gain in [('StepGrass0',.4,.34),('StepGrass2',.9,.34),('StepGrass4',1.4,.34),
    ('StepStone1',2.1,.34),('StepStone3',2.6,.34),('Equip0',3.2,.2),('Swing0',3.55,.21),
    ('HitStone0',3.64,.3),('BreakStone0',4,.5),('Pickup0',4.55,.27),
    ('Swing1',5.1,.21),('HitWood2',5.18,.3),('BreakWood1',5.42,.5),('Land2',6.4,.4)]:layer(name,at,gain)
data=bytearray()
for i,value in enumerate(mix):
    t=i/2/RATE;fade=min(1,t/.2,(8-t)/.5)
    value*=max(0,fade);assert abs(value)<1
    data.extend(round(value*8388607).to_bytes(3,'little',signed=True))
path=ROOT/'.docs/verification/hifi-audio-preview.wav'
with wave.open(str(path),'wb') as sound:
    sound.setnchannels(2);sound.setsampwidth(3);sound.setframerate(RATE);sound.writeframes(data)
print('AUDIO_PREVIEW_PASS: 8 seconds, stereo 48 kHz / 24-bit')
