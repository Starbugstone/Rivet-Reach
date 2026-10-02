using UnityEngine;

namespace RivetReach
{
    // Presentation distances only. Never requests residency or advances simulation.
    public static class FactoryVisibility
    {
        public const float FadeStart=48,FadeEnd=64,DetailRange=72;
        // Explicit verification control, reset by its scenario; never a saved/player option.
        internal static bool LegacyReview,FullDetailReview;
        static float outer=304;
        public static int DetailColumns=>FullDetailReview?Mathf.CeilToInt(outer/32)+1:3;
        public static float DetailSquared=>FullDetailReview?outer*outer:(LegacyReview?64:DetailRange)*(LegacyReview?64:DetailRange);
        public static float DistanceSquared(Vector3 eye,Bounds bounds)=>bounds.SqrDistance(eye);
        public static bool Visible(Vector3 eye,Bounds bounds,float fogEnd)=>DistanceSquared(eye,bounds)<fogEnd*fogEnd;
        public static Matrix4x4 Placement(Vector3 cell,int rotation)
        {
            var q=Quaternion.Euler(0,rotation*90,0);return Matrix4x4.TRS(cell+Vector3.one*.5f-q*(Vector3.one*.5f),q,Vector3.one);
        }
        public static void Globals(Vector3 eye,float fogEnd)
        {
            outer=fogEnd;Shader.SetGlobalVector("_RRPresentationEye",eye);
            Shader.SetGlobalVector("_RRPresentationFade",LegacyReview||FullDetailReview?new Vector4(1000000,1000001,0,0):new Vector4(FadeStart,FadeEnd,0,0));
        }
    }
}
