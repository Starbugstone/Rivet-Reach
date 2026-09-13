namespace RivetReach
{
    public sealed partial class WorldSurvival
    {
        public bool AccelerateCrop(BlockPos p)
        {
            var world=game.World;byte id=world.Get(p);var crop=CropRules.For(id);
            if(crop==null||id>=crop.Mature||!world.Ready(p)||!world.Ready(p.Offset(0,-1,0))||world.GrowthLight(p)<9||!world.Grow(p,id))return false;
            // Replace the old deadline; do not give the next stage an almost-due job.
            if(id+1<crop.Mature)Schedule(p,checked(Tick+crop.stageTicks));
            return true;
        }
    }
    public sealed partial class Expedition
    {
        public bool TryUseCompost()
        {
            if(!Started||Health.Dead||Mode!=ScreenMode.Play||Inventory.Slots[Selected].Id!=CompostId.Compost)return false;
            if(!World.Raycast(Player.Camera.transform.position,Player.Camera.transform.forward,5,out var p,out _,out _)||!Survival.AccelerateCrop(p))return false;
            if(!Creative)Inventory.Take(Selected,1);
            return true;
        }
    }
}
