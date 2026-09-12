using UnityEngine;

namespace RivetReach
{
    public sealed partial class FirstPersonPlayer
    {
        float eatingWeight,eatingBiteTime;
        byte eatingVisualItem;
        Transform eatingUpper,eatingFore,eatingHand;
        Quaternion eatingUpperRotation,eatingForeRotation,eatingHandRotation;
        public float EatingPoseWeight=>eatingWeight;
        EatingCrumbs eatingCrumbs;
        public int EatingCrumbCount=>eatingCrumbs!=null?eatingCrumbs.Particles.particleCount:0;
        public int EatingCrumbsEmitted=>eatingCrumbs!=null?eatingCrumbs.Emitted:0;

        // Restore before the next clip evaluation, including frames where the world
        // is paused or waiting for terrain. The offset must never accumulate.
        void RestoreEatingPose()
        {
            if(eatingUpper!=null)eatingUpper.localRotation=eatingUpperRotation;
            if(eatingFore!=null)eatingFore.localRotation=eatingForeRotation;
            if(eatingHand!=null)eatingHand.localRotation=eatingHandRotation;
            eatingUpper=eatingFore=eatingHand=null;
        }

        void AnimateEating()
        {
            var selected=Game.Inventory.Slots[Game.Selected];
            bool visible=Game.Mode==ScreenMode.Play&&!Game.Health.Dead&&!Game.Creative&&!Inspecting&&
                Arms.AnimationReady&&!selected.Empty&&HeldBlock.ItemId==selected.Id&&
                HeldBlock.DesiredGrip==GripPose.Block&&Game.Registry.Get(selected.Id).foodPoints>0;
            if(!visible||eatingVisualItem!=selected.Id)
            {eatingWeight=0;eatingBiteTime=0;eatingVisualItem=selected.Id;}
            if(!visible){eatingCrumbs?.ResetBite();return;}
            bool active=eating>0;
            if(active)eatingBiteTime=eating;
            eatingWeight=Mathf.MoveTowards(eatingWeight,active?1:0,Time.deltaTime/(active?.20f:.16f));
            if(eatingWeight<=0){eatingCrumbs?.ResetBite();return;}
            float weight=Mathf.SmoothStep(0,1,eatingWeight);
            float bite=Mathf.Sin(Mathf.Max(0,eatingBiteTime-.20f)*Mathf.PI*10)*.012f;
            float fovScale=Arms.transform.localScale.x;
            // Camera-relative mouth framing shares the existing first-person FOV
            // compensation. Food and fingers keep their authored socket contact.
            Vector3 mouth=Camera.transform.TransformPoint(new Vector3(.035f*fovScale,(-.055f+bite)*fovScale,.32f+Mathf.Abs(bite)*.5f));
            var upper=Arms.Bone("UpperArmR");var fore=Arms.Bone("ForearmR");var hand=Arms.Bone("HandR");
            var socket=Arms.Bone("BlockSocket");var frame=Arms.transform;
            eatingUpper=upper;eatingFore=fore;eatingHand=hand;
            eatingUpperRotation=upper.localRotation;eatingForeRotation=fore.localRotation;eatingHandRotation=hand.localRotation;
            Vector3 Point(Transform bone)=>frame.InverseTransformPoint(bone.position);
            Vector3 root=Point(upper),elbow=Point(fore),wrist=Point(hand);
            // Solve in the rig's unscaled frame so changing FOV cannot stretch bones.
            var target=wrist+(frame.InverseTransformPoint(mouth)-Point(socket))*weight;
            float a=Vector3.Distance(root,elbow),b=Vector3.Distance(elbow,wrist);
            var direction=(target-root).normalized;
            float distance=Mathf.Clamp(Vector3.Distance(root,target),Mathf.Abs(a-b)+.0001f,a+b-.0001f);
            target=root+direction*distance;
            var bend=Vector3.ProjectOnPlane(new Vector3(1,-.7f,0),direction).normalized;
            float along=(a*a-b*b+distance*distance)/(2*distance);
            var targetElbow=root+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            var handRotation=hand.rotation;
            void Aim(Transform bone,Transform child,Vector3 goal)
            {
                var turn=Quaternion.FromToRotation(Point(child)-Point(bone),goal-Point(bone));
                bone.rotation=frame.rotation*turn*Quaternion.Inverse(frame.rotation)*bone.rotation;
            }
            Aim(upper,fore,Vector3.Lerp(elbow,targetElbow,weight));Aim(fore,hand,target);
            hand.rotation=handRotation;
            if(active&&eatingWeight>.85f&&(ArcadePresentation.Active==null||ArcadePresentation.Active.Intensity>0))
            {
                if(eatingCrumbs==null){eatingCrumbs=gameObject.AddComponent<EatingCrumbs>();eatingCrumbs.Initialize(this);}
                eatingCrumbs.Bite(eatingItem,eating,socket.position);
            }
            else eatingCrumbs?.ResetBite();
        }
    }
}
