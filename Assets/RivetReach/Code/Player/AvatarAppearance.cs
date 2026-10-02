using System;
using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    // Explorer appearance generations and layered region tints.
    // Generation 2 is the machinist rework (Tools/create_explorer_v2.py). Generation 1 keeps the
    // original explorers and SkinField/SkinOchre untouched; set Generation to 1 to revert.
    public static class AvatarAppearance
    {
        public static readonly int Generation=2;
        const string V2="Characters/V2/";
        // Cells hidden under a helmet when above the brow: hair, goggle strap/lens and their fittings.
        static readonly HashSet<int> helmetCells=new HashSet<int>{34,35,42,43,40,37,48,49,56,57,54,55,62,63};

        public static string ModelPath(bool female)=>(Generation>=2?V2:"Characters/")+(female?"ExplorerFemale":"ExplorerMale");
        public static int SkinCount=>Generation>=2?Palette.presets.Length:2;
        public static string SkinLabel(int skin)
        {
            if(Generation<2)return skin==0?"FIELD / TEAL":"OCHRE / SLATE";
            var preset=Palette.presets[Mathf.Clamp(skin,0,Palette.presets.Length-1)];
            return preset.name.ToUpperInvariant()+" / "+preset.vestName.ToUpperInvariant();
        }
        public static Texture2D BaseMap(int skin)=>Resources.Load<Texture2D>(Generation>=2?V2+"SkinBase":"Characters/"+(skin==0?"SkinField":"SkinOchre"));

        // Configure a per-avatar copy of Materials/Player for the selected skin.
        public static void Apply(Material material,int skin)
        {
            material.SetTexture("_BaseMap",BaseMap(skin));
            if(Generation<2)return;
            material.SetTexture("_SurfaceMap",Resources.Load<Texture2D>(V2+"SkinSurface"));
            material.SetTexture("_BumpMap",Resources.Load<Texture2D>(V2+"SkinNormal"));
            material.SetTexture("_TintMap",TintMap(Mathf.Clamp(skin,0,Palette.presets.Length-1)));
            material.SetFloat("_FullDetail",1);material.SetFloat("_VertexOcclusion",1);
        }

        // Second generation: hide by tint cell; first generation: hair tile 9 only.
        public static bool HiddenUnderHelmet(Vector2 uv)
        {
            if(Generation<2)return Mathf.FloorToInt(uv.x*4)+Mathf.FloorToInt(uv.y*4)*4==9;
            int cell=Mathf.Clamp(Mathf.FloorToInt(uv.x*8),0,7)+Mathf.Clamp(Mathf.FloorToInt(uv.y*8),0,7)*8;
            return helmetCells.Contains(cell);
        }

        [Serializable] public sealed class Preset
        {
            public string name,vestName,skin,hair,eyes,shirt,vest,trousers,leather,boots,brass,copper,accent;
            public string Layer(string layer)=>layer switch
            {
                "skin"=>skin,"hair"=>hair,"eyes"=>eyes,"shirt"=>shirt,"vest"=>vest,"trousers"=>trousers,"leather"=>leather,
                "boots"=>boots,"brass"=>brass,"copper"=>copper,"accent"=>accent,_=>null
            };
        }
        [Serializable] public sealed class Palettes {public string[] skin,hair,eyes,cloth,leather,metal;}
        [Serializable] public sealed class PaletteData {public int version,grid;public string[] layers,cells;public Palettes palettes;public Preset[] presets;}

        static PaletteData palette;
        public static PaletteData Palette
        {
            get
            {
                if(palette!=null)return palette;
                var asset=Resources.Load<TextAsset>(V2+"AppearancePalette");
                palette=asset!=null?JsonUtility.FromJson<PaletteData>(asset.text):null;
                if(palette==null||palette.cells==null||palette.cells.Length!=64||palette.presets==null||palette.presets.Length==0)
                    throw new InvalidOperationException("Invalid explorer appearance palette");
                return palette;
            }
        }

        static readonly Dictionary<int,Texture2D> tints=new Dictionary<int,Texture2D>();
        // One texel per atlas cell; the shader point-samples it with the skin UV.
        public static Texture2D TintMap(int preset)
        {
            if(tints.TryGetValue(preset,out var cached)&&cached!=null)return cached;
            var data=Palette;var p=data.presets[preset];
            var texture=new Texture2D(data.grid,data.grid,TextureFormat.RGBA32,false,false)
                {name="Explorer tint "+p.name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};
            var pixels=new Color32[data.grid*data.grid];
            for(int i=0;i<pixels.Length;i++)
            {
                string hex=data.cells[i]=="fixed"?null:p.Layer(data.cells[i]);
                pixels[i]=hex!=null&&ColorUtility.TryParseHtmlString(hex,out var colour)?(Color32)colour:new Color32(255,255,255,255);
            }
            texture.SetPixels32(pixels);texture.Apply(false,true);tints[preset]=texture;return texture;
        }
    }
}
