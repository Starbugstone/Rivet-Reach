using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class GameUI
    {
        // Reuse the authored item rendering, with no additional camera or model instance.
        // This card belongs to the retained inventory shell and is rebound on every open.
        void BuildStationIllustration(Transform parent)
        {
            var card = Panel(parent, 0, 0, 210, 270, new Color(.065f, .115f, .135f));
            card.name = "Station illustration";
            currentScreen.stationIllustration = card.gameObject;
            Panel(card.transform, 0, 0, 210, 2, gold);
            var grid = new Color(.18f, .27f, .29f, .35f);
            for (int i = 1; i < 7; i++)
            {
                Panel(card.transform, i * 30, 35, 1, 180, grid);
                Panel(card.transform, 15, 35 + i * 30, 180, 1, grid);
            }
            Label(card.transform, "INTERACTING WITH", 12, 12, 186, 20, 11, gold).alignment = TextAnchor.MiddleCenter;
            var art = Rect(card.transform, "Machine graphic", 10, 32, 190, 190).gameObject.AddComponent<RawImage>();
            currentScreen.stationImage = art;
            Panel(card.transform, 16, 227, 178, 1, gold * new Color(1, 1, 1, .4f));
            var name = Label(card.transform, "", 10, 232, 190, 34, 15);
            name.alignment = TextAnchor.MiddleCenter;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            currentScreen.stationImageName = name;
            // Decoration must never intercept inventory dragging or clicks.
            foreach (var graphic in card.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
            card.gameObject.SetActive(false);
        }

        void BindStationIllustration()
        {
            byte id = game.OpenMachine?.Definition.Id ?? game.OpenStation?.Block ?? 0;
            bool visible = id != 0;
            currentScreen.inventoryPortrait.SetActive(!visible);
            currentScreen.stationIllustration.SetActive(visible);
            if (!visible) return;
            var texture = ItemIcon(id);
            currentScreen.stationImage.texture = texture;
            // Fit the full image even if a future authored icon is not square.
            float scale = 190f / Mathf.Max(texture.width, texture.height);
            var size = new Vector2(texture.width, texture.height) * scale;
            var rect = currentScreen.stationImage.rectTransform;
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(10 + (190 - size.x) / 2, -32 - (190 - size.y) / 2);
            currentScreen.stationImageName.text = game.Registry.Get(id).displayName;
        }
    }
}
