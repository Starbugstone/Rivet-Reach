using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace RivetReach
{
    public static class SpawnerVisuals
    {
        public static Texture2D Icon=>Resources.Load<Texture2D>("Spawners/38");
        public static Texture2D Palette=>Resources.Load<Texture2D>("Spawners/Palette");
        public static GameObject Create(Transform parent,MobDefinition species=null,Material cageMaterial=null,Material displayMaterial=null)
        {
            var prefab=Resources.Load<GameObject>("Spawners/Cage");
            if(prefab==null)throw new InvalidOperationException("Missing Spawners/Cage asset.");
            // Imported FBX roots carry axis/unit conversion. Keep both authored cage
            // and miniature beneath a neutral metre-space presentation root.
            var root=new GameObject("Mob spawner cage");root.transform.SetParent(parent,false);
            UnityEngine.Object.Instantiate(prefab,root.transform,false);
            if(cageMaterial!=null)foreach(var renderer in root.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=cageMaterial;
            if(species==null)
            {
                string key=MobSystem.SpawnerDefinitions[0].species;
                species=Array.Find(Resources.LoadAll<MobDefinition>("Mobs/Definitions"),d=>d.stableId==key);
            }
            if(species==null)throw new InvalidOperationException("Missing spawner display species.");
            var model=Resources.Load<GameObject>("Mobs/"+species.model);
            if(model==null)throw new InvalidOperationException("Missing spawner display model "+species.model);
            var imported=UnityEngine.Object.Instantiate(model,root.transform,false);
            var display=FreezeIdlePose(imported,species,root.transform);
            imported.SetActive(false);DestroyObject(imported);
            FitInsideCage(display.transform,root.transform);
            // The model is a frozen mesh pose. It owns no runtime animation, physics,
            // collider or entity state; MobSpawnerPresentation rotates only this root.
            foreach(var collider in display.GetComponentsInChildren<Collider>())DestroyComponent(collider);
            foreach(var renderer in display.GetComponentsInChildren<Renderer>())
            {renderer.sharedMaterial=displayMaterial??Resources.Load<Material>("Mobs/CreatureMaterial");renderer.shadowCastingMode=ShadowCastingMode.Off;}
            return root;
        }
        static GameObject FreezeIdlePose(GameObject display,MobDefinition species,Transform cage)
        {
            var animator=display.GetComponent<Animator>();
            if(animator==null)throw new InvalidOperationException("Missing spawner display animator "+species.model);
            var idle=Array.Find(Resources.LoadAll<AnimationClip>("Mobs/"+species.model),clip=>clip.name=="Idle");
            if(idle==null)throw new InvalidOperationException("Missing spawner display idle animation "+species.model);
            var graph=PlayableGraph.Create("Spawner display pose");
            try
            {
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var pose=AnimationClipPlayable.Create(graph,idle);pose.SetApplyFootIK(false);pose.SetSpeed(0);
                AnimationPlayableOutput.Create(graph,"Spawner display pose",animator).SetSourcePlayable(pose);
                graph.Play();graph.Evaluate(0);
                return BakeStaticDisplay(display,cage);
            }
            finally {if(graph.IsValid())graph.Destroy();}
        }
        static void FitInsideCage(Transform display,Transform cage)
        {
            var bounds=CageLocalBounds(display,cage);
            float extent=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
            if(extent<=.0001f||float.IsNaN(extent)||float.IsInfinity(extent))throw new InvalidOperationException("Invalid spawner display bounds.");
            // Fit the independent static pivot in the neutral root's metre space.
            // A 0.58 m span clears the cage's inner rails.
            // Centre the vertices themselves so rotation stays centred rather than
            // orbiting an imported origin offset.
            foreach(var filter in display.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;var vertices=mesh.vertices;
                for(int i=0;i<vertices.Length;i++)vertices[i]-=bounds.center;
                mesh.vertices=vertices;mesh.RecalculateBounds();
            }
            display.localScale=Vector3.one*(.58f/extent);
            display.localPosition=new Vector3(0,.5f,0);
        }
        static Bounds CageLocalBounds(Transform display,Transform cage)
        {
            bool found=false;Bounds bounds=default;
            foreach(var renderer in display.GetComponentsInChildren<Renderer>())
            {
                // The old SkinnedMeshRenderer is disabled until Destroy runs at the
                // end of a player frame; never let it influence the baked display fit.
                if(!renderer.enabled)continue;
                var local=renderer.localBounds;
                for(int z=0;z<2;z++)for(int y=0;y<2;y++)for(int x=0;x<2;x++)
                {
                    var point=renderer.transform.TransformPoint(new Vector3(x==0?local.min.x:local.max.x,y==0?local.min.y:local.max.y,z==0?local.min.z:local.max.z));
                    point=cage.InverseTransformPoint(point);
                    if(found)bounds.Encapsulate(point);else{bounds=new Bounds(point,Vector3.zero);found=true;}
                }
            }
            if(!found)throw new InvalidOperationException("Spawner display has no enabled renderers.");
            return bounds;
        }
        static GameObject BakeStaticDisplay(GameObject imported,Transform cage)
        {
            var display=new GameObject("Species display (visual only)");display.transform.SetParent(cage,false);
            var cleanup=display.AddComponent<FrozenSpawnerDisplay>();int count=0;
            foreach(var skin in imported.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var baked=new Mesh{name="Frozen spawner display mesh"};skin.BakeMesh(baked);
                var vertices=baked.vertices;var normals=baked.normals;var tangents=baked.tangents;
                // BakeMesh returns positions in the skinned renderer's local frame.
                // Store them in the cage frame before the imported animation hierarchy
                // is removed, so a later Animator reset cannot move the frozen mesh.
                for(int i=0;i<vertices.Length;i++)vertices[i]=cage.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]));
                for(int i=0;i<normals.Length;i++)normals[i]=cage.InverseTransformDirection(skin.transform.TransformDirection(normals[i])).normalized;
                for(int i=0;i<tangents.Length;i++)
                {var tangent=cage.InverseTransformDirection(skin.transform.TransformDirection(new Vector3(tangents[i].x,tangents[i].y,tangents[i].z))).normalized;tangents[i]=new Vector4(tangent.x,tangent.y,tangent.z,tangents[i].w);}
                baked.vertices=vertices;baked.normals=normals;baked.tangents=tangents;baked.RecalculateBounds();
                var frozen=new GameObject("Frozen display mesh").transform;frozen.SetParent(display.transform,false);
                frozen.gameObject.AddComponent<MeshFilter>().sharedMesh=baked;
                frozen.gameObject.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                cleanup.Meshes.Add(baked);count++;
            }
            if(count==0){DestroyObject(display);throw new InvalidOperationException("Spawner display has no skinned meshes.");}
            return display;
        }
        static void DestroyComponent(UnityEngine.Object component)
        {
            if(Application.isPlaying)UnityEngine.Object.Destroy(component);
            else UnityEngine.Object.DestroyImmediate(component);
        }
        static void DestroyObject(UnityEngine.Object value)
        {
            if(Application.isPlaying)UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
    // Baked meshes are runtime instances, so release them with the cage view rather
    // than retaining one mesh allocation for each streamed-in spawner.
    sealed class FrozenSpawnerDisplay : MonoBehaviour
    {
        internal readonly System.Collections.Generic.List<Mesh> Meshes=new System.Collections.Generic.List<Mesh>();
        void OnDestroy()
        {
            foreach(var mesh in Meshes)if(mesh!=null)
            {if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}
            Meshes.Clear();
        }
    }
}
