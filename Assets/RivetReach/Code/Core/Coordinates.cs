using System;
using UnityEngine;

namespace RivetReach
{
    // Authoritative addresses are never floating-point Unity positions.
    [Serializable]
    public readonly struct BlockPos : IEquatable<BlockPos>
    {
        public readonly long X, Z;
        public readonly int Y;
        public BlockPos(long x, int y, long z) { X = x; Y = y; Z = z; }
        public static long FloorDiv(long v, int d) => v >= 0 ? v / d : (v + 1) / d - 1;
        public ChunkPos Chunk => new ChunkPos(FloorDiv(X, 32), (int)FloorDiv(Y, 32), FloorDiv(Z, 32));
        public int Index => (int)(X - Chunk.X * 32) + 32 * ((Y - Chunk.Y * 32) + 32 * (int)(Z - Chunk.Z * 32));
        public BlockPos Offset(int x, int y, int z) => new BlockPos(checked(X+x), checked(Y+y), checked(Z+z));
        public bool Equals(BlockPos p) => X == p.X && Y == p.Y && Z == p.Z;
        public override bool Equals(object o) => o is BlockPos p && Equals(p);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public override string ToString() => $"{X}, {Y}, {Z}";
    }

    public readonly struct ChunkPos : IEquatable<ChunkPos>
    {
        public readonly long X, Z;
        public readonly int Y;
        public ChunkPos(long x, int y, long z) { X=x; Y=y; Z=z; }
        public BlockPos Min => new BlockPos(X*32, Y*32, Z*32);
        public ChunkPos Offset(int x,int y,int z) => new ChunkPos(X+x,Y+y,Z+z);
        public bool Equals(ChunkPos p) => X==p.X && Y==p.Y && Z==p.Z;
        public override bool Equals(object o) => o is ChunkPos p && Equals(p);
        public override int GetHashCode() => HashCode.Combine(X,Y,Z);
    }

    [Serializable]
    public readonly struct WorldPoint
    {
        public readonly BlockPos Cell;
        public readonly Vector3 Fraction;
        public WorldPoint(BlockPos cell, Vector3 fraction) { Cell=cell; Fraction=fraction; }
        public static WorldPoint FromLocal(Vector3 p, BlockPos origin)
        {
            int x=Mathf.FloorToInt(p.x), y=Mathf.FloorToInt(p.y), z=Mathf.FloorToInt(p.z);
            return new WorldPoint(origin.Offset(x,y,z), p-new Vector3(x,y,z));
        }
        public Vector3 Local(BlockPos origin) => new Vector3((float)(Cell.X-origin.X),Cell.Y-origin.Y,(float)(Cell.Z-origin.Z))+Fraction;
    }
}
