using UnityEngine;

namespace RivetReach
{
    public static class PlacementFacing
    {
        // Authored fronts point along local -Z. Use positions so aiming at an
        // off-centre edge still faces the player; pitch never tips a machine.
        public static int TowardsPlayer(Vector3 centre,Vector3 player,float fallbackYaw)
        {
            var toward=player-centre;
            float yaw=toward.x*toward.x+toward.z*toward.z<.000001f
                ?fallbackYaw:Mathf.Atan2(-toward.x,-toward.z)*Mathf.Rad2Deg;
            return (Mathf.RoundToInt(yaw/90)%4+4)%4;
        }
    }
}
