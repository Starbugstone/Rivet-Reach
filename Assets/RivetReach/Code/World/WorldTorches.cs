using System;
using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class VoxelWorld
    {
        // Attachments belong to the session, independently of streamed views and lights.
        readonly Dictionary<ChunkPos,Dictionary<BlockPos,BlockPos>> torchSupports=new Dictionary<ChunkPos,Dictionary<BlockPos,BlockPos>>();
        public TorchPresentation TorchView {get;private set;}
        public bool TorchSupport(BlockPos cell,out BlockPos support)
        {
            support=default;
            return torchSupports.TryGetValue(cell.Chunk,out var page)&&page.TryGetValue(cell,out support);
        }
        public bool CanPlaceTorch(BlockPos cell,BlockPos support)
        {
            long dx=cell.X-support.X,dz=cell.Z-support.Z;int dy=cell.Y-support.Y;
            return Ready(cell)&&Ready(support)&&Get(cell)==BlockId.Air&&BlockId.Solid(Get(support))&&
                Math.Abs(dx)+Math.Abs(dz)+Math.Abs(dy)==1&&dy>=0&&cell.Y>TerrainGenerator.MinY&&cell.Y<=TerrainGenerator.MaxY;
        }
        public bool PlaceTorch(BlockPos cell,BlockPos support)
        {
            if(!CanPlaceTorch(cell,support))return false;
            if(!torchSupports.TryGetValue(cell.Chunk,out var page)){page=new Dictionary<BlockPos,BlockPos>();torchSupports.Add(cell.Chunk,page);}
            page.Add(cell,support);
            if(Change(cell,BlockId.Air,BlockId.Torch))return true;
            page.Remove(cell);if(page.Count==0)torchSupports.Remove(cell.Chunk);return false;
        }
        public IEnumerable<KeyValuePair<BlockPos,BlockPos>> NearbyTorches(BlockPos observer)
        {
            // Fixed chunk lookup; distant session edits are never scanned for presentation.
            for(int z=-2;z<=2;z++)for(int y=-2;y<=2;y++)for(int x=-2;x<=2;x++)
                if(torchSupports.TryGetValue(observer.Chunk.Offset(x,y,z),out var page))
                    foreach(var entry in page)if(Ready(entry.Key))yield return entry;
        }
        void TorchChanged(BlockPos cell,byte before,byte after)
        {
            if(before==BlockId.Torch&&after!=BlockId.Torch&&torchSupports.TryGetValue(cell.Chunk,out var page))
            {
                page.Remove(cell);if(page.Count==0)torchSupports.Remove(cell.Chunk);
                // Mining emits its own drop; fluid replacement also recovers exactly one torch.
                if(Fluids.IsFluid(after))BlockMined?.Invoke(cell,BlockId.Torch);
            }
            if(!BlockId.Solid(after))
            {
                // Only the five possible attachments can depend on this support.
                Detach(cell.Offset(0,1,0));Detach(cell.Offset(1,0,0));Detach(cell.Offset(-1,0,0));
                Detach(cell.Offset(0,0,1));Detach(cell.Offset(0,0,-1));
            }
            if(before==BlockId.Torch||after==BlockId.Torch)TorchView?.Refresh();
            void Detach(BlockPos attached)
            {
                if(TorchSupport(attached,out var support)&&support.Equals(cell)&&Change(attached,BlockId.Torch,BlockId.Air,false,false))
                    BlockMined?.Invoke(attached,BlockId.Torch);
            }
        }
    }
}
