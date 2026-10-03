using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewPerformancePlan()
        {
            const string pacingKey="display.framePacing", scaleKey="uiScale";
            bool hadPacing=PlayerPrefs.HasKey(pacingKey),hadScale=PlayerPrefs.HasKey(scaleKey);
            int oldPacing=PlayerPrefs.GetInt(pacingKey);float oldScale=PlayerPrefs.GetFloat(scaleKey,1);
            int oldCap=Application.targetFrameRate,oldSync=QualitySettings.vSyncCount;
            bool female=game.Player.Female;int skin=game.Player.Skin;
            try
            {
                PlayerPrefs.SetFloat(scaleKey,1);
                FramePacing.Apply(FramePacing.Mode.Legacy90,true);game.SetMode(ScreenMode.Settings);
                for(int i=0;i<4;i++)
                {
                    var mode=FramePacing.Current;
                    var button=game.UI.VisibleRoot.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text==FramePacing.Label(mode));
                    Check(button.interactable,"Frame pacing choice is visible and interactive: "+mode);
                    Canvas.ForceUpdateCanvases();
                    var buttonBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(game.UI.VisibleRoot,button.transform);
                    Check(game.UI.VisibleRoot.GetComponentsInChildren<Slider>().All(slider=>!buttonBounds.Intersects(
                        RectTransformUtility.CalculateRelativeRectTransformBounds(game.UI.VisibleRoot,slider.handleRect))),
                        "Frame pacing button does not overlap slider handles");
                    yield return Capture("settings-"+mode);
                    button.onClick.Invoke();yield return null;
                    Check(FramePacing.Current==FramePacing.Next(mode),"Settings button persists the next frame pacing mode");
                }
                var scale=game.UI.VisibleRoot.GetComponentsInChildren<Slider>().Single(slider=>slider.minValue==.85f&&slider.maxValue==1);
                scale.value=.85f;
                Check(PlayerPrefs.GetFloat(scaleKey)==.85f&&game.UI.VisibleRoot.localScale==Vector3.one*.85f,"Interface scale control applies and stores 0.85");
                yield return Capture("settings-scale085");
                game.Diagnostics=true;game.SetMode(ScreenMode.Play);yield return new WaitForSecondsRealtime(.4f);
                var diagnostics=game.UI.VisibleRoot.GetComponentsInChildren<Text>().Single(t=>t.text.Contains("Light invalidations source"));
                Check(diagnostics.preferredHeight<=diagnostics.rectTransform.rect.height+1,"Expanded diagnostics header fits at interface scale 0.85");
                Check(game.MaxSimulationTicks<=WorldSurvival.MaxTicksPerFrame,"Actual rendered-frame catch-up respects the three-tick bound");
                yield return Capture("timing-overlay");
                PlayerPrefs.SetFloat(scaleKey,1);game.Diagnostics=false;
                foreach(bool model in new[]{false,true})foreach(int selectedSkin in new[]{0,1})
                {
                    game.SetAppearance(model,selectedSkin);game.SetMode(ScreenMode.Inventory);
                    yield return Capture("portrait-"+(model?"female":"male")+"-"+selectedSkin);
                }
            }
            finally
            {
                if(hadPacing)PlayerPrefs.SetInt(pacingKey,oldPacing);else PlayerPrefs.DeleteKey(pacingKey);
                if(hadScale)PlayerPrefs.SetFloat(scaleKey,oldScale);else PlayerPrefs.DeleteKey(scaleKey);
                PlayerPrefs.Save();Application.targetFrameRate=oldCap;QualitySettings.vSyncCount=oldSync;
                game.SetAppearance(female,skin);
            }
        }
    }
}
