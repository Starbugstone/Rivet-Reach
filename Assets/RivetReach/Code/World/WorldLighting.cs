using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class VoxelWorld
    {
        struct LightEntry { public int x,y,z,w;public LightEntry(int x,int y,int z,int w){this.x=x;this.y=y;this.z=z;this.w=w;} }
        static readonly byte[][] uniformLightFields=new byte[256][];
        static byte[] UniformLight(byte value)
        {if(uniformLightFields[value]==null){var data=new byte[ChunkLighting.Count];if(value!=0)Array.Fill(data,value);uniformLightFields[value]=data;}return uniformLightFields[value];}
        sealed class LightPage
        {
            public byte[] Values=UniformLight(0);
            public byte Uniform;
            public int Revision,PublishedRevision=-1,Slot=-1;public bool Ready;
        }
        sealed class LightResult
        {public ChunkPos Position;public int Revision,Token;public byte[] Values;public int[] Heights;public int HeightRevision;public double Ms;}
        readonly Dictionary<ChunkPos,LightPage> lightPages=new Dictionary<ChunkPos,LightPage>();
        readonly HashSet<ChunkPos> dirtyLights=new HashSet<ChunkPos>();
        readonly Dictionary<(long,long),(int revision,int[] heights)> lightColumns=new Dictionary<(long,long),(int,int[])>();
        readonly Dictionary<(long,long),int> lightColumnRevisions=new Dictionary<(long,long),int>();
        readonly Stack<int> freeLightSlots=new Stack<int>();
        readonly List<ChunkPos> removeLights=new List<ChunkPos>();
        Task<LightResult> lightWork;
        ComputeBuffer lightBuffer,lightTable;
        ComputeShader lightCopy;
        uint[] lightUpload=new uint[ChunkLighting.Count/4];
        LightEntry[] lightEntries;
        int lightCapacity,nextLightSlot;bool lightTableDirty;BlockPos lightTableOrigin;
        public Func<BlockPos,byte> MachineLight;
        internal bool BypassLightForReview;
        public int PendingLightChunks=>dirtyLights.Count+(lightWork==null?0:1);
        public int LightSolveCount {get;private set;}
        public double LastLightWorkerMs {get;private set;}
        public double LastLightMainMs {get;private set;}
        public double MaxLightMainMs {get;private set;}
        public double TotalLightWorkerMs {get;private set;}
        public long LightGpuBytes=>(long)lightCapacity*ChunkLighting.Count+(lightEntries?.Length??0)*16L;
        public bool LightingReady(BlockPos p)=>Ready(p)&&lightPages.TryGetValue(p.Chunk,out var page)&&page.Ready&&page.PublishedRevision==page.Revision&&!dirtyLights.Contains(p.Chunk);
        // Simulation light is separate from the camera's visibility floor and held glow.
        // Each channel has at most 15 cells of influence; check only that resident region
        // so remote loaders/edits cannot suspend local spawning. Missing neighbours match
        // the solver's zero boundary until they become resident and invalidate the field.
        public bool TryGetSpawnLight(BlockPos p,bool isNight,out byte level)
        {
            level=0;if(!LightingReady(p))return false;
            var min=p.Offset(-15,-15,-15).Chunk;var max=p.Offset(15,15,15).Chunk;
            for(long z=min.Z;z<=max.Z;z++)for(int y=min.Y;y<=max.Y;y++)for(long x=min.X;x<=max.X;x++)
            {
                var key=new ChunkPos(x,y,z);
                if(!chunks.TryGetValue(key,out var resident)||resident.Cells==null)continue;
                if(!lightPages.TryGetValue(key,out var neighbour)||!neighbour.Ready||neighbour.PublishedRevision!=neighbour.Revision||dirtyLights.Contains(key))return false;
            }
            byte value=lightPages[p.Chunk].Values[p.Index];
            level=(byte)Math.Max(value&15,isNight?0:value>>4);return true;
        }
        // Presentation reads solved column heights only; it never generates distant terrain.
        // Missing or invalidated columns suppress precipitation until the solve publishes.
        public bool TryPrecipitationHeight(BlockPos p,out int height)
        {
            height=0;if(!Ready(p)||!lightColumns.TryGetValue((p.Chunk.X,p.Chunk.Z),out var column))return false;
            height=column.heights[(int)(p.X-p.Chunk.Min.X)+32*(int)(p.Z-p.Chunk.Min.Z)];return true;
        }
        public byte PropagatedSkyLight(BlockPos p)=>lightPages.TryGetValue(p.Chunk,out var page)&&page.Ready?(byte)(page.Values[p.Index]>>4):(byte)0;
        public byte GrowthLight(BlockPos p)
        {
            if(!Ready(p))return 0;
            // Direct sky is immediate, including the existing roof-edit contract. Cached
            // propagation supplies cave entrances and placed lights once their solve is ready.
            byte sky=SkyLight(p);
            if(lightPages.TryGetValue(p.Chunk,out var page)&&page.Ready&&page.PublishedRevision==page.Revision)
            {byte v=page.Values[p.Index];return (byte)Math.Max(sky,Math.Max(v>>4,v&15));}
            return sky;
        }
        public void LightSourceChanged(BlockPos p)=>DirtyLight(p.Chunk);
        void DirtyLight(ChunkPos p)
        {
            if(!chunks.TryGetValue(p,out var c)||c.Cells==null)return;
            if(!lightPages.TryGetValue(p,out var page)){page=new LightPage();lightPages.Add(p,page);lightTableDirty=true;}
            page.Revision++;dirtyLights.Add(p);
        }
        void LightingChanged(BlockPos p,byte before,byte after)
        {
            if(BlockId.Opaque(before)!=BlockId.Opaque(after))
            {
                var col=(p.Chunk.X,p.Chunk.Z);lightColumnRevisions.TryGetValue(col,out int revision);lightColumnRevisions[col]=revision+1;lightColumns.Remove(col);
                foreach(var key in chunks.Keys)if(key.X==p.Chunk.X&&key.Z==p.Chunk.Z)DirtyLight(key);
            }
            else if(before==BlockId.Torch||after==BlockId.Torch||Fluids.Registry.Get(before)==Fluids.Lava||Fluids.Registry.Get(after)==Fluids.Lava||before==IndustryId.Lamp||after==IndustryId.Lamp)DirtyLight(p.Chunk);
        }
        void LightingResidency()
        {
            removeLights.Clear();foreach(var p in lightPages.Keys)if(!chunks.ContainsKey(p))removeLights.Add(p);
            foreach(var p in removeLights)
            {
                var page=lightPages[p];if(page.Slot>=0)freeLightSlots.Push(page.Slot);
                lightPages.Remove(p);dirtyLights.Remove(p);lightTableDirty=true;
                foreach(var d in ChunkLighting.Faces)DirtyLight(p.Offset(d.x,d.y,d.z));
            }
            // Column caches are bounded by loaded columns, including remote loader tickets.
            var old=new List<(long,long)>();foreach(var p in lightColumns.Keys)if(!wantedColumns.Contains(p))old.Add(p);
            foreach(var p in old){lightColumns.Remove(p);lightColumnRevisions.Remove(p);}
        }
        void AdvanceLighting()
        {
            long began=Stopwatch.GetTimestamp();
            if(lightWork!=null&&lightWork.IsCompleted)
            {
                var task=lightWork;lightWork=null;
                if(task.IsFaulted){Error=task.Exception?.GetBaseException().Message;}
                else
                {
                    var result=task.Result;
                    if(lightPages.TryGetValue(result.Position,out var page)&&page.Revision==result.Revision&&chunks.TryGetValue(result.Position,out var resident)&&resident.Token==result.Token)
                    {
                        var col=(result.Position.X,result.Position.Z);lightColumnRevisions.TryGetValue(col,out int revision);
                        if(revision==result.HeightRevision)lightColumns[col]=(revision,result.Heights);
                        var old=page.Values;page.Values=result.Values;page.Ready=true;page.PublishedRevision=page.Revision;LastLightWorkerMs=result.Ms;TotalLightWorkerMs+=result.Ms;LightSolveCount++;
                        UploadLight(page);
                        for(int face=0;face<6;face++)
                        {
                            bool changed=false;
                            for(int b=0;b<32&&!changed;b++)for(int a=0;a<32;a++)
                            {int i=BorderIndex(face,a,b);if(old[i]!=page.Values[i]){changed=true;break;}}
                            if(changed){var d=ChunkLighting.Faces[face];DirtyLight(result.Position.Offset(d.x,d.y,d.z));}
                        }
                    }
                    else if(lightPages.ContainsKey(result.Position))dirtyLights.Add(result.Position);
                }
            }
            if(lightWork==null&&dirtyLights.Count>0)
            {
                ChunkPos selected=default;double best=double.MaxValue;var centre=Address(Observer.position).Chunk;
                foreach(var p in dirtyLights){double d=Distance(p,centre);if(d<best){selected=p;best=d;}}
                dirtyLights.Remove(selected);LaunchLight(selected);
            }
            if(lightTableDirty||!lightTableOrigin.Equals(Origin))PublishLightTable();
            LastLightMainMs=(Stopwatch.GetTimestamp()-began)*1000.0/Stopwatch.Frequency;MaxLightMainMs=Math.Max(MaxLightMainMs,LastLightMainMs);
        }
        void LaunchLight(ChunkPos p)
        {
            if(!chunks.TryGetValue(p,out var c)||c.Cells==null||!lightPages.TryGetValue(p,out var page))return;
            var cells=(byte[])c.Cells.Clone();var borders=new byte[6][];var emissions=new byte[ChunkLighting.Count];
            for(int face=0;face<6;face++)
            {
                borders[face]=new byte[1024];var d=ChunkLighting.Faces[face];
                if(lightPages.TryGetValue(p.Offset(d.x,d.y,d.z),out var neighbour)&&neighbour.Ready)
                    for(int b=0;b<32;b++)for(int a=0;a<32;a++)borders[face][a+32*b]=neighbour.Values[BorderIndex(face^1,a,b)];
            }
            for(int z=0;z<32;z++)for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {
                byte id=cells[ChunkMesher.Index(x,y,z)];int i=x+32*(y+32*z);
                if(id==BlockId.Torch)emissions[i]=14;
                else if(id==IndustryId.Lamp)emissions[i]=MachineLight?.Invoke(p.Min.Offset(x,y,z))??0;
                else if(Fluids.Registry.Get(id)==Fluids.Lava)emissions[i]=15;
            }
            var col=(p.X,p.Z);lightColumnRevisions.TryGetValue(col,out int heightRevision);
            int[] cached=lightColumns.TryGetValue(col,out var entry)&&entry.revision==heightRevision?entry.heights:null;
            // Snapshot all vertical versions/edits before leaving the main thread. Light
            // queries never generate/register chunks or change saved generation history.
            var sources=new TerrainGenerator[(TerrainGenerator.MaxY-TerrainGenerator.MinY+1)/32];
            var changes=new Dictionary<BlockPos,byte>();
            if(cached==null)for(int y=0;y<sources.Length;y++)
            {
                var key=new ChunkPos(p.X,TerrainGenerator.MinY/32+y,p.Z);sources[y]=GeneratorFor(key);
                if(edits.TryGetValue(key,out var e))foreach(var pair in e)changes[key.Min.Offset(pair.Key%32,pair.Key/32%32,pair.Key/1024)]=pair.Value;
            }
            int requestRevision=page.Revision,token=c.Token;
            lightWork=Task.Run(()=>
            {
                var clock=Stopwatch.StartNew();int[] heights=cached;
                if(heights==null)
                {
                    heights=new int[1024];var unique=new HashSet<TerrainGenerator>(sources);
                    for(int z=0;z<32;z++)for(int x=0;x<32;x++)
                    {int top=TerrainGenerator.MinY;foreach(var source in unique)top=Math.Max(top,source.OpaqueHeight(p.Min.X+x,p.Min.Z+z));heights[x+32*z]=top;}
                    foreach(var change in changes)if(BlockId.Opaque(change.Value))
                    {int index=(int)(change.Key.X-p.Min.X)+32*(int)(change.Key.Z-p.Min.Z);heights[index]=Math.Max(heights[index],change.Key.Y);}
                    for(int z=0;z<32;z++)for(int x=0;x<32;x++)
                    {
                        int i=x+32*z,top=heights[i];
                        while(top>TerrainGenerator.MinY)
                        {
                            var at=new BlockPos(p.Min.X+x,top,p.Min.Z+z);
                            byte id=changes.TryGetValue(at,out byte edit)?edit:sources[(top-TerrainGenerator.MinY)/32].At(at);
                            if(BlockId.Opaque(id))break;top--;
                        }
                        heights[i]=top;
                    }
                }
                return new LightResult{Position=p,Revision=requestRevision,Token=token,HeightRevision=heightRevision,Heights=heights,Values=ChunkLighting.Solve(cells,p.Min.Y,heights,borders,emissions),Ms=clock.Elapsed.TotalMilliseconds};
            });
        }
        static int BorderIndex(int face,int a,int b)=>face<2?(face==0?31:0)+32*(a+32*b):face<4?a+32*((face==2?31:0)+32*b):a+32*(b+32*(face==4?31:0));
        void UploadLight(LightPage page)
        {
            byte value=page.Values[0];bool uniform=true;foreach(byte b in page.Values)if(b!=value){uniform=false;break;}
            if(uniform)
            {
                if(page.Slot>=0){freeLightSlots.Push(page.Slot);page.Slot=-1;lightTableDirty=true;}
                if(page.Uniform!=value){page.Uniform=value;lightTableDirty=true;}
                page.Values=UniformLight(value);return;
            }
            if(page.Slot<0){page.Slot=freeLightSlots.Count>0?freeLightSlots.Pop():nextLightSlot++;lightTableDirty=true;}
            if(page.Slot>=lightCapacity)
            {
                var previous=lightBuffer;int words=lightCapacity*ChunkLighting.Count/4;
                lightCapacity=Math.Max(16,Mathf.NextPowerOfTwo(page.Slot+1));
                lightBuffer=new ComputeBuffer(lightCapacity*ChunkLighting.Count/4,4);
                if(previous!=null)
                {
                    if(lightCopy==null)lightCopy=Resources.Load<ComputeShader>("LightingCopy");
                    lightCopy.SetBuffer(0,"_Previous",previous);lightCopy.SetBuffer(0,"_Next",lightBuffer);lightCopy.SetInt("_Words",words);
                    int groups=(words+255)/256;lightCopy.Dispatch(0,Math.Min(1024,groups),(groups+1023)/1024,1);previous.Release();
                }
                Shader.SetGlobalBuffer("_RRLightCells",lightBuffer);WriteLight(page);
            }
            else WriteLight(page);
        }
        void WriteLight(LightPage page)
        {
            for(int i=0;i<lightUpload.Length;i++){int j=i*4;lightUpload[i]=(uint)(page.Values[j]|page.Values[j+1]<<8|page.Values[j+2]<<16|page.Values[j+3]<<24);}
            lightBuffer.SetData(lightUpload,0,page.Slot*lightUpload.Length,lightUpload.Length);
        }
        static uint LightHash(int x,int y,int z)=>unchecked((uint)x*73856093u^(uint)y*19349663u^(uint)z*83492791u);
        void PublishLightTable()
        {
            int size=Math.Max(64,Mathf.NextPowerOfTwo(lightPages.Count*2));
            if(lightEntries==null||lightEntries.Length!=size){lightEntries=new LightEntry[size];lightTable?.Release();lightTable=new ComputeBuffer(size,16);}
            Array.Clear(lightEntries,0,lightEntries.Length);
            foreach(var pair in lightPages)
            {
                int x=(int)(pair.Key.X-Origin.X/32),y=pair.Key.Y,z=(int)(pair.Key.Z-Origin.Z/32);
                int slot=(int)(LightHash(x,y,z)&(uint)(size-1));while(lightEntries[slot].w!=0)slot=(slot+1)&(size-1);
                lightEntries[slot]=new LightEntry(x,y,z,pair.Value.Slot<0?-pair.Value.Uniform-1:pair.Value.Slot+1);
            }
            // Keep a valid binding even in a completely dark world or at startup.
            if(lightBuffer==null){lightCapacity=1;lightBuffer=new ComputeBuffer(ChunkLighting.Count/4,4);lightBuffer.SetData(new uint[ChunkLighting.Count/4]);}
            lightTable.SetData(lightEntries);Shader.SetGlobalBuffer("_RRLightTable",lightTable);Shader.SetGlobalBuffer("_RRLightCells",lightBuffer);
            Shader.SetGlobalInt("_RRLightMask",size-1);Shader.SetGlobalFloat("_RRLightEnabled",1);lightTableDirty=false;lightTableOrigin=Origin;
        }
        void LightCamera(UnityEngine.Rendering.ScriptableRenderContext context,Camera camera)
        {Shader.SetGlobalFloat("_RRLightEnabled",!stopped&&!BypassLightForReview&&lightTable!=null&&camera.targetTexture==null?1:0);}
        void StopLighting()
        {UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering-=LightCamera;Shader.SetGlobalFloat("_RRLightEnabled",0);lightBuffer?.Release();lightTable?.Release();lightBuffer=null;lightTable=null;lightPages.Clear();dirtyLights.Clear();lightColumns.Clear();}
    }
}
