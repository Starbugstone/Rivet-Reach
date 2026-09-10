"""Original semantic skin atlases with cloth grain and restrained skin microstructure.
Copyright (c) 2026 Starbugstone. Run in Blender; no third-party texture inputs.
"""
import bpy, numpy as np, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/RivetReach/Resources/Characters'
sys.dont_write_bytecode=True;sys.path.insert(0,str(ROOT/'Tools'))
from create_player_assets import atlas

def save(name,data,linear=False):
    im=bpy.data.images.new(name,width=data.shape[1],height=data.shape[0],alpha=True)
    if linear:im.colorspace_settings.name='Non-Color'
    im.pixels.foreach_set(data.astype(np.float32).reshape(-1));im.filepath_raw=str(OUT/(name+'.png'));im.file_format='PNG';im.save()
    return im

def generate():
    size=1024;tile=256;yy,xx=np.mgrid[:tile,:tile];u=xx/(tile-1);v=yy/(tile-1)
    rng=np.random.default_rng(951);noise=rng.normal(0,1,(tile,tile))
    skin=.35*np.sin(u*42+np.sin(v*17))+.22*np.sin(v*61+u*16)+noise*.12
    seam=np.clip(np.minimum(u-.06,.94-u)/.08,0,1)
    skin*=seam
    weave=np.sin(xx*np.pi/2)*np.sin(yy*np.pi/2)
    detail=np.ones((size,size,4));normal=np.ones((size,size,4));normal[:,:,:3]=(.5,.5,1)
    for alternate in [False,True]:
        name='SkinOchre' if alternate else 'SkinField';atlas(name,alternate)
        base=bpy.data.images.get(name);pixels=np.array(base.pixels[:]).reshape(256,256,4)
        output=np.ones((size,size,4))
        for region in range(16):
            x=region%4*tile;y=region//4*tile
            colour=pixels[region//4*64+32,region%4*64+32,:3]
            flesh=region in [0,4,5];cloth=region in [1,2,3,6,7,13];metal=region in [12,15]
            variation=(skin*.025 if flesh else weave*.018+noise*.009 if cloth else noise*.008)
            tint=colour[None,None,:]*(1+variation[:,:,None])
            if flesh:
                # Subtle broad warm/cool mottling, without painting mesh seams.
                tint+=np.stack((skin*.008,skin*.001,-skin*.004),axis=2)
            output[y:y+tile,x:x+tile,:3]=np.clip(tint,0,1)
            rough=.72 if flesh else .88 if cloth else .30 if metal else .66
            detail[y:y+tile,x:x+tile,0]=.78 if metal else 0
            detail[y:y+tile,x:x+tile,1]=np.clip(rough+noise*.018,0,1)
            detail[y:y+tile,x:x+tile,2]=1 if flesh else 0
            height=noise*(.006 if flesh else .015)+weave*(.024 if cloth else 0)
            if flesh:height*=seam
            dy,dx=np.gradient(height);norm=np.stack((-dx,-dy,np.ones_like(dx)),axis=2);norm/=np.linalg.norm(norm,axis=2)[:,:,None]
            normal[y:y+tile,x:x+tile,:3]=norm*.5+.5
            if region in [4,5]:
                nail=np.array((.73,.52,.40) if alternate else (.85,.68,.54))
                output[y:y+24,x:x+24,:3]=nail
                detail[y:y+24,x:x+24,1]=.35
                normal[y:y+24,x:x+24,:3]=(.5,.5,1)
                # u > .953 is reserved for the fitted leather glove overlay.
                glove=np.array((.12,.16,.19) if alternate else (.22,.105,.052))
                output[y:y+tile,x+244:x+tile,:3]=glove[None,None,:]*(1+noise[:,244:,None]*.024)
                detail[y:y+tile,x+244:x+tile,:3]=(0,.65,0)
                normal[y:y+tile,x+244:x+tile,:3]=(.5,.5,1)
        save(name,output)
    save('SkinSurface',detail,True);save('SkinNormal',normal,True)
    print('SKIN_MATERIALS_PASS',flush=True)

if __name__=='__main__':generate()
