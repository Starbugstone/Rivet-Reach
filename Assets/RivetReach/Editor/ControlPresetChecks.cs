using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RivetReach.Editor
{
    public static class ControlPresetChecks
    {
        public static void Run()
        {
            var lines=new List<string>();
            void Check(bool ok,string text){if(!ok)throw new Exception("Control preset: "+text);lines.Add("PASS "+text);}
            var actions=new[]{"Forward","Back","Left","Right","Jump","Sprint","Crouch","Inventory","Drop","Interact","Pause","Inspect","Diagnostics","Previous slot","Next slot"};
            var keys=actions.Select(action=>"binding."+action).Concat(new[]{"binding.movementVersion","mineButton"}).ToArray();
            var original=keys.ToDictionary(key=>key,key=>(exists:PlayerPrefs.HasKey(key),value:PlayerPrefs.GetInt(key)));
            bool hadSensitivity=PlayerPrefs.HasKey("sensitivity");float sensitivity=PlayerPrefs.GetFloat("sensitivity");
            void Defaults()
            {
                foreach(var action in actions)PlayerPrefs.DeleteKey("binding."+action);
                PlayerPrefs.SetInt("binding.movementVersion",1);PlayerPrefs.SetInt("mineButton",1);PlayerPrefs.SetFloat("sensitivity",.23f);
            }
            try
            {
                Defaults();var input=new PlayerInput();var before=new Dictionary<string,Key>(input.Keys);
                Check(input.Keys["Inventory"]==Key.Tab&&input.Keys["Interact"]==Key.E&&!PlayerPrefs.HasKey("binding.Inventory")&&!PlayerPrefs.HasKey("binding.Interact"),
                    "Constructing input retains the established defaults and does not apply the optional preset");
                Check(input.TryApplyEInventoryPreset(out _)&&input.Keys["Inventory"]==Key.E&&input.Keys["Interact"]==Key.F,
                    "Explicit preset applies E inventory and F interact");
                Check(before.All(pair=>pair.Key=="Inventory"||pair.Key=="Interact"||input.Keys[pair.Key]==pair.Value)&&PlayerPrefs.GetInt("mineButton")==1&&input.Sensitivity==.23f&&Inventory.HotbarCount==15,
                    "Preset preserves movement, hotbar actions, mouse Use preference, sensitivity and the15-slot hotbar");
                input=new PlayerInput();Check(input.Keys["Inventory"]==Key.E&&input.Keys["Interact"]==Key.F,"Chosen preset survives input reconstruction");
                before=new Dictionary<string,Key>(input.Keys);Check(input.TryApplyEInventoryPreset(out _)&&before.All(pair=>input.Keys[pair.Key]==pair.Value),"Applying the selected preset again is idempotent");
                Defaults();PlayerPrefs.SetInt("binding.Inventory",(int)Key.I);PlayerPrefs.SetInt("binding.Interact",(int)Key.G);PlayerPrefs.SetInt("binding.Forward",(int)Key.UpArrow);
                input=new PlayerInput();Check(input.Keys["Inventory"]==Key.I&&input.Keys["Interact"]==Key.G&&input.Keys["Forward"]==Key.UpArrow,
                    "Loading custom bindings never silently replaces them with a preset");
                input.BeginRebind("Forward");Check(input.TryApplyEInventoryPreset(out _)&&input.Rebinding==null&&input.Keys["Forward"]==Key.UpArrow,
                    "Choosing the preset cancels pending rebinding and preserves unrelated custom keys");
                foreach(var occupied in new[]{Key.E,Key.F})
                {
                    Defaults();PlayerPrefs.SetInt("binding.Inventory",(int)Key.I);PlayerPrefs.SetInt("binding.Interact",(int)Key.G);PlayerPrefs.SetInt("binding.Jump",(int)occupied);
                    input=new PlayerInput();before=new Dictionary<string,Key>(input.Keys);
                    Check(!input.TryApplyEInventoryPreset(out var reason)&&reason.Contains("Jump")&&reason.Contains(occupied.ToString())&&before.All(pair=>input.Keys[pair.Key]==pair.Value)&&
                        PlayerPrefs.GetInt("binding.Inventory")== (int)Key.I&&PlayerPrefs.GetInt("binding.Interact")== (int)Key.G,
                        "Custom "+occupied+" conflict rejects the complete preset without changing any binding");
                }
                Defaults();PlayerPrefs.SetInt("binding.Inventory",(int)Key.F);PlayerPrefs.SetInt("binding.Interact",(int)Key.E);
                input=new PlayerInput();Check(input.TryApplyEInventoryPreset(out _)&&input.Keys["Inventory"]==Key.E&&input.Keys["Interact"]==Key.F&&input.Keys.Values.Distinct().Count()==input.Keys.Count,
                    "Shared rebind authority swaps an existing E/F pair without duplicate actions");
            }
            finally
            {
                foreach(var pair in original)if(pair.Value.exists)PlayerPrefs.SetInt(pair.Key,pair.Value.value);else PlayerPrefs.DeleteKey(pair.Key);
                if(hadSensitivity)PlayerPrefs.SetFloat("sensitivity",sensitivity);else PlayerPrefs.DeleteKey("sensitivity");PlayerPrefs.Save();
            }
            Directory.CreateDirectory("Logs/ReleaseReview");File.WriteAllLines("Logs/ReleaseReview/control-preset-checks.txt",new[]{"PASS "+lines.Count+" assertions"}.Concat(lines));
        }
    }
}
