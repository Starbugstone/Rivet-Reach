using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        // Explicit capture mode: real placed stations and their production UI, in a disposable session.
        IEnumerator CaptureMachineInterfaces()
        {
            report.workload = "Machine illustration capture, station switching and retained inventory controls";
            game.SetCreative(true); game.Diagnostics = false; game.Mobs.NaturalSpawning = false;
            game.Player.enabled = false;
            var world = game.World;
            var origin = world.Address(game.Player.transform.position).Offset(0, 2, 3);
            for (int x = -3; x <= 4; x++) for (int z = -4; z <= 3; z++) for (int y = -1; y <= 4; y++)
            {
                var p = origin.Offset(x, y, z); byte old = world.Get(p);
                if (old != 0) world.Remove(p, old);
                if (y == -1) world.Place(p, BlockId.Stone);
            }
            game.Player.transform.position = world.Local(origin) + new Vector3(.5f, 0, -2.5f);
            game.Player.Camera.transform.position = game.Player.transform.position + Vector3.up * 1.64f;
            game.Player.Camera.transform.LookAt(world.Local(origin) + Vector3.one * .5f);
            game.Inventory.Add(BlockId.RawIron, 16); game.Inventory.Add(BlockId.Charcoal, 8);
            game.Sky.Clock.SetTime(.4); game.Sky.Apply();
            var ids = new byte[] { IndustryId.Crusher, IndustryId.Boiler, IndustryId.Pump, IndustryId.Battery,
                IndustryId.BatteryController, IndustryId.TankController, IndustryId.ItemPipe,
                BlockId.Furnace, BlockId.Chest, BlockId.Workbench, IndustryId.Bench, IndustryId.Crusher };
            RawImage retainedArt = null;
            foreach (byte id in ids)
            {
                game.SetMode(ScreenMode.Play);
                byte old = world.Get(origin); if (old != 0) world.Remove(origin, old);
                Check(world.Place(origin, id), "Place illustration subject " + id);
                yield return new WaitForSecondsRealtime(.15f);
                Check(BlockId.Station(id) ? game.TryOpenStation(origin) : game.TryOpenMachine(origin), "Open placed subject " + id);
                yield return null;
                var art = game.UI.VisibleRoot.GetComponentsInChildren<RawImage>().Single(v => v.name == "Machine graphic");
                Check(art.texture == game.UI.ItemIcon(id), "Illustration matches opened subject " + id);
                if (retainedArt != null) Check(art == retainedArt, "Switch subject using retained illustration " + id);
                retainedArt = art;
                Check(!game.UI.VisibleRoot.GetComponentsInChildren<RawImage>().Any(v => v.name == "Portrait"), "Station replaces character portrait " + id);
                if (id == IndustryId.Crusher)
                {
                    var machine = game.OpenMachine; int rotation = machine.Rotation;
                    game.UI.VisibleRoot.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<Text>().text == "ROTATE PORTS 90°").onClick.Invoke();
                    Check(machine.Rotation == (rotation + 1) % 4, "Retained rotate control addresses current crusher");
                    // Transfer through the actual slot pointer path, including the replacement crusher.
                    int source = Enumerable.Range(0, game.Inventory.Count).First(i => game.Inventory.Slots[i].Id == BlockId.RawIron);
                    yield return ClickCraftUI(source, false, true);
                    Check(machine.Items.Total(BlockId.RawIron) > 0, "Input transfer remains available beside illustration");
                }
                if (id == IndustryId.Battery) game.OpenMachine.EnergyCells[0].Charge(25000000);
                yield return new WaitForSecondsRealtime(.15f);
                yield return Capture("machine-interface-" + id);
                if (id == IndustryId.Crusher) yield return ClickCraftUI(300, false, true);
                // Respect the real charged-cell dismantling gate before replacing this fixture.
                if (id == IndustryId.Battery) game.OpenMachine.EnergyCells[0].Discharge(game.OpenMachine.EnergyCells[0].Amount);
            }
            game.SetMode(ScreenMode.Play); game.SetMode(ScreenMode.Inventory); yield return null;
            Check(game.UI.VisibleRoot.GetComponentsInChildren<RawImage>().Any(v => v.name == "Portrait") &&
                !game.UI.VisibleRoot.GetComponentsInChildren<RawImage>().Any(v => v.name == "Machine graphic"), "Personal inventory restores character portrait");
            yield return Capture("machine-interface-personal");
        }
    }
}
