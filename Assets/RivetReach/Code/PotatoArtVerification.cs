using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewPotatoArt()
        {
            var player=game.Player;var world=game.World;
            game.Diagnostics=false;game.SetCreative(true);
            player.Pitch=10;player.Yaw=0;
            game.Inventory.Add(BlockId.Potato,8,0,1);game.Inventory.Add(BlockId.BakedPotato,8,1,2);
            game.Inventory.Add(BlockId.CopperOre,8,2,3);game.Inventory.Add(BlockId.IronOre,8,3,4);
            foreach(byte id in new[]{BlockId.Potato,BlockId.BakedPotato})
            {
                game.Selected=id==BlockId.Potato?0:1;
                yield return new WaitForSeconds(1);
                var held=player.HeldBlock;
                Check(held!=null&&held.ItemId==id&&held.Visible,"Selected food has a visible held view: "+id);
                var mesh=held.Socket.GetComponentsInChildren<MeshFilter>().Single(f=>f.gameObject.activeInHierarchy&&f.sharedMesh!=null&&f.sharedMesh.name==FoodVisuals.Key(id));
                Check(mesh.sharedMesh.triangles.Length>300,"Held food uses authored 3D geometry: "+id);
                Check(mesh.GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap")==FoodVisuals.Palette,"Held food uses its shared skin palette: "+id);
                yield return Capture("held-"+FoodVisuals.Key(id));
                var at=player.transform.position+player.transform.forward*3+Vector3.up*.8f+(id==BlockId.Potato?Vector3.left:Vector3.right)*.45f;
                game.Items.Spawn(new ItemStack(id,3),at,Vector3.zero,120);
            }
            yield return new WaitForSeconds(1);
            foreach(var pile in game.Items.Piles.Where(p=>FoodVisuals.UsesModel(p.Stack.Id)))
            {
                Check(pile.View!=null&&pile.View.GetComponentsInChildren<MeshFilter>().Count(f=>f.sharedMesh.name==FoodVisuals.Key(pile.Stack.Id))==3,"Dropped stack uses three shared authored food meshes: "+pile.Stack.Id);
                Check(pile.View.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial==Resources.Load<Material>("Food/Potato")),"Dropped food shares the world material");
            }
            player.Pitch=32;yield return Capture("potato-drops");
            game.SetMode(ScreenMode.Inventory);yield return null;yield return null;
            foreach(byte id in new[]{BlockId.Potato,BlockId.BakedPotato})
                Check(game.UI.VisibleRoot.GetComponentsInChildren<UnityEngine.UI.RawImage>().Any(i=>i.texture==FoodVisuals.Icon(id)),"Inventory shows rendered food icon: "+id);
            yield return Capture("potato-inventory");game.SetMode(ScreenMode.Play);game.SetCreative(false);
            // Exercise an actual bite to catch presentation changes interfering with item use.
            game.Hunger.Exert(60);game.Selected=1;player.Pitch=-35;yield return new WaitForSeconds(.5f);
            int before=game.Inventory.Total(BlockId.BakedPotato),food=game.Hunger.Food;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return new WaitForSeconds(1.35f);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            Check(game.Inventory.Total(BlockId.BakedPotato)==before-1&&game.Hunger.Food==food+5,"Eating the new baked potato consumes one item and restores five food points");
        }
    }
}
