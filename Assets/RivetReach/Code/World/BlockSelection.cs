using System;
using UnityEngine;

namespace RivetReach
{
    [Flags]
    public enum BlockTraits { None=0, HasCustomSelectionShape=1, NoMiningDrop=2 }

    // Selection is deliberately independent of movement solidity and rendered meshes.
    // Shapes are immutable, cached definition data; a ray never constructs geometry.
    public sealed class SelectionShape
    {
        readonly Bounds[] boxes;
        public Bounds Bounds {get;}
        public SelectionShape(params Bounds[] boxes)
        {
            if(boxes==null||boxes.Length==0)throw new ArgumentException("Selection needs at least one box.");
            this.boxes=(Bounds[])boxes.Clone();var bounds=boxes[0];
            foreach(var box in boxes)
            {
                if(box.size.x<=0||box.size.y<=0||box.size.z<=0||box.min.x<0||box.min.y<0||box.min.z<0||box.max.x>1||box.max.y>1||box.max.z>1)
                    throw new ArgumentException("Selection boxes must fit inside their owning voxel.");
                bounds.Encapsulate(box);
            }
            Bounds=bounds;
        }
        public static SelectionShape Box(float minX,float minY,float minZ,float maxX,float maxY,float maxZ)
            =>new SelectionShape(new Bounds(new Vector3((minX+maxX)*.5f,(minY+maxY)*.5f,(minZ+maxZ)*.5f),new Vector3(maxX-minX,maxY-minY,maxZ-minZ)));
        public bool Intersect(Vector3 origin,Vector3 direction,float reach,out float distance,out Vector3Int face)
        {
            distance=float.PositiveInfinity;face=Vector3Int.zero;
            foreach(var box in boxes)
            {
                float near=0,far=reach;var normal=Vector3Int.zero;bool hit=true;
                for(int axis=0;axis<3;axis++)
                {
                    float d=direction[axis],o=origin[axis],min=box.min[axis],max=box.max[axis];
                    if(Mathf.Abs(d)<1e-8f){if(o<min||o>max){hit=false;break;}continue;}
                    float a=(min-o)/d,b=(max-o)/d;int sign=-1;
                    if(a>b){float swap=a;a=b;b=swap;sign=1;}
                    if(a>near){near=a;normal=Vector3Int.zero;normal[axis]=sign;}
                    far=Mathf.Min(far,b);if(near>far){hit=false;break;}
                }
                if(hit&&near<=reach&&near<distance){distance=near;face=normal;}
            }
            return !float.IsPositiveInfinity(distance);
        }
    }

    public interface ISelectionShapeProvider
    {
        SelectionShape GetSelectionShape(VoxelWorld world,BlockPos position);
    }

    public readonly struct BlockDefinition
    {
        public readonly BlockTraits Traits;
        public readonly SelectionShape Selection;
        public readonly ISelectionShapeProvider Provider;
        public BlockDefinition(SelectionShape shape,ISelectionShapeProvider provider=null,BlockTraits traits=BlockTraits.None)
        {Selection=shape;Provider=provider;Traits=traits|(shape!=null||provider!=null?BlockTraits.HasCustomSelectionShape:BlockTraits.None);}
        public SelectionShape Shape(VoxelWorld world,BlockPos position)=>Provider?.GetSelectionShape(world,position)??Selection;
    }

    public static class BlockDefinitions
    {
        static readonly BlockDefinition[] definitions=Build();
        public static BlockDefinition Get(byte id)=>definitions[id];
        static BlockDefinition[] Build()
        {
            var result=new BlockDefinition[256]; // Default value is the full-cube fast path.
            result[BlockId.MobSpawner]=new BlockDefinition(null,null,BlockTraits.NoMiningDrop);
            result[BedId.Bed]=new BlockDefinition(null,new BedSelection(false));
            result[BedId.Head]=new BlockDefinition(null,new BedSelection(true));
            result[BlockId.Torch]=new BlockDefinition(null,new AttachedTorchSelection());
            result[BlockId.Sapling]=new BlockDefinition(SelectionShape.Box(.12f,0,.12f,.88f,.85f,.88f));
            foreach(var crop in CropRules.Definitions)for(int id=crop.first;id<=crop.Mature;id++)
            {
                int stage=id-crop.first;
                float height=crop.first==BlockId.PotatoPlant?.28f+.16f*stage:
                    crop.first==FarmId.CarrotPlant?.22f:crop.first==FarmId.MushroomPlant?.36f:.23f+.18f*stage;
                float width=crop.first==BlockId.PotatoPlant?.25f+.18f*stage:.48f+.055f*stage;
                result[id]=new BlockDefinition(SelectionShape.Box(.5f-width*.5f,0,.5f-width*.5f,.5f+width*.5f,height,.5f+width*.5f));
            }
            return result;
        }
        // Attachment state chooses one of five authored cached shapes. This provider
        // owns torch orientation; the central raycaster knows only the capability.
        sealed class AttachedTorchSelection : ISelectionShapeProvider
        {
            static readonly SelectionShape floor=SelectionShape.Box(.39f,.02f,.39f,.61f,.92f,.61f);
            static readonly SelectionShape west=SelectionShape.Box(0,.15f,.39f,.46f,1,.61f);
            static readonly SelectionShape east=SelectionShape.Box(.54f,.15f,.39f,1,1,.61f);
            static readonly SelectionShape south=SelectionShape.Box(.39f,.15f,0,.61f,1,.46f);
            static readonly SelectionShape north=SelectionShape.Box(.39f,.15f,.54f,.61f,1,1);
            public SelectionShape GetSelectionShape(VoxelWorld world,BlockPos position)
            {
                if(!world.TorchSupport(position,out var support)||support.Y<position.Y)return floor;
                return support.X<position.X?west:support.X>position.X?east:support.Z<position.Z?south:north;
            }
        }
    }

    public readonly struct BlockSelectionHit
    {
        public readonly BlockPos Position;
        public readonly byte Block;
        public readonly Vector3 Point;
        public readonly Vector3Int Face;
        public readonly float Distance;
        public BlockSelectionHit(BlockPos position,byte block,Vector3 point,Vector3Int face,float distance)
        {Position=position;Block=block;Point=point;Face=face;Distance=distance;}
    }
}
