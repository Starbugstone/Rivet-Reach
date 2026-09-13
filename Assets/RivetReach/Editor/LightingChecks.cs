using System;
using System.IO;
using UnityEngine;
namespace RivetReach.Editor
{
    public static class LightingChecks
    {
        public static void Run()
        {
            int count=0;void Check(bool ok,string message){count++;if(!ok)throw new Exception(message);}
            var cells=new byte[34*34*34];var heights=new int[1024];var borders=new byte[6][];for(int i=0;i<6;i++)borders[i]=new byte[1024];var emissions=new byte[ChunkLighting.Count];
            Array.Fill(heights,40);var dark=ChunkLighting.Solve(cells,0,heights,borders,emissions);
            Check(Array.TrueForAll(dark,b=>b==0),"Every cell in sealed cave is dark");
            heights[16+32*16]=-1;var shaft=ChunkLighting.Solve(cells,0,heights,borders,emissions);
            Check(shaft[16+32*(2+32*16)]>>4==15,"Vertical shaft carries full skylight");Check(shaft[21+32*(2+32*16)]>>4==10,"Cave entrance loses one level per step");
            Check(shaft[31+32*(2+32*16)]==0,"No sky light beyond fifteen steps");
            for(int z=0;z<32;z++)for(int y=0;y<32;y++)cells[ChunkMesher.Index(18,y,z)]=BlockId.Stone;
            var wall=ChunkLighting.Solve(cells,0,heights,borders,emissions);Check(wall[19+32*(2+32*16)]==0,"Opaque wall stops sky propagation");
            Array.Fill(heights,40);emissions[12+32*(2+32*16)]=14;var torch=ChunkLighting.Solve(cells,0,heights,borders,emissions);
            Check((torch[17+32*(2+32*16)]&15)==9,"Torch supports crops within five path steps");Check((torch[19+32*(2+32*16)]&15)==0,"Torch cannot light through a solid wall");
            emissions[12+32*(2+32*16)]=0;Check(ChunkLighting.Solve(cells,0,heights,borders,emissions)[12+32*(2+32*16)]==0,"Removed source leaves no stale internal light");
            borders[1][2+32*16]=14;var seam=ChunkLighting.Solve(cells,0,heights,borders,emissions);Check((seam[32*(2+32*16)]&15)==13,"Cross-chunk source attenuates once");
            Check(ChunkLighting.Merge(0xf0,14)==0xfe&&ChunkLighting.Fade(0x91)==0x80,"Sky and artificial channels remain independent");
            Directory.CreateDirectory("Logs/Lighting");File.WriteAllText("Logs/Lighting/checks.txt","PASS "+count+" assertions\n");
            Debug.Log("Lighting checks: "+count+" passed");
        }
    }
}
