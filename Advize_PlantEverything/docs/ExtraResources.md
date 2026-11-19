# ExtraResources

PlantEverything can make **any prefab buildable with the Cultivator**. Some prefabs are not included by
default, so this page explains how to add your own.

- **Where the file lives:** `BepInEx/config/PlantEverything/ExtraResources.json`
- **How to enable it:** set `EnableExtraResources` to `true` in the mod's config
  (`BepInEx/config/PlantEverything/PlantEverything.cfg`).
- **Restart the game** (or reload the world) after editing the file.

> The file is created automatically with a commented-out example the first time the mod runs with
> `EnableExtraResources` enabled.

---

## File format

The file is **not** a JSON array. It is a list of **JSON objects separated by semicolons** (`;`).

```json
{ "prefabName": "Pickable_Stone", "resourceName": "Wood", "resourceCost": 5, "groundOnly": true, "pieceName": "Loose Stone", "pieceDescription": "A stone you can plant." };
{ "prefabName": "Pickable_Branch", "resourceName": "Wood", "resourceCost": 2, "groundOnly": true, "pieceName": "Branch", "pieceDescription": "A branch you can plant." }
```

Notes:

- A trailing `;` is fine. Blank entries are ignored.
- Field names are **case-sensitive** and must match exactly.
- Unknown fields are ignored, so a file written for a newer version still loads.
- The file is watched: it is re-read when it changes, but **the game must be restarted** for changes to
  take full effect (some settings only apply on a world reload).

---

## Fields

| Field | Type | Required | Meaning |
|---|---|---|---|
| `prefabName` | string | yes | The **existing** prefab to make buildable. Must be the exact prefab name, e.g. `Pickable_Stone`. |
| `resourceName` | string | yes | The item you **pay** to build it, e.g. `Wood`. Must be a valid item name. |
| `resourceCost` | int | yes | How many of `resourceName` to pay. **Must not be `0`.** |
| `groundOnly` | bool | no | `true` = can only be placed on the ground (default `false`). |
| `pieceName` | string | no | The name shown in the build menu. Defaults to `prefabName`. |
| `pieceDescription` | string | no | The description shown in the build menu. Defaults to empty. |

### `prefabName` — the prefab to build

This must be a **world prefab** — something that already exists in the game and can be picked up or
interacted with, such as `Pickable_Stone`, `Pickable_Branch`, `RaspberryBush`, or `Pickable_Mushroom`.

> **Do not use an item name here.** Items like `Resin` or `Wood` are shared item prefabs used by the whole
> game. Adding one of them here would attach a build component to the shared item, which makes every
> copy of that item in the world behave like a placed object — including loose ones lying on the ground.
> Use a `Pickable_*` prefab instead.

### `resourceName` / `resourceCost` — the build cost

These are what you **pay** to place the object, not what it drops. The dropped item comes from the
referenced prefab's own pickup behaviour.

> The build-menu **icon** is taken from `resourceName` (the cost item), not from the prefab. A spawner is
> the exception, its icon is the item it yields.

### `pieceName` / `pieceDescription` — display text

These are **plain text**, not localisation keys. To use a localised name, put the text itself in the
config (the build menu localises `$`-prefixed strings only, so a literal name is displayed as-is).

---

## What an extra resource does and does not change

- The prefab is made **buildable**: it gets a build component and is added to the Cultivator's menu.
- The prefab's **own** behaviour is left alone. Its pickup, respawn timer, drop amount and visuals stay
  exactly as the game defines them. (The mod's own built-in plants — raspberry bushes, mushrooms and so
  on — are the exception: their respawn time and drop amount are configurable in the mod config.)
- With **ResourcesSpawnEmpty** turned on, a prefab added through this file is placed empty and has to
  regrow before it can be harvested, just like the plants the mod adds itself.

---

## Spawners — defining a new pickable

Normally an entry can only *reference* a prefab that already exists, which means the object drops
whatever that prefab drops. A **spawner** entry goes further: the mod builds a **new prefab** for you,
so it can drop something different from the template it is based on.

Set `spawnerName` to switch an entry into spawner mode.

| Field | Type | Required | Meaning |
|---|---|---|---|
| `spawnerName` | string | yes | The **name of the new prefab** the mod creates. Must be unique. |
| `dropItem` | string | yes | The item this spawner yields when harvested, e.g. `Resin`. |
| `respawnTime` | float | no | Minutes between harvests. `0` (default) = inherit from the template. |
| `amount` | int | no | How many are yielded per harvest. `0` (default) = inherit from the template. |
| `fullVisual` | string | no | Which mesh to show while the spawner is ready to harvest. |
| `emptyVisual` | string | no | Which mesh to show while the spawner is empty. Omit for an invisible empty state. |

A spawner is placed like any other extra resource, so `resourceName`, `resourceCost`, `groundOnly`,
`pieceName` and `pieceDescription` work the same way. The **icon** in the build menu is the icon of the
item the spawner yields.

### How a spawner is put together

A spawner is **built from a template**, but every meaningful part is set explicitly rather than
inherited:

| Part | Where it comes from |
|---|---|
| Name (and therefore the world-save identity) | `spawnerName` |
| What it drops | `dropItem` |
| Respawn timer | `respawnTime` |
| Amount per harvest | `amount` |
| Mesh while ready | `fullVisual` |
| Mesh while empty | `emptyVisual` |
| What you pay to build it | `resourceName` / `resourceCost` |
| Menu name and description | `pieceName` / `pieceDescription` |
| Ground placement | `groundOnly` |
| Collider, physics, LOD, lights | inherited from the template prefab |

The template is the prefab named by `prefabName`. It contributes the *structure* of the object (its
collider, physics, LOD group and lights) and nothing else — you choose the item, the drop amount and both
meshes yourself.

The mod never modifies the template prefab itself. The template is copied, and the copy is what gets
placed, so pointing at a shared prefab is safe.

### Choosing the meshes

`fullVisual` and `emptyVisual` name an **asset** whose mesh is used for the spawner. Both accept:

- **A game prefab**, e.g. `Resin`. The mesh is taken from the prefab's `model` child.
- **An item**, e.g. `Resin`. Same thing — items are looked up in the item database.
- **One of the mod's own empty-state prefabs**, e.g. `Pickable_Mushroom_yellow_Picked` — the bare
  mushroom stem.

The mesh, the materials and the placement (position, rotation and scale) of the named asset are used, so
the spawner looks like the asset you picked. The collider, the LOD group and the lights are the ones of
the template.

The empty-state prefabs of the mod (`*_Picked`) carry no material of their own — they use the material of
the plant they were made from. The same is done for a spawner, so `Pickable_Mushroom_yellow_Picked`
shows up in the colour of the yellow mushroom.

A spawner with no `emptyVisual` is **invisible while empty**, which is how most vanilla pickables behave
(`Pickable_Stone`, `Pickable_Branch` and vanilla mushrooms all vanish when harvested and reappear when
they respawn).

### The empty state

The two states are toggled by the same mechanism the mod uses for its own plants:

- **Full** — the `fullVisual` mesh is shown and the spawner can be harvested.
- **Empty** — the `emptyVisual` mesh is shown (or nothing at all if omitted) and the spawner cannot be
  harvested until `respawnTime` has elapsed.

The empty state is always shown for a spawner that has an `emptyVisual`, even when
`ShowPickableSpawners` is turned off — that setting only affects the plants the mod adds itself.

### Spawning empty

A spawner follows the **ResourcesSpawnEmpty** setting of the mod: with it turned on, a spawner that is
placed starts out empty and has to regrow before it can be harvested, just like the plants the mod adds
itself.

A spawner that cannot regrow — one with no `respawnTime` and a template without a respawn timer — is
always placed full, because an empty spawner would never come back.

### The respawn timer

`respawnTime` is the number of **minutes** between two harvests. `0` means the timer of the template is
used.

The timer behaves like the timer of the game's own pickables, so a spawner comes back the same way a
vanilla mushroom or a berry bush does.

### Worked example — a Resin node

A node that looks like a mushroom, yields Resin, and shows a bare mushroom stem while it is regrowing:

```json
{
  "prefabName": "Pickable_Mushroom_yellow",
  "spawnerName": "PE_ResinNode",
  "dropItem": "Resin",
  "respawnTime": 240,
  "amount": 1,
  "fullVisual": "Resin",
  "emptyVisual": "Pickable_Mushroom_yellow_Picked",
  "resourceName": "Wood",
  "resourceCost": 10,
  "groundOnly": true,
  "pieceName": "Resin Node",
  "pieceDescription": "Yields resin as it regrows."
}
```

Reading it back:

- `prefabName` — the mushroom is only the **template**; the Resin node is a separate object.
- `spawnerName` — the new prefab's name, and its identity in the world save.
- `fullVisual: "Resin"` — the Resin lump is shown when it is ready.
- `emptyVisual: "Pickable_Mushroom_yellow_Picked"` — the mushroom stem is shown while it regrows.
- `resourceName: "Wood"`, `resourceCost: 10` — costs 10 Wood to place.
- `respawnTime: 240` — four hours between harvests.

The build menu shows the **Resin** as the icon of the node, because that is what it yields.

Drop `emptyVisual` and the node is simply invisible while empty, like a vanilla stone.

### Rules for spawner names

- The name is the prefab's **identity**: it is hashed into the world save, so it must be **stable**.
  Renaming it invalidates spawners already placed in the world.
- It must be **unique**. Two spawners sharing a name collide, and it must not be the name of an existing
  prefab or item.
- It must not contain a **space** or `(`. The game truncates prefab names at the first of those when
  computing the hash, which would silently break persistence.
- It must differ from the name of the template prefab.
- Keep a `PE_` prefix so it is obvious the prefab is mod-created.

---

## Multiplayer

The contents of the file are synced to clients, so everyone builds the same prefabs from the same
config and the spawner names match on both sides.

The **server's** copy of the file is authoritative. A client's local file is only read if the client is
the host.

A client builds the spawners when the config reaches it, which is after the world has been loaded. The
spawners that were placed in the world show up as soon as the config arrives.

Spawners that were already placed keep working after the file changes, as long as `spawnerName` is
unchanged.

---

## Troubleshooting

**The build menu does not show my entry.**
Check that `EnableExtraResources` is `true`, that the file is in
`BepInEx/config/PlantEverything/`, and that the fields are spelled exactly as in the table above.
`resourceCost` must not be `0`.

**"Invalid resource" in the console.**
One field is missing or invalid. The message names the offending `prefabName`.

**My entry disappears on restart.**
An entry was rejected. Check the console for a warning naming the entry.

**The spawner shows the wrong mesh.**
`fullVisual` and `emptyVisual` must name an asset that actually has a mesh. A name that does not
resolve is reported in the console and the spawner is not created at all.

**The spawner does not come back.**
`respawnTime` has to be larger than `0`. `0` means the timer of the template is used, and a template
without a respawn timer never regrows.

**My placed spawner vanished after a world reload.**
The `spawnerName` changed between sessions. Names are part of the world save, so renaming a spawner
invalidates the ones already placed.
