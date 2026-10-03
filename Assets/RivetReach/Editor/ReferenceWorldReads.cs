using System;
using UnityEngine;
namespace RivetReach.Editor
{
    // Frozen pre-optimization ray traversal; the read delegate retains edit-first authority.
    internal sealed class ReferenceWorldReads
    {
        readonly VoxelWorld world;
        readonly Func<BlockPos,byte> read;
        public ReferenceWorldReads(VoxelWorld world,Func<BlockPos,byte> read){this.world=world;this.read=read;}
        BlockPos Address(Vector3 p)=>world.Address(p);
        Vector3 Local(BlockPos p)=>world.Local(p);
        bool Ready(BlockPos p)=>world.Ready(p);
        byte Get(BlockPos p)=>read(p);
        bool Solid(BlockPos p)=>!Ready(p)||BlockId.Solid(Get(p))&&!(world.IsOpenMachine?.Invoke(p)??false);
        public bool Overlaps(Vector3 feet,float width,float height)
        {
            var min=Address(feet+new Vector3(-width/2+.001f,.001f,-width/2+.001f));
            var max=Address(feet+new Vector3(width/2-.001f,height-.001f,width/2-.001f));
            for(long z=min.Z;z<=max.Z;z++)for(int y=min.Y;y<=max.Y;y++)for(long x=min.X;x<=max.X;x++)
            {var cell=new BlockPos(x,y,z);if(Solid(cell)&&(!Ready(cell)||world.OccupiesBlock(feet,width,height,cell,Get(cell))))return true;}
            return false;
        }
        public bool Trace(Vector3 start,Vector3 direction,float reach,out BlockSelectionHit hit,bool fluidSources,bool solidsOnly)
        {
            hit=default;if(reach<0||direction.sqrMagnitude<1e-12f)return false;
            direction.Normalize();var face=Vector3Int.zero;var cell=Address(start);
            Vector3 localCell=Local(cell),step=new Vector3(Math.Sign(direction.x),Math.Sign(direction.y),Math.Sign(direction.z));
            Vector3 delta=new Vector3(direction.x==0?float.PositiveInfinity:Mathf.Abs(1/direction.x),direction.y==0?float.PositiveInfinity:Mathf.Abs(1/direction.y),direction.z==0?float.PositiveInfinity:Mathf.Abs(1/direction.z));
            Vector3 t=new Vector3((direction.x>0?localCell.x+1-start.x:start.x-localCell.x)*delta.x,(direction.y>0?localCell.y+1-start.y:start.y-localCell.y)*delta.y,(direction.z>0?localCell.z+1-start.z:start.z-localCell.z)*delta.z);
            for(int axis=0;axis<3;axis++)if(direction[axis]==0)t[axis]=float.PositiveInfinity;
            float distance=0;
            for(int i=0;i<64&&distance<=reach;i++)
            {
                if(!Ready(cell)){if(solidsOnly){hit=new BlockSelectionHit(cell,0,start+direction*distance,face,distance);return true;}return false;}
                byte b=Get(cell);var fluid=Fluids.Registry.Get(b);
                if(solidsOnly?Solid(cell):b!=0&&(fluid==null||fluidSources&&fluid.IsSource(b)))
                {
                    var definition=BlockDefinitions.Get(b);float selectedDistance=distance;var selectedFace=face;
                    if(solidsOnly&&definition.Collision==null||!solidsOnly&&(definition.Traits&BlockTraits.HasCustomSelectionShape)==0||
                        definition.Shape(world,cell).Intersect(start-Local(cell),direction,reach,out selectedDistance,out selectedFace))
                    {hit=new BlockSelectionHit(cell,b,start+direction*selectedDistance,selectedFace,selectedDistance);return true;}
                }
                int axis=t.x<t.y?(t.x<t.z?0:2):(t.y<t.z?1:2);
                distance=t[axis];t[axis]+=delta[axis];
                face=Vector3Int.zero;face[axis]=-(int)step[axis];
                cell=cell.Offset(axis==0?(int)step.x:0,axis==1?(int)step.y:0,axis==2?(int)step.z:0);
            }
            return false;
        }
    }
}
