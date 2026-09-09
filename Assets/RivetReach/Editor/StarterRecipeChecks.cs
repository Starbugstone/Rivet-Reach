using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    // Independent acceptance fixtures: changing an authored recipe must not silently
    // change the expected Minecraft-style layout alongside the existing catalog tests.
    public static class StarterRecipeChecks
    {
        public static int Run(ItemRegistry items,RecipeRegistry registry)
        {
            int checks=0;var verified=new HashSet<byte>();var lines=new List<string>();
            void Check(bool value,string message){checks++;if(!value)throw new Exception("Starter recipe: "+message);}
            int Limit(byte id)=>items.Get(id).stackLimit;
            void Recipe(byte output,int count,byte material,string pattern,int minimum=3,bool mirror=false,bool shapeless=false)
            {
                var rows=pattern.Split('/');int width=rows[0].Length,height=rows.Length;
                byte Ingredient(char symbol)=>symbol=='M'?material:symbol=='S'?BlockId.Stick:(byte)0;
                var authored=registry.Recipes.SingleOrDefault(r=>r.Output.Id==output);
                Check(authored!=null,"Missing "+items.Get(output).displayName);
                Check(authored.Output.Count==count&&authored.MinimumGridSize==minimum,"Quantity/grid gate: "+authored.Id);
                Check(authored.Kind==(shapeless?RecipeKind.Shapeless:RecipeKind.Shaped)&&authored.AllowsMirroring==mirror,"Shape/mirror rule: "+authored.Id);
                Check((shapeless||(authored.Width==width&&authored.Height==height))&&authored.Ingredients.Count==width*height,"Dimensions: "+authored.Id);
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                {
                    byte id=Ingredient(rows[y][x]);var actual=authored.Ingredients[y*width+x];
                    Check(id==0?actual.Empty:actual.Id==id&&actual.Count==1,"Ingredient position/quantity: "+authored.Id);
                }
                for(int size=minimum;size<=4;size++)for(int dy=0;dy<=size-height;dy++)for(int dx=0;dx<=size-width;dx++)
                    for(int flip=0;flip<=(mirror?1:0);flip++)
                    {
                        var session=new CraftingSession(registry,size,Limit);
                        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                        {
                            byte id=Ingredient(rows[y][flip==0?x:width-1-x]);int slot=(dy+y)*size+dx+x;
                            if(id!=0)session.Grid.Add(id,1,slot,slot+1);
                        }
                        Check(session.Preview?.Output.Id==output,"Matching translated/mirrored recipe: "+authored.Id);
                        ItemStack cursor=default;Check(session.CraftToCursor(ref cursor).Succeeded&&cursor.Id==output&&cursor.Count==count&&session.Grid.Slots.All(s=>s.Empty),"Exact consumption/output: "+authored.Id);
                    }
                verified.Add(output);lines.Add(items.Get(output).displayName+" × "+count+" | "+pattern+" | M="+items.Get(material).displayName+"; S=Stick | min "+minimum+"×"+minimum+(mirror?" | horizontal mirror":""));
            }
            Recipe(BlockId.Planks,4,BlockId.Log,"M",2,shapeless:true);
            Recipe(BlockId.Stick,4,BlockId.Planks,"M/M",2);
            Recipe(BlockId.Workbench,1,BlockId.Planks,"MM/MM",2);
            Recipe(BlockId.Furnace,1,BlockId.Cobblestone,"MMM/M.M/MMM");
            Recipe(BlockId.Chest,1,BlockId.Planks,"MMM/M.M/MMM");
            byte[] materials={BlockId.Planks,BlockId.Cobblestone,BlockId.CopperIngot,BlockId.IronIngot,BlockId.Diamond};
            for(int tier=0;tier<materials.Length;tier++)
            {
                byte first=(byte)(BlockId.WoodAxe+tier*5),material=materials[tier];
                Recipe(first,1,material,"MM/MS/.S",mirror:true);
                Recipe((byte)(first+1),1,material,"MMM/.S./.S.");
                Recipe((byte)(first+2),1,material,"M/M/S");
                Recipe((byte)(first+3),1,material,"M/S/S");
                Recipe((byte)(first+4),1,material,"MM/.S/.S",mirror:true);
            }
            for(int tier=0;tier<3;tier++)
            {
                byte first=(byte)(70+tier*4),material=materials[tier+2];
                Recipe(first,1,material,"MMM/M.M");Recipe((byte)(first+1),1,material,"M.M/MMM/MMM");
                Recipe((byte)(first+2),1,material,"MMM/M.M/M.M");Recipe((byte)(first+3),1,material,"M.M/M.M");
            }
            byte[] stored={BlockId.Coal,BlockId.CopperIngot,BlockId.IronIngot,BlockId.GoldIngot,BlockId.Diamond};
            byte[] blocks={BlockId.CoalBlock,BlockId.CopperBlock,BlockId.IronBlock,BlockId.GoldBlock,BlockId.DiamondBlock};
            for(int i=0;i<stored.Length;i++)
            {
                byte block=blocks[i];Recipe(block,1,stored[i],"MMM/MMM/MMM");
                Recipe(stored[i],9,block,"M",2,shapeless:true);
            }
            Check(verified.Count==52&&registry.Recipes.Count==52,"The active survival catalog contains exactly the reviewed 52 recipes");
            var invalid=new CraftingSession(registry,2,Limit);
            invalid.Grid.Add(BlockId.Planks,1,0,1);invalid.Grid.Add(BlockId.Planks,1,1,2);
            Check(invalid.Preview==null,"Horizontal planks cannot substitute for vertical sticks");
            invalid.Grid.Take(0,1);invalid.Grid.Take(1,1);
            invalid.Grid.Add(BlockId.Log,1,0,1);invalid.Grid.Add(BlockId.Log,1,1,2);invalid.Grid.Add(BlockId.Log,1,2,3);invalid.Grid.Add(BlockId.Log,1,3,4);
            Check(invalid.Preview==null,"Logs cannot substitute for planks in a workbench");
            invalid.Grid.Take(0,1);invalid.Grid.Take(1,1);invalid.Grid.Take(2,1);invalid.Grid.Take(3,1);
            invalid.Grid.Add(BlockId.Stone,1,0,1);invalid.Grid.Add(BlockId.Stone,1,1,2);invalid.Grid.Add(BlockId.Log,1,3,4);
            Check(invalid.Preview==null,"Obsolete stone/log starter axe is unavailable");
            string report=$"PASS: {checks} independent recipe acceptance checks; all 52 active layouts and output quantities.\nDots are empty cells; slashes separate rows.\n"+string.Join("\n",lines)+"\n";
            File.WriteAllText("Logs/starter-recipe-checks.txt",report);Debug.Log(report);return checks;
        }
    }
}
