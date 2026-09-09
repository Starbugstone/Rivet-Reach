using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RivetReach
{
    public sealed class PlayerInput
    {
        public readonly Dictionary<string,Key> Keys=new Dictionary<string,Key>{
            {"Forward",Key.W},{"Back",Key.S},{"Left",Key.A},{"Right",Key.D},{"Jump",Key.Space},
            {"Sprint",Key.LeftShift},{"Crouch",Key.LeftCtrl},{"Inventory",Key.Tab},{"Drop",Key.Q},
            {"Pause",Key.Escape},{"Inspect",Key.F5},{"Diagnostics",Key.F12},
            {"Previous slot",Key.LeftBracket},{"Next slot",Key.RightBracket}};
        public float Sensitivity=0.11f;
        public string Rebinding {get;private set;}
        float rebindAfter;
        public PlayerInput()
        {
            foreach(var n in new List<string>(Keys.Keys))Keys[n]=(Key)PlayerPrefs.GetInt("binding."+n,(int)Keys[n]);
            // Migrate the previous default when it was stored by key rebinding.
            if(Keys["Diagnostics"]==Key.F3)
            {
                foreach(var n in new List<string>(Keys.Keys))if(n!="Diagnostics"&&Keys[n]==Key.F12)Keys[n]=Key.F3;
                Keys["Diagnostics"]=Key.F12;
            }
            Sensitivity=PlayerPrefs.GetFloat("sensitivity",.11f);
        }
        public bool Held(string name) => Rebinding==null && Keyboard.current!=null&&Keyboard.current[Keys[name]].isPressed;
        public bool Pressed(string name) => Rebinding==null && Keyboard.current!=null&&Keyboard.current[Keys[name]].wasPressedThisFrame;
        public Vector2 Move => new Vector2((Held("Right")?1:0)-(Held("Left")?1:0),(Held("Forward")?1:0)-(Held("Back")?1:0)).normalized;
        public Vector2 Look => Mouse.current==null?Vector2.zero:Mouse.current.delta.ReadValue()*Sensitivity;
        public bool Mine => Rebinding==null&&Mouse.current!=null&&(PlayerPrefs.GetInt("mineButton",0)==0?Mouse.current.leftButton.isPressed:Mouse.current.rightButton.isPressed);
        public bool Place => Rebinding==null&&Mouse.current!=null&&(PlayerPrefs.GetInt("mineButton",0)==0?Mouse.current.rightButton.isPressed:Mouse.current.leftButton.isPressed);
        public bool PlacePressed => Rebinding==null&&Mouse.current!=null&&(PlayerPrefs.GetInt("mineButton",0)==0?Mouse.current.rightButton.wasPressedThisFrame:Mouse.current.leftButton.wasPressedThisFrame);
        public void BeginRebind(string name) {Rebinding=name;rebindAfter=Time.unscaledTime+.2f;}
        public bool PollRebind()
        {
            if(Rebinding==null||Time.unscaledTime<rebindAfter||Keyboard.current==null)return false;
            foreach(var key in Keyboard.current.allKeys)if(key.wasPressedThisFrame)
            {
                Key selected=key.keyCode,old=Keys[Rebinding];
                // Swap conflicting actions, so movement/pause cannot silently become inaccessible.
                foreach(var n in new List<string>(Keys.Keys))if(n!=Rebinding&&Keys[n]==selected){Keys[n]=old;PlayerPrefs.SetInt("binding."+n,(int)old);}
                Keys[Rebinding]=selected;PlayerPrefs.SetInt("binding."+Rebinding,(int)selected);PlayerPrefs.Save();Rebinding=null;return true;
            }
            return false;
        }
    }
}
