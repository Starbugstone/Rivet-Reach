using System;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    [CustomEditor(typeof(RecipeAsset))]
    public sealed class RecipeAssetEditor : UnityEditor.Editor
    {
        string[] itemIds, labels;
        void OnEnable()
        {
            var registry=AssetDatabase.LoadAssetAtPath<ItemRegistry>("Assets/RivetReach/Resources/Definitions/Items.asset");
            int count=registry==null?0:registry.items.Length;
            itemIds=new string[count+1];labels=new string[count+1];itemIds[0]="";labels[0]="Empty";
            for(int i=0;i<count;i++){itemIds[i+1]=registry.items[i].stableId;labels[i+1]=registry.items[i].displayName;}
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stableId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("kind"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minimumGridSize"));
            bool shaped=serializedObject.FindProperty("kind").enumValueIndex==(int)RecipeKind.Shaped;
            var cells=serializedObject.FindProperty("ingredients");
            if(shaped)
            {
                var width=serializedObject.FindProperty("width");var height=serializedObject.FindProperty("height");
                EditorGUILayout.PropertyField(width);EditorGUILayout.PropertyField(height);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mirror"),new GUIContent("Horizontal mirror"));
                int expected=width.intValue*height.intValue;
                if(cells.arraySize!=expected)
                {
                    int old=cells.arraySize;cells.arraySize=expected;
                    for(int i=old;i<expected;i++){var cell=cells.GetArrayElementAtIndex(i);cell.FindPropertyRelative("itemId").stringValue="";cell.FindPropertyRelative("count").intValue=0;}
                }
                EditorGUILayout.HelpBox("Top-left origin; blank borders are ignored. Inner empty cells matter. Rotation is not automatic. Width changes reinterpret the row-major cells.",MessageType.Info);
                for(int y=0;y<height.intValue;y++)
                {
                    EditorGUILayout.BeginHorizontal();
                    for(int x=0;x<width.intValue;x++)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox,GUILayout.MinWidth(55));
                        EditorGUILayout.LabelField($"{x+1}, {y+1}",EditorStyles.miniLabel);
                        Cell(cells.GetArrayElementAtIndex(y*width.intValue+x));
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                serializedObject.FindProperty("mirror").boolValue=false;
                EditorGUILayout.HelpBox("Each entry occupies one input slot. Repeated items require separate slots; counts specify how many to consume from each slot.",MessageType.Info);
                EditorGUILayout.PropertyField(cells,true);
            }
            EditorGUILayout.Space();EditorGUILayout.LabelField("Output",EditorStyles.boldLabel);
            Cell(serializedObject.FindProperty("output"));
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("Add this asset to Definitions/Recipes.asset. Changes take effect on the next Play session. Validate the catalog before building.",MessageType.Info);
            if(GUILayout.Button("Validate active recipe catalog"))
            {
                try{int count=RecipeCatalogAsset.Load().Compile(ItemRegistry.Load()).Recipes.Count;Debug.Log($"Validated {count} crafting recipes.");}
                catch(Exception ex){Debug.LogError(ex.Message);}
            }
        }
        void Cell(SerializedProperty cell)
        {
            var id=cell.FindPropertyRelative("itemId");var count=cell.FindPropertyRelative("count");
            int selected=Array.IndexOf(itemIds,id.stringValue??"");
            if(selected<0){EditorGUILayout.PropertyField(id,GUIContent.none);EditorGUILayout.HelpBox("Unknown item ID",MessageType.Error);}
            else
            {
                int next=EditorGUILayout.Popup(selected,labels);
                if(next!=selected){id.stringValue=itemIds[next];count.intValue=next==0?0:Math.Max(1,count.intValue);}
            }
            count.intValue=EditorGUILayout.IntField(count.intValue);
        }
    }
}
