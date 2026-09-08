"""Original 48 kHz / 24-bit layered game foley; no samples or third-party inputs.
Run with Blender's bundled Python/numpy: blender --background --python <file>.
Copyright (c) 2026 Starbugstone. See LICENSE.md.
"""
import json, math, wave
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/RivetReach/Resources/Audio';OUT.mkdir(parents=True,exist_ok=True)
RATE=48000
REPORT=[]

def noise(rng,n,low,high):
    frequencies=np.fft.rfftfreq(n,1/RATE)
    spectrum=np.fft.rfft(rng.normal(0,1,n))
    spectrum*=1/(1+(frequencies/max(1,high))**4)
    if low:spectrum*=1-1/(1+(frequencies/low)**4)
    result=np.fft.irfft(spectrum,n)
    return result/max(.001,np.std(result))

def transient(rng,t,low,high,decay,attack=.002):
    return noise(rng,len(t),low,high)*(1-np.exp(-t/attack))*np.exp(-t/decay)

def save(name,data,peak=.55,loop=False):
    data=np.asarray(data,dtype=np.float64)
    data-=np.mean(data,axis=0)
    if not loop:
        fade=min(480,len(data)//4);shape=(fade,)+(1,)*(data.ndim-1)
        data[:fade]*=np.sin(np.linspace(0,math.pi/2,fade)).reshape(shape)**2
        data[-fade:]*=np.cos(np.linspace(0,math.pi/2,fade)).reshape(shape)**2
    data*=peak/max(.001,np.max(np.abs(data)))
    assert np.all(np.isfinite(data)) and np.max(np.abs(data))<1
    integers=np.rint(data.reshape(-1)*8388607).astype(np.int32)
    packed=np.empty((len(integers),3),dtype=np.uint8)
    for i in range(3):packed[:,i]=(integers>>(8*i))&255
    with wave.open(str(OUT/(name+'.wav')),'wb') as wav:
        wav.setnchannels(1 if data.ndim==1 else data.shape[1]);wav.setsampwidth(3);wav.setframerate(RATE);wav.writeframes(packed.tobytes())
    REPORT.append({'asset':name,'sampleRate':RATE,'bits':24,'channels':1 if data.ndim==1 else data.shape[1],
        'seconds':len(data)/RATE,'peakDBFS':round(20*np.log10(np.max(np.abs(data))),2),
        'rmsDBFS':round(20*np.log10(np.sqrt(np.mean(data*data))),2),'loop':loop,
        'endpointDifference':float(np.max(np.abs(data[-1]-data[0])))})

def material_sound(material,kind,variant):
    rng=np.random.default_rng(1200+material*91+variant*173+len(kind)*19)
    duration={'Step':.43,'Hit':.34,'Break':.82}[kind];t=np.arange(int(RATE*duration))/RATE
    strength={'Step':.65,'Hit':1,'Break':.9}[kind]
    # Low boot/palm mass, short contact grain, then independently scattered debris.
    data=transient(rng,t,30,170,.034)*.5
    frequency=[95,81,152,115,73][material]*(.93+rng.random()*.14)
    data+=np.sin(math.tau*(frequency*t-35*t*t))*np.exp(-t/.037)*(1-np.exp(-t/.0015))*.55
    low,high,decay=[(650,5200,.063),(230,2500,.05),(900,7500,.024),(220,3400,.039),(1600,8200,.09)][material]
    data+=transient(rng,t,low,high,decay)*(.34 if kind=='Step' else .55)
    if material in [2,3]:
        ratios=[1,1.49,2.18,3.7] if material==2 else [1,2.3,3.4,5.2]
        for j,ratio in enumerate(ratios):
            data+=np.sin(math.tau*(480 if material==2 else 260)*ratio*(.96+rng.random()*.08)*t+rng.random())*np.exp(-t/(.022+j*.006))/(9+j*3)
    for j in range(7 if kind=='Break' else 3):
        at=.014+rng.random()*(.48 if kind=='Break' else .11)
        local=np.maximum(0,t-at);env=(t>=at)*(1-np.exp(-local/.0006))*np.exp(-local/(.008+rng.random()*.016))
        data+=noise(rng,len(t),high*.2,high)*env*(.15 if kind=='Break' else .06)
    if material in [0,4]:data+=transient(rng,t,1600,10000,.14,.019)*.14
    return data*strength

for material,label in enumerate(['Grass','Soil','Stone','Wood','Leaves']):
    for kind,count in [('Step',5),('Hit',4),('Break',3)]:
        for variant in range(count):save(kind+label+str(variant),material_sound(material,kind,variant),.52 if kind=='Step' else .67)
for variant in range(4):
    rng=np.random.default_rng(830+variant);t=np.arange(int(RATE*.32))/RATE
    env=np.exp(-((t-(.075+variant*.003))/.036)**2)
    save('Swing'+str(variant),noise(rng,len(t),180,3800)*env+.22*transient(rng,t,1800,8000,.07,.008),.38)
    save('Equip'+str(variant),transient(rng,t,230,4600,.065,.006)+.25*np.sin(t*math.tau*540)*np.exp(-t/.022),.4)
    save('Pickup'+str(variant),transient(rng,t,700,4000,.027)+.3*np.sin(t*math.tau*920)*np.exp(-t/.022),.35)
    save('Land'+str(variant),transient(rng,t,25,210,.065)+.3*transient(rng,t,400,4700,.08,.008),.72)
# FFT-filtered cyclic noise and periodic gusts make a continuous stereo ambience.
rng=np.random.default_rng(712);seconds=36;n=RATE*seconds;t=np.arange(n)/RATE
mid=noise(rng,n,80,1800);side=noise(rng,n,160,3300)
gust=.45+.16*np.sin(math.tau*t/seconds*3)+.12*np.sin(math.tau*t/seconds*7+.6)
leaves=noise(rng,n,2300,9500)*(.035+.028*np.sin(math.tau*t/seconds*11)**4)
data=np.column_stack((mid*gust+side*.18+leaves,mid*gust-side*.18+leaves))
# Match value and slope across the wrap with a short cyclic Hermite correction.
for channel in range(2):
    count=2048;s=np.linspace(0,1,count)
    delta=data[0,channel]-data[-1,channel]
    slope=(data[1,channel]-data[0,channel])-(data[-1,channel]-data[-2,channel])
    data[-count:,channel]+=(3*s*s-2*s*s*s)*delta+(s*s*s-s*s)*slope*(count-1)
save('WindCanopy',data,.34,True)
source=ROOT/'ArtSource/Audio';source.mkdir(parents=True,exist_ok=True)
(source/'sound-manifest.json').write_text(json.dumps({'provenance':'Original deterministic synthesis. No recordings, external samples or generative service inputs.','generator':'Tools/create_sound_assets.py','assets':REPORT},indent=2)+'\n')
print('SOUND_ASSETS_PASS '+str(len(REPORT)),flush=True)
