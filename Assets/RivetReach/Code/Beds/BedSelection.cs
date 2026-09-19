using UnityEngine;

namespace RivetReach
{
    // Cached per-cell boxes follow the authored mattress, rails and legs; rays can
    // pass through the open space below the bed. Movement still reserves both cells.
    public sealed class BedSelection : ISelectionShapeProvider
    {
        readonly bool head;
        static readonly SelectionShape[,] shapes=Build();
        public BedSelection(bool head){this.head=head;}
        public SelectionShape GetSelectionShape(VoxelWorld world,BlockPos cell)=>shapes[head?1:0,world.BedAt(cell)?.Rotation??0];
        static SelectionShape[,] Build()
        {
            var result=new SelectionShape[2,4];
            Bounds Box(float x,float y,float z,float X,float Y,float Z)=>new Bounds(new Vector3((x+X)/2,(y+Y)/2,(z+Z)/2),new Vector3(X-x,Y-y,Z-z));
            for(int h=0;h<2;h++)for(int r=0;r<4;r++)
            {
                float end=h==0?.02f:.82f;
                var boxes=new[]{Box(.045f,.54f,h==0?.15f:0,.955f,h==0?.89f:.96f,h==0?1:.87f),
                    Box(.02f,.28f,0,.16f,.55f,1),Box(.84f,.28f,0,.98f,.55f,1),
                    Box(.02f,0,end,.16f,.97f,end+.16f),Box(.84f,0,end,.98f,.97f,end+.16f),
                    Box(.1f,.37f,end,.9f,h==0?.66f:.97f,end+.12f)};
                for(int i=0;i<boxes.Length;i++)
                {
                    var box=boxes[i];var c=box.center;var center=r switch {1=>new Vector3(c.z,c.y,1-c.x),2=>new Vector3(1-c.x,c.y,1-c.z),3=>new Vector3(1-c.z,c.y,c.x),_=>c};
                    var size=r%2==0?box.size:new Vector3(box.size.z,box.size.y,box.size.x);boxes[i]=new Bounds(center,size);
                }
                result[h,r]=new SelectionShape(boxes);
            }
            return result;
        }
    }
}
