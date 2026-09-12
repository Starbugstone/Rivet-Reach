using UnityEngine;

namespace RivetReach
{
    public sealed partial class Expedition
    {
        public bool HoldingWrench=>Inventory!=null&&!Inventory.Slots[Selected].Empty&&Inventory.Slots[Selected].Id==IndustryId.Wrench;
        public bool TryGetPipeEndTarget(out MachineState pipe,out int face)
        {
            pipe=null;face=-1;
            if(Mode!=ScreenMode.Play||Player==null)return false;
            var eye=Player.Camera.transform;var ray=new Ray(eye.position,eye.forward);
            // Use the normal voxel visibility test first: no toggling through walls,
            // through a machine, beyond reach, or into an unloaded chunk.
            if(!World.Raycast(ray.origin,ray.direction,5,out var position,out var id)||!PipeConnections.IsTransport(id))return false;
            var candidate=Industry.Simulation.At(position);if(candidate==null)return false;
            float nearest=5;
            for(int f=0;f<6;f++)
            {
                if(!Industry.Simulation.HasPipeEnd(candidate,f))continue;
                var bounds=PipeEndBounds(World.Local(position),f);
                if(!bounds.IntersectRay(ray,out float distance)||distance<0||distance>nearest)continue;
                nearest=distance;face=f;
            }
            if(face<0)return false;pipe=candidate;return true;
        }
        public static Bounds PipeEndBounds(Vector3 pipeCorner,int face)
        {
            var d=IndustryDefinition.Directions[face];var axis=new Vector3(d.x,d.y,d.z);
            var size=Vector3.one*.66f;size[face/2]=.32f;
            return new Bounds(pipeCorner+Vector3.one*.5f+axis*.34f,size);
        }
        public bool TryConfigurePipeEnd()
        {
            if(!HoldingWrench||!TryGetPipeEndTarget(out var pipe,out int face)||!Industry.Simulation.TogglePipeEnd(pipe,face))return false;
            bool input=Industry.Simulation.PipeEndRole(pipe,face)==PortRole.Input;
            Notify((pipe.Definition.Id==IndustryId.ItemPipe?"Items":"Fluid")+(input?": INPUT into machine (blue)":": OUTPUT from machine (red)"),2);
            Sound.Place(pipe.Definition.Id,World.Local(pipe.Position));Player.Arms.TriggerSwing();Player.Body.TriggerSwing();return true;
        }
    }
}
