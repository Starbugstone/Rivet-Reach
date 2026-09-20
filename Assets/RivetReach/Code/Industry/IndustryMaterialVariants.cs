using System;
using UnityEngine;

namespace RivetReach
{
    // Session-owned finite palettes preserve the authored shader/maps/keywords.
    // No per-renderer overrides: compatible URP shaders can use the SRP Batcher.
    // This does not imply fewer draws; native render cost still needs measuring.
    internal sealed class IndustryMaterialVariants : IDisposable
    {
        readonly Material[] body=new Material[2],status=new Material[5];
        public int Count {get;private set;}
        public IndustryMaterialVariants(Material workshop,Material indicator)
        {
            if(workshop==null||indicator==null)throw new ArgumentNullException(workshop==null?nameof(workshop):nameof(indicator));
            body[0]=Variant(workshop,"Idle", "_EmissionColor",new Color(.12f,.12f,.12f));
            body[1]=Variant(workshop,"Active","_EmissionColor",new Color(1.5f,1.5f,1.5f));
            status[0]=Variant(indicator,"Running","_BaseColor",new Color(.24f,1,.61f));
            status[1]=Variant(indicator,"Underpowered","_BaseColor",new Color(1,.68f,.12f));
            status[2]=Variant(indicator,"Disabled","_BaseColor",new Color(.2f,.25f,.28f));
            status[3]=Variant(indicator,"Ready","_BaseColor",new Color(.30f,.52f,.62f));
            status[4]=Variant(indicator,"Other","_BaseColor",new Color(1,.31f,.08f));
            Count=body.Length+status.Length;
        }
        static Material Variant(Material source,string label,string property,Color colour)
        {
            var material=new Material(source){name=source.name+" (session "+label+")",hideFlags=HideFlags.DontSave};
            material.SetColor(property,colour);return material;
        }
        public Material Body(bool active)=>body[active?1:0];
        public Material Status(MachineStatus state,bool running)=>status[running?(state==MachineStatus.Underpowered?1:0):state==MachineStatus.DisabledBySignal?2:state==MachineStatus.Ready?3:4];
        public static void Apply(Renderer renderer,Material material)
        {
            renderer.SetPropertyBlock(null);renderer.sharedMaterial=material;
        }
        public void Dispose()
        {
            Release(body);Release(status);Count=0;
        }
        static void Release(Material[] materials)
        {
            for(int i=0;i<materials.Length;i++)
            {
                if(materials[i]!=null)
                {
                    // In play, Unity retires the owner's views and these materials
                    // at the frame boundary. Editor verification cleans up immediately.
                    if(Application.isPlaying)UnityEngine.Object.Destroy(materials[i]);
                    else UnityEngine.Object.DestroyImmediate(materials[i]);
                    materials[i]=null;
                }
            }
        }
    }
}
