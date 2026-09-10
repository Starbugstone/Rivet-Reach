using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    // Original simple geometry and a bounded light pool. No per-torch simulation callback.
    public sealed class TorchPresentation : MonoBehaviour
    {
        public const int LightLimit=8;
        public const float LightRange=10,ViewRange=48;
        readonly Dictionary<BlockPos,GameObject> views=new Dictionary<BlockPos,GameObject>();
        readonly List<Light> lights=new List<Light>();
        VoxelWorld world;
        Material wood,band,flame,core;
        float refreshAt;
        public int ViewCount=>views.Count;
        public int ActiveLightCount=>lights.Count(l=>l.enabled);
        public IEnumerable<Light> Lights=>lights;
        public void Initialize(VoxelWorld owner)
        {
            world=owner;
            wood=Surface("Torch wood",new Color(.30f,.13f,.045f));
            band=Surface("Torch binding",new Color(.11f,.075f,.045f));
            flame=Surface("Torch flame",new Color(1,.30f,.025f),true);
            core=Surface("Torch flame core",new Color(1,.82f,.24f),true);
            for(int i=0;i<LightLimit;i++)
            {
                var source=Instantiate(Resources.Load<GameObject>("TorchLight"),transform,false);
                var light=source.GetComponent<Light>();
                light.enabled=false;lights.Add(light);
            }
            world.OriginShifted+=Shift;
        }
        Material Surface(string name,Color colour,bool emissive=false)
        {
            var material=new Material(Shader.Find(emissive?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit")){name=name};
            material.SetColor("_BaseColor",emissive?colour*2:colour);material.SetFloat("_Smoothness",.15f);
            return material;
        }
        static Vector3 Base(BlockPos cell,BlockPos support)
        {
            if(cell.Y>support.Y)return new Vector3(.5f,.04f,.5f);
            return new Vector3(.5f+(support.X-cell.X)*.43f,.18f,.5f+(support.Z-cell.Z)*.43f);
        }
        static Vector3 Axis(BlockPos cell,BlockPos support)=>cell.Y>support.Y?Vector3.up:
            new Vector3((cell.X-support.X)*.36f,1,(cell.Z-support.Z)*.36f).normalized;
        public Vector3 FlamePosition(BlockPos cell,BlockPos support)=>world.Local(cell)+Base(cell,support)+Axis(cell,support)*.70f;
        GameObject Create(BlockPos cell,BlockPos support)
        {
            var root=new GameObject("Placed torch");root.transform.SetParent(transform,false);
            root.transform.rotation=Quaternion.FromToRotation(Vector3.up,Axis(cell,support));
            void Part(string name,Vector3 at,Vector3 scale,Material material)
            {
                var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=name;Destroy(part.GetComponent<Collider>());
                part.transform.SetParent(root.transform,false);part.transform.localPosition=at;part.transform.localScale=scale;
                var renderer=part.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
            Part("Handle",new Vector3(0,.28f,0),new Vector3(.10f,.56f,.10f),wood);
            Part("Binding",new Vector3(0,.52f,0),new Vector3(.14f,.16f,.14f),band);
            Part("Flame",new Vector3(0,.67f,0),new Vector3(.13f,.21f,.13f),flame);
            Part("Flame tip",new Vector3(.015f,.80f,0),new Vector3(.065f,.12f,.065f),flame);
            Part("Hot core",new Vector3(0,.62f,-.01f),new Vector3(.145f,.10f,.145f),core);
            return root;
        }
        void LateUpdate(){if(world!=null&&Time.unscaledTime>=refreshAt)Refresh();}
        void Shift(Vector3 shift){Refresh();}
        public void Refresh()
        {
            if(world==null||world.Observer==null)return;
            refreshAt=Time.unscaledTime+.2f;
            var near=world.NearbyTorches(world.Address(world.Observer.position))
                .Where(t=>(world.Local(t.Key)-world.Observer.position).sqrMagnitude<ViewRange*ViewRange)
                .OrderBy(t=>(FlamePosition(t.Key,t.Value)-world.Observer.position).sqrMagnitude).ToArray();
            var wanted=new HashSet<BlockPos>(near.Select(t=>t.Key));
            foreach(var cell in views.Keys.Where(p=>!wanted.Contains(p)).ToArray()){views[cell].SetActive(false);Destroy(views[cell]);views.Remove(cell);}
            foreach(var entry in near)
            {
                if(!views.TryGetValue(entry.Key,out var view)){view=Create(entry.Key,entry.Value);views.Add(entry.Key,view);}
                view.transform.position=world.Local(entry.Key)+Base(entry.Key,entry.Value);
            }
            for(int i=0;i<lights.Count;i++)
            {
                lights[i].enabled=i<near.Length&&(FlamePosition(near[i].Key,near[i].Value)-world.Observer.position).sqrMagnitude<24*24;
                if(lights[i].enabled)lights[i].transform.position=FlamePosition(near[i].Key,near[i].Value);
            }
        }
        public void Clear()
        {
            enabled=false;foreach(var light in lights)if(light!=null)light.enabled=false;
            foreach(var view in views.Values){view.SetActive(false);Destroy(view);}views.Clear();
        }
        void OnDestroy()
        {
            if(world!=null)world.OriginShifted-=Shift;Clear();
            foreach(var material in new[]{wood,band,flame,core})if(material!=null)Destroy(material);
        }
    }
}
