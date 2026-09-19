# Item-pipe routing priorities

Item Pipes choose where compatible cargo goes using each receiver's saved numeric priority. This lets you reserve preferred inputs without changing pipe directions or creating a separate pipe type.

## Set a priority

Open a compatible receiver with **Use/right-click** or **Interact**. Its routing controls show the current priority from **0** through **100**. Use **−** and **+** for small changes, or enter an exact whole number in the priority field. The value is saved with that receiver. **ITEM PRIORITY** is separate from a powered machine’s **POWER** setting.

![Editable item priority below an Electric Furnace’s separate power controls](images/crates/electric-machine-item-priority.png)

New receivers start with these working defaults:

| Receiver | Default priority |
|---|---:|
| Machine or Furnace input | 50 |
| Crate Controller | 40 |
| Bulk Crate | 30 |
| Chest | 20 |

A higher number is considered first. Set equal numbers when two destinations should take turns. Priority changes do not move items already stored.

## How routing chooses a destination

1. The pipe considers only connected, loaded receivers that can accept the item.
2. It tries the highest priority that has an eligible receiver.
3. Receivers tied at that priority take turns round-robin for whole items.
4. If all receivers at that priority reject the item, the pipe tries the next lower eligible priority.
5. If no receiver can take it, the item stays in the source inventory.

A machine still accepts only its useful recipe inputs and face-specific fuel. An incompatible item does not win merely because that receiver has a high number. Full inventories, a Bulk Crate assigned or locked to another type, and unavailable terrain reject a delivery without deleting or moving the source item. Filled portable batteries/tanks can travel between chests with their contents intact; crates and processing machines reject these payloads.

A [Crate Controller](Crates-and-warehouses.md) chooses matching assigned crates before empty unlocked ones after it has been selected as the receiver. Direct access and controller access to the same crate do not double its output.

![Chest set to the same item priority as a Furnace](images/crates/chest-item-priority.png)

## Examples

Set a Furnace to **60**, a Crate Controller to **40**, and a Chest to **20**. Iron ore goes to the Furnace while it can accept the ore; when it cannot, the controller is considered before the Chest.

Set two Bulk Crates to **30** and connect both as inputs. Compatible items alternate between them while both can accept the item. If one fills, the other receives the remaining turns.

See [Pipes](Pipes.md) for wrench direction controls and [Crates and warehouses](Crates-and-warehouses.md) for physical storage setup.

![A real pipe run with a source chest, Furnace, warehouse controller, individual crate and overflow chest](images/crates/item-priority-network.png)

Red arrows extract; blue arrows insert. Arrows appear while holding the Wrench. These native captures were taken on 19 September 2026 on a constructed review platform.

## Transfer timing

A physical source inventory sends at most one item every five simulation ticks (four transfers per second). A crate reached directly and through a controller still counts as one source. A long pipe run does not delay an item block by block. Routing remembers connected endpoints until topology changes; it checks current compatibility and capacity before committing a transfer. A rejected destination is retried next transfer step, so many sources do not repeatedly probe the same full inventory in one step. No acceptable destination means the source keeps its item.
