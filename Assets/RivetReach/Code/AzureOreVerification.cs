using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewAzureOre()
        {
            var world=game.World;var player=game.Player;
            game.Mobs.enabled=false;game.Diagnostics=false;game.SetCreative(true);
            var prefab=Resources.Load<GameObject>("Industry/Runtime/azure_ore");
            var meshes=prefab.GetComponentsInChildren<MeshFilter>();
            int triangles=meshes.Sum(m=>m.sharedMesh.triangles.Length/3);
            Check(meshes.Length==1&&triangles>1000&&triangles<5000,"Azure imported as one detailed mesh under 5000 triangles");
            var bounds=new Bounds(Vector3.one*.5f,Vector3.zero);
            foreach(var filter in meshes)
            {
                foreach(var vertex in filter.sharedMesh.vertices)bounds.Encapsulate(filter.transform.TransformPoint(vertex));
                Check(filter.sharedMesh.normals.Length==filter.sharedMesh.vertexCount&&filter.sharedMesh.uv.Length==filter.sharedMesh.vertexCount,"Imported normals and shared atlas UVs cover all vertices");
                Check(filter.sharedMesh.subMeshCount==1,"Azure retains one material submesh");
            }
            Check(bounds.min.x>=0&&bounds.min.y>=0&&bounds.min.z>=0&&bounds.max.x<=1&&bounds.max.y<=1&&bounds.max.z<=1,"Azure relief fits the one metre cell");
            File.WriteAllText(Path.Combine(output,"azure-import.txt"),$"Unity {Application.unityVersion}; {triangles} triangles; {meshes.Length} mesh; one material; bounds {bounds}\n");
            var target=OreGenerator.Veins(game.Seed,new BlockPos(-64,-56,-64),new BlockPos(64,-24,64))
                .Where(v=>v.Band.Block==IndustryId.AzureOre&&world.Generator.At(v.Centre)==IndustryId.AzureOre).Select(v=>v.Centre).First();
            player.enabled=false;player.ResetMotion();
            player.transform.position=world.Local(target)+new Vector3(.5f,-1,-3);
            yield return null;yield return Settle();
            // Clear only the approach; the photographed ore remains naturally generated.
            for(int z=-5;z<0;z++)for(int y=-1;y<=3;y++)for(int x=-3;x<=3;x++)
            {var p=target.Offset(x,y,z);byte id=world.Get(p);if(id!=0)world.Remove(p,id);}
            for(int z=-5;z<0;z++)for(int x=-3;x<=3;x++)
            {var p=target.Offset(x,-2,z);if(world.Get(p)==0)world.Place(p,BlockId.Stone);}
            player.Yaw=0;player.Pitch=0;
            player.Camera.transform.position=world.Local(target)+new Vector3(.5f,.65f,-3.2f);
            player.Camera.transform.LookAt(world.Local(target)+new Vector3(.5f,.5f,0));
            // A review light makes the unchanged terrain shader readable in the excavated tunnel.
            var lamp=new GameObject("Azure review light");lamp.transform.position=world.Local(target)+new Vector3(-1,2,-2);
            var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=10;light.intensity=3;light.color=new Color(1,.87f,.70f);
            yield return new WaitForSecondsRealtime(1);
            Check(world.Get(target)==IndustryId.AzureOre,"Photographed deposit is generated Azure ore");
            yield return Capture("azure-natural-deposit");
            game.SetCreative(false);game.Items.enabled=false;
            Check(!world.Mine(target,IndustryId.AzureOre,ToolCapability.Pickaxe,ToolTier.Stone),"Stone pickaxe still cannot mine Azure");
            int spawned=game.Items.TotalSpawned;
            Check(world.Mine(target,IndustryId.AzureOre,ToolCapability.Pickaxe,ToolTier.Copper),"Copper pickaxe mines the natural deposit");
            Check(game.Items.TotalSpawned==spawned+1&&world.Get(target)==0,"Mining removes one block and yields exactly one ore");
            // Display the imported mesh at inspection scale beside the real deposit.
            var display=Instantiate(prefab);display.transform.position=world.Local(target);
            display.transform.rotation=Quaternion.Euler(0,25,0);
            player.Camera.transform.position=world.Local(target)+new Vector3(2,1.8f,-2.7f);
            player.Camera.transform.LookAt(world.Local(target)+Vector3.one*.5f);
            yield return Capture("azure-unity-model");Destroy(display);
            game.SetCreative(true);game.Inventory.Add(IndustryId.AzureOre,1);
            game.Selected=game.Inventory.FindSlot(s=>s.Id==IndustryId.AzureOre);
            player.enabled=true;player.Yaw=0;player.Pitch=12;
            yield return new WaitForSecondsRealtime(1);
            Check(player.HeldBlock.Visible&&player.HeldBlock.ItemId==IndustryId.AzureOre,"Azure can be held after mining");
            Check(player.HeldBlock.Socket.GetComponentsInChildren<MeshFilter>().Any(m=>m.sharedMesh!=null&&m.sharedMesh.name.StartsWith("azure_ore")),"Held Azure uses the imported 3D mesh");
            yield return Capture("azure-held");
            game.SetMode(ScreenMode.Inventory);yield return Capture("azure-inventory");
            Destroy(lamp);
        }
    }
}
