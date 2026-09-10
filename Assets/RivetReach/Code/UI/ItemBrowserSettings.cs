using System;
using UnityEngine;

namespace RivetReach
{
    [CreateAssetMenu(menuName = "Rivet Reach/Interface/Item browser settings")]
    public sealed class ItemBrowserSettings : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string itemId;
            public Texture2D icon;
            public string searchKeywords;
            public int sortOrder;
        }
        [Tooltip("Optional overrides. Every registered item appears even without an entry. Restart Play after edits.")]
        public Entry[] entries = Array.Empty<Entry>();
    }
}
