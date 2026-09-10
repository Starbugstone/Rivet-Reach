using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    [CustomPropertyDrawer(typeof(ItemBrowserSettings.Entry))]
    public sealed class ItemBrowserEntryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => (EditorGUIUtility.singleLineHeight + 3) * 4 + 5;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var row = new Rect(position.x, position.y + 2, position.width, EditorGUIUtility.singleLineHeight);
            var id = property.FindPropertyRelative("itemId");
            var registry = ItemRegistry.Load();
            if (registry == null) EditorGUI.PropertyField(row, id);
            else
            {
                var items = registry.items.OrderBy(i => i.displayName, StringComparer.OrdinalIgnoreCase).ToArray();
                var choices = new[] { string.IsNullOrEmpty(id.stringValue) ? "Choose item…" : "Unknown: " + id.stringValue }
                    .Concat(items.Select(i => i.displayName + " (" + i.stableId + ")")).ToArray();
                int current = Array.FindIndex(items, i => i.stableId == id.stringValue) + 1;
                int selected = EditorGUI.Popup(row, "Item", current, choices);
                if (selected > 0) id.stringValue = items[selected - 1].stableId;
            }
            foreach (string field in new[] { "icon", "searchKeywords", "sortOrder" })
            { row.y += EditorGUIUtility.singleLineHeight + 3; EditorGUI.PropertyField(row, property.FindPropertyRelative(field)); }
            EditorGUI.EndProperty();
        }
    }
}
