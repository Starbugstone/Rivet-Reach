using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class GameUI
    {
        float nextGuidanceRefresh;
        int shownDepth=int.MinValue;
        Text depthOwner;
        void RefreshGuidance()
        {
            if(Time.unscaledTime<nextGuidanceRefresh)return;
            nextGuidanceRefresh=Time.unscaledTime+.25f;
            if(currentScreen.stationSetup!=null&&game.InventoryOpen)
                currentScreen.stationSetup.text=MachineSetupFeedback.For(game);
            if(worldTime==null||!game.Started)return;
            if(currentScreen.depthText==null)
            {
                var panel=Panel(root,874,85,378,150,new Color(.025f,.045f,.055f,.86f));
                panel.name="Depth and navigation";panel.raycastTarget=false;
                Panel(panel.transform,0,0,3,150,gold).raycastTarget=false;
                currentScreen.depthText=Label(panel.transform,"",14,10,350,53,13);
                currentScreen.navigationText=Label(panel.transform,"",14,72,350,70,14,gold);
                panel.transform.SetSiblingIndex(worldTime.transform.GetSiblingIndex());
            }
            var position=game.World.Address(game.Player.transform.position);
            if(shownDepth!=position.Y||depthOwner!=currentScreen.depthText)
            {
                shownDepth=position.Y;depthOwner=currentScreen.depthText;
                depthOwner.text=$"ELEVATION  Y {position.Y}\n"+ResourceGuidance.AtDepth(position.Y,game.Registry);
            }
            string home="Home · use a Bed to set home";
            if(game.Beds.Home.HasValue)
            {
                var bed=game.World.BedAt(game.Beds.Home.Value);
                home=bed!=null&&bed.Identity==game.Beds.HomeIdentity?
                    "Home · "+PlayerNavigation.Bearing(position,bed.Foot,game.Player.Yaw):"Home · bed removed; set another";
            }
            string death=game.Navigation.LastDeath.HasValue?
                "Last death · "+PlayerNavigation.Bearing(position,game.Navigation.LastDeath.Value,game.Player.Yaw):"";
            currentScreen.navigationText.text=home+(death.Length>0?"\n"+death+"\nLocation only · items may be lost":"");
        }
    }
}
