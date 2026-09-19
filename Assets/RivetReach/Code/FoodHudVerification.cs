using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewFoodHud()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-rr-save-directory");Check(at>=0,"Food HUD verification uses the interrupted run's isolated checkpoint");
            game.InitializeSaves(args[at+1]);Check(game.ContinueLatestSave(),"Load actual high-reserve schema17 checkpoint with current food and tool catalogs: "+game.SaveStatus);
            game.enabled=false;FreezeSaveFixture();game.Animals.enabled=false;
            using(var reader=new BinaryReader(File.OpenRead(Path.Combine(args[at+1],"food-fixture.bin"))))
            {Check(game.Hunger.Food==reader.ReadInt32()&&game.Hunger.Saturation==reader.ReadInt32()&&game.Hunger.Exhaustion==reader.ReadDouble(),"Rebalance preserves exactly the nutrition already saved before the change");Check(game.Survival.Tick==reader.ReadInt64(),"Checkpoint receives no offline survival credit");}
            yield return Settle(120);game.enabled=true;yield return null;game.enabled=false;Check(!game.LoadingSave,"Complete load residency before native UI interactions");
            var player=game.Player;game.SetMode(ScreenMode.Play);player.enabled=true;player.Pitch=10;player.Yaw=-45;
            // This short UI fixture starts hungry and supplies one test meal. The
            // preceding crop/cooker run and retained trace own production evidence.
            game.Hunger.Exert(4*(game.Hunger.Food-11+game.Hunger.Saturation));
            game.Inventory.Add(FarmId.Porridge,1);game.Selected=game.Inventory.FindSlot(s=>s.Id==FarmId.Porridge);yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return new WaitForSecondsRealtime(.6f);yield return Capture("eating-prepared-meal");yield return new WaitForSecondsRealtime(.4f);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            Check(game.Hunger.Food==20&&game.Hunger.Saturation==3&&game.Inventory.Total(FarmId.Porridge)==0,"Real bite grants nine food and the confirmed moderate three-point reserve");
            yield return Capture("well-fed-garden");player.enabled=false;
            game.SetMode(ScreenMode.Pause);game.InitializeSaves(Path.Combine(output,"Saves"));Check(game.SaveGame("Moderate food reserve"),"Save moderate meal and existing world using current schema");
            using(var writer=new BinaryWriter(File.Create(Path.Combine(output,"Saves","food-fixture.bin")))){writer.Write(game.Hunger.Food);writer.Write(game.Hunger.Saturation);writer.Write(game.Hunger.Exhaustion);writer.Write(game.Survival.Tick);}
            game.SetMode(ScreenMode.Play);yield return null;yield return new WaitForEndOfFrame();
            var food=game.UI.VisibleRoot.GetComponentsInChildren<SurvivalMeter>().Single(m=>m.MeterKind==SurvivalMeter.Kind.Food);
            var hearts=game.UI.VisibleRoot.GetComponentInChildren<HeartRegenerationWobble>().GetComponent<Text>();
            float[] Centres(Graphic graphic,bool pixel)
            {
                var mesh=graphic.canvasRenderer.GetMesh();var vertices=mesh.vertices;
                Check(vertices.Length>=40&&(!pixel||vertices.Length%10==0),"Read actual rendered survival meter vertices");
                int stride=pixel?vertices.Length/10:4;var centres=new float[10];
                for(int icon=0;icon<10;icon++){for(int v=0;v<stride;v++)centres[icon]+=vertices[icon*stride+v].y;centres[icon]/=stride;}return centres;
            }
            var baseline=Centres(food,true);game.Hunger.Exert(12);yield return null;yield return new WaitForEndOfFrame();
            Check(game.Hunger.Food==20&&game.Hunger.Saturation==0&&Centres(food,true).Zip(baseline,(a,b)=>Mathf.Abs(a-b)<.001f).All(v=>v),"Reserve-only drain leaves food icons still");
            game.Hunger.Exert(4);yield return new WaitForSecondsRealtime(.10f);yield return new WaitForEndOfFrame();var moved=Centres(food,true);
            Check(moved[9]-baseline[9]>.3f&&moved[9]-baseline[9]<=3&&Enumerable.Range(0,9).All(i=>Mathf.Abs(moved[i]-baseline[i])<.001f),"Only the food icon losing its half-point wobbles vertically, bounded to three pixels");yield return Capture("food-tick-wobble");
            yield return new WaitForEndOfFrame();Check(Centres(food,true).Zip(baseline,(a,b)=>Mathf.Abs(a-b)<.001f).All(v=>v),"Food icons return exactly to rest after the short pulse");
            game.Health.Damage(1,DamageKind.Fall,0);yield return null;yield return new WaitForEndOfFrame();var heartBase=Centres(hearts,false);
            yield return new WaitForSecondsRealtime(.10f);yield return new WaitForEndOfFrame();Check(Centres(hearts,false).Zip(heartBase,(a,b)=>Mathf.Abs(a-b)<.001f).All(v=>v),"Damage alone does not trigger the regeneration wobble");
            game.Health.Advance(80,game.Hunger);yield return new WaitForSecondsRealtime(.10f);yield return new WaitForEndOfFrame();var heartMoved=Centres(hearts,false);
            Check(game.Health.Hearts==20&&heartMoved[9]-heartBase[9]>.3f&&heartMoved[9]-heartBase[9]<=3&&Enumerable.Range(0,9).All(i=>Mathf.Abs(heartMoved[i]-heartBase[i])<.001f),"Only the heart restored by the normal paid regeneration tick wobbles");yield return Capture("heart-regeneration-wobble");
            yield return new WaitForEndOfFrame();Check(Centres(hearts,false).Zip(heartBase,(a,b)=>Mathf.Abs(a-b)<.001f).All(v=>v),"Hearts return exactly to rest");
            game.Hunger.Exert(4);yield return null;game.SetMode(ScreenMode.Pause);yield return new WaitForSecondsRealtime(.12f);game.SetMode(ScreenMode.Play);yield return null;yield return new WaitForEndOfFrame();
            Check(Centres(food,true).Zip(baseline,(a,b)=>Mathf.Abs(a-b)<.001f).All(v=>v),"Reopening the HUD does not replay a hidden-screen pulse");
            // Encode a brief actual HUD capture sequence; no simulated/redrawn art.
            Directory.CreateDirectory(Path.Combine(output,"hud-frames"));
            var corners=new Vector3[4];food.rectTransform.GetWorldCorners(corners);var bounds=new Rect(corners[0].x,corners[0].y,corners[2].x-corners[0].x,corners[2].y-corners[0].y);
            hearts.rectTransform.GetWorldCorners(corners);float left=Mathf.Min(bounds.xMin,corners[0].x)-8,bottom=Mathf.Min(bounds.yMin,corners[0].y)-8,right=Mathf.Max(bounds.xMax,corners[2].x)+8,top=Mathf.Max(bounds.yMax,corners[2].y)+8;
            var rect=new Rect(Mathf.Floor(left),Mathf.Floor(bottom),Mathf.Ceil(right-left),Mathf.Ceil(top-bottom));
            for(int frame=0;frame<40;frame++)
            {
                if(frame==5)game.Hunger.Exert(4);
                if(frame==22){game.Health.Damage(1,DamageKind.Fall,0);yield return null;game.Health.Advance(80,game.Hunger);}
                yield return new WaitForEndOfFrame();var texture=new Texture2D((int)rect.width,(int)rect.height,TextureFormat.RGB24,false);texture.ReadPixels(rect,0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,"hud-frames",frame.ToString("D3")+".png"),texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);yield return new WaitForSecondsRealtime(.05f);
            }
            Check(game.Hunger.Food>=12&&!game.Health.Dead,"HUD feedback leaves ordinary survival state authoritative");
        }
    }
}
