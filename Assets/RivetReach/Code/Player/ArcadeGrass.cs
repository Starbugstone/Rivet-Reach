using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    // Bounded cosmetic geometry on actual, exposed natural grass cells near the observer.
    public sealed class ArcadeGrass : MonoBehaviour
    {
        public int TuftCount {get;private set;}
        Expedition game;VoxelWorld world;Mesh mesh;Material material;GameObject view;
        BlockPos centre;bool dirty=true;float retry;
        readonly List<Vector3> vertices=new List<Vector3>(6000);
        readonly List<Vector3> normals=new List<Vector3>(6000);
        readonly List<Vector2> uv=new List<Vector2>(6000);
        readonly List<int> indices=new List<int>(12000);
        void Awake()
        {
            game=GetComponentInParent<Expedition>();
            view=new GameObject("Nearby grass blades");view.transform.SetParent(transform,false);
            mesh=new Mesh{name="Bounded natural grass tufts"};mesh.MarkDynamic();view.AddComponent<MeshFilter>().sharedMesh=mesh;
            material=new Material(Shader.Find("RivetReach/ArcadeGrass"));var renderer=view.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;Bind();
        }
        void Bind()
        {
            if(world!=null){world.BlockChanged-=Changed;world.OriginShifted-=Shift;}
            world=game.World;world.BlockChanged+=Changed;world.OriginShifted+=Shift;dirty=true;mesh.Clear();
        }
        void Changed(BlockPos cell)
        {if(System.Math.Abs(cell.X-centre.X)<=18&&System.Math.Abs(cell.Z-centre.Z)<=18)dirty=true;}
        void Shift(Vector3 offset){view.transform.position-=offset;dirty=true;}
        void LateUpdate()
        {
            if(world!=game.World)Bind();
            view.SetActive(game.Started);if(!game.Started||game.Paused)return;
            var at=world.Address(game.Player.transform.position);
            if(System.Math.Abs(at.X-centre.X)>4||System.Math.Abs(at.Z-centre.Z)>4)dirty=true;
            if(Time.time>retry&&world.PendingCount>0){dirty=true;retry=Time.time+1;}
            if(!dirty)return;dirty=false;centre=at;Rebuild();
        }
        void Rebuild()
        {
            vertices.Clear();normals.Clear();uv.Clear();indices.Clear();TuftCount=0;
            long startX=BlockPos.FloorDiv(centre.X,2)*2-16,startZ=BlockPos.FloorDiv(centre.Z,2)*2-16;
            for(int z=0;z<17;z++)for(int x=0;x<17;x++)
            {
                long wx=startX+x*2,wz=startZ+z*2;
                uint hash=TerrainGenerator.Hash(wx,0,wz,world.Generator.Seed);
                if(hash%5==0||(wx-centre.X)*(wx-centre.X)+(wz-centre.Z)*(wz-centre.Z)>16*16)continue;
                int y=world.Generator.Height(wx,wz);var cell=new BlockPos(wx,y,wz);
                if(!world.Ready(cell)||world.Get(cell)!=BlockId.Grass||world.Get(cell.Offset(0,1,0))!=0)continue;
                Vector3 root=world.Local(cell)+new Vector3(.25f+(hash%100)*.005f,1.002f,.25f+((hash>>8)%100)*.005f);
                for(int blade=0;blade<4;blade++)
                {
                    float angle=(hash%628)*.01f+blade*1.65f,height=.18f+((hash>>(blade*4))%100)*.0019f;
                    var across=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var forward=Vector3.Cross(across,Vector3.up);
                    Vector3 p=root+across*.055f;int start=vertices.Count;
                    vertices.Add(p-across*.022f);vertices.Add(p+across*.022f);
                    vertices.Add(p+Vector3.up*height*.62f+forward*.03f-across*.018f);vertices.Add(p+Vector3.up*height*.62f+forward*.03f+across*.018f);
                    vertices.Add(p+Vector3.up*height+forward*.08f);
                    uv.Add(new Vector2(0,0));uv.Add(new Vector2(1,0));uv.Add(new Vector2(0,.62f));uv.Add(new Vector2(1,.62f));uv.Add(new Vector2(.5f,1));
                    for(int k=0;k<5;k++)normals.Add((forward+Vector3.up*.35f).normalized);
                    indices.Add(start);indices.Add(start+2);indices.Add(start+1);
                    indices.Add(start+1);indices.Add(start+2);indices.Add(start+3);
                    indices.Add(start+2);indices.Add(start+4);indices.Add(start+3);
                }
                TuftCount++;
            }
            view.transform.localPosition=Vector3.zero;mesh.Clear();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
            var bounds=mesh.bounds;bounds.Expand(.3f);mesh.bounds=bounds;
        }
        void OnDestroy()
        {if(world!=null){world.BlockChanged-=Changed;world.OriginShifted-=Shift;}if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
