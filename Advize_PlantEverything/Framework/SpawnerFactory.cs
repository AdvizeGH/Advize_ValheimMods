namespace Advize_PlantEverything;

using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;
using static StaticMembers;

/// <summary>
/// Builds the prefabs described by spawner entries in ExtraResources.json.
///
/// A spawner is a copy of a vanilla prefab - the template - with its own drop,
/// respawn timer and visuals. The template itself is never modified, so shared
/// prefabs such as the item prefabs in ObjectDB stay untouched.
///
/// The copy is registered with the ZNetScene so that it can be placed in the
/// world and, more importantly, so that placed copies can be resolved by their
/// prefab hash when the world is loaded again.
/// </summary>
internal static class SpawnerFactory
{
    // the name ShowPickableSpawnerPatches looks for when toggling the empty visual
    private const string PickedChildName = "PE_Picked";

    private const string PickedSuffix = "_Picked";

    private static readonly Dictionary<string, GameObject> spawners = [];
    private static readonly Dictionary<string, string> signatures = [];
    private static GameObject holder;

    /// <summary>
    /// The game hashes prefab names up to the first space or '(', so a name
    /// containing either of them would not survive a round trip through a
    /// world save.
    /// </summary>
    internal static bool IsValidName(string name) => !string.IsNullOrWhiteSpace(name) && !name.Contains(' ') && !name.Contains('(');

    /// <summary>
    /// Creates the spawner described by <paramref name="er"/> if it does not
    /// exist yet, and makes sure it is registered with <paramref name="instance"/>.
    /// </summary>
    internal static bool Ensure(ExtraResource er, ZNetScene instance, bool logErrors = false)
    {
        string signature = BuildSignature(er);

        if (spawners.TryGetValue(er.spawnerName, out GameObject existing) && existing &&
            signatures.TryGetValue(er.spawnerName, out string existingSignature) && existingSignature == signature)
        {
            prefabRefs[er.spawnerName] = existing;
            Register(existing, instance);
            return false;
        }

        GameObject prefab = Create(er, instance, out string error);

        if (!prefab)
        {
            Dbgl($"Could not create spawner {er.spawnerName}: {error}", logErrors || config.EnableDebugMessages, LogLevel.Warning);
            return false;
        }

        Replace(er.spawnerName, prefab, instance);
        prefabRefs[er.spawnerName] = prefab;
        signatures[er.spawnerName] = signature;
        Register(prefab, instance);

        Dbgl($"Created spawner {er.spawnerName}", logErrors || config.EnableDebugMessages);

        return true;
    }

    /// <summary>
    /// Removes spawners that are no longer configured.
    /// </summary>
    internal static void Prune(ZNetScene instance, List<ExtraResource> active)
    {
        List<string> stale = spawners.Keys.Where(name => !active.Any(er => er.IsSpawner && er.spawnerName == name)).ToList();

        foreach (string name in stale)
        {
            GameObject prefab = spawners[name];

            spawners.Remove(name);
            signatures.Remove(name);

            if (prefab)
            {
                Unregister(prefab, instance);

                if (prefabRefs.TryGetValue(name, out GameObject current) && current == prefab)
                {
                    prefabRefs.Remove(name);
                }

                UnityEngine.Object.Destroy(prefab);
            }
        }
    }

    private static GameObject Create(ExtraResource er, ZNetScene instance, out string error)
    {
        error = null;

        GameObject template = ResolvePrefab(er.prefabName, instance);

        if (!template)
        {
            error = $"the template prefab {er.prefabName} could not be found";
            return null;
        }

        if (!template.GetComponent<Pickable>())
        {
            error = $"the template prefab {er.prefabName} has no Pickable component";
            return null;
        }

        if (template.GetComponent<ItemDrop>())
        {
            error = $"the template prefab {er.prefabName} is an item, items cannot be used as a template";
            return null;
        }

        if (er.spawnerName == er.prefabName)
        {
            error = "the spawner name must differ from the template prefab name";
            return null;
        }

        GameObject previous = spawners.TryGetValue(er.spawnerName, out GameObject old) ? old : null;

        if (prefabRefs.TryGetValue(er.spawnerName, out GameObject named) && named && named != previous)
        {
            error = $"a prefab named {er.spawnerName} already exists";
            return null;
        }

        if (GetItemPrefab(er.spawnerName))
        {
            error = $"the name {er.spawnerName} is already used by an item";
            return null;
        }

        GameObject dropPrefab = GetItemPrefab(er.dropItem);

        if (!dropPrefab)
        {
            error = $"the drop item {er.dropItem} could not be found";
            return null;
        }

        if (!dropPrefab.GetComponent<ItemDrop>())
        {
            error = $"{er.dropItem} is not an item";
            return null;
        }

        Transform fullSource = null;

        if (!string.IsNullOrEmpty(er.fullVisual))
        {
            GameObject fullVisual = ResolveVisual(er.fullVisual);

            if (!fullVisual)
            {
                error = $"the full visual {er.fullVisual} could not be found";
                return null;
            }

            fullSource = FindMeshSource(fullVisual);

            if (!fullSource)
            {
                error = $"the full visual {er.fullVisual} does not contain a mesh";
                return null;
            }
        }

        Transform emptySource = null;

        if (!string.IsNullOrEmpty(er.emptyVisual))
        {
            GameObject emptyVisual = ResolveVisual(er.emptyVisual);

            if (!emptyVisual)
            {
                error = $"the empty visual {er.emptyVisual} could not be found";
                return null;
            }

            emptySource = FindMeshSource(emptyVisual);

            if (!emptySource)
            {
                error = $"the empty visual {er.emptyVisual} does not contain a mesh";
                return null;
            }
        }

        GameObject clone = Clone(template, er.spawnerName);
        Pickable pickable = clone.GetComponent<Pickable>();

        pickable.m_itemPrefab = dropPrefab;
        // a spawner yields what it is configured to yield, nothing else
        pickable.m_extraDrops = new DropTable();
        // the hover text names the item the spawner yields
        pickable.m_overrideName = "";

        if (er.amount > 0)
        {
            pickable.m_amount = er.amount;
        }

        if (er.respawnTime > 0f)
        {
            pickable.m_respawnTimeMinutes = er.respawnTime;
        }

        if (pickable.m_respawnTimeMinutes <= 0f)
        {
            Dbgl($"Spawner {er.spawnerName} has no respawn time, it will not come back after being harvested", forceLog: true, level: LogLevel.Warning);
        }

        Transform fullHolder = FindFullVisual(pickable, clone);

        if (fullSource && fullHolder)
        {
            Material[] templateMaterials = fullHolder.GetComponent<MeshRenderer>()?.sharedMaterials;

            ApplyVisual(fullSource, fullHolder, templateMaterials);
        }

        if (fullHolder)
        {
            // hides the full visual while the spawner is picked
            pickable.m_hideWhenPicked = fullHolder.gameObject;
        }

        if (emptySource)
        {
            GameObject picked = UnityEngine.Object.Instantiate(emptySource.gameObject, clone.transform, false);

            picked.name = PickedChildName;
            picked.SetActive(false);

            MeshRenderer pickedRenderer = picked.GetComponent<MeshRenderer>();

            if (pickedRenderer && !HasMaterials(pickedRenderer.sharedMaterials))
            {
                Material[] fallback = ResolveFallbackMaterials(er.emptyVisual, fullHolder);

                if (fallback != null && fallback.Length > 0)
                {
                    pickedRenderer.sharedMaterials = fallback;
                }
            }
        }

        ZNetView nview = clone.GetComponent<ZNetView>();

        if (nview)
        {
            // placed spawners have to survive a restart
            nview.m_persistent = true;
        }

        return clone;
    }

    /// <summary>
    /// Copies the mesh, the materials and the placement of
    /// <paramref name="source"/> onto <paramref name="target"/>.
    /// </summary>
    private static void ApplyVisual(Transform source, Transform target, Material[] fallback)
    {
        MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
        MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
        MeshFilter targetFilter = target.GetComponent<MeshFilter>();
        MeshRenderer targetRenderer = target.GetComponent<MeshRenderer>();

        if (!sourceFilter || !sourceRenderer || !targetFilter || !targetRenderer) return;

        targetFilter.sharedMesh = sourceFilter.sharedMesh;

        Material[] materials = sourceRenderer.sharedMaterials;

        if (!HasMaterials(materials)) materials = fallback;

        if (materials != null && materials.Length > 0)
        {
            targetRenderer.sharedMaterials = materials;
        }

        ApplyTransform(source, target);
    }

    /// <summary>
    /// Moves the target onto the source, without moving the children of the
    /// target - a template usually has its light attached to its visual.
    /// </summary>
    private static void ApplyTransform(Transform source, Transform target)
    {
        List<(Transform child, Vector3 position, Quaternion rotation)> children = [];

        foreach (Transform child in target)
        {
            children.Add((child, child.position, child.rotation));
        }

        target.localPosition = source.localPosition;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;

        foreach ((Transform child, Vector3 position, Quaternion rotation) in children)
        {
            child.position = position;
            child.rotation = rotation;
        }
    }

    /// <summary>
    /// The materials of a visual that does not bring its own. The prefabs of
    /// the mod that show a picked plant have no material, they use the material
    /// of the plant they were made from.
    /// </summary>
    private static Material[] ResolveFallbackMaterials(string visualName, Transform fullHolder)
    {
        if (!string.IsNullOrEmpty(visualName) && visualName.EndsWith(PickedSuffix, StringComparison.Ordinal))
        {
            string baseName = visualName.Substring(0, visualName.Length - PickedSuffix.Length);

            if (prefabRefs.TryGetValue(baseName, out GameObject basePrefab) && basePrefab)
            {
                Transform visual = basePrefab.transform.Find("visual");

                if (!visual)
                {
                    MeshFilter filter = basePrefab.GetComponentInChildren<MeshFilter>(true);

                    visual = filter ? filter.transform : null;
                }

                MeshRenderer renderer = visual ? visual.GetComponent<MeshRenderer>() : null;

                if (renderer && HasMaterials(renderer.sharedMaterials)) return renderer.sharedMaterials;
            }
        }

        MeshRenderer fullRenderer = fullHolder ? fullHolder.GetComponent<MeshRenderer>() : null;

        return fullRenderer ? fullRenderer.sharedMaterials : null;
    }

    private static bool HasMaterials(Material[] materials) => materials != null && materials.Length > 0 && materials.All(m => m != null);

    /// <summary>
    /// The object of the clone that shows the spawner while it is ready to be
    /// harvested. Templates usually hide that same object when they are picked,
    /// but the visual is looked up explicitly so that any template works.
    /// </summary>
    private static Transform FindFullVisual(Pickable pickable, GameObject clone)
    {
        if (pickable.m_hideWhenPicked)
        {
            Transform hidden = pickable.m_hideWhenPicked.transform;

            if (hidden.GetComponent<MeshFilter>()) return hidden;
        }

        MeshFilter filter = clone.GetComponentInChildren<MeshFilter>(true);

        return filter ? filter.transform : null;
    }

    /// <summary>
    /// The object of a visual source that carries the mesh.
    /// </summary>
    private static Transform FindMeshSource(GameObject source)
    {
        if (!source) return null;

        Transform picked = source.transform.Find(PickedChildName);

        if (picked && picked.GetComponent<MeshFilter>()) return picked;

        MeshFilter filter = source.GetComponentInChildren<MeshFilter>(true);

        if (!filter)
        {
            Dbgl($"The visual source {source.name} does not contain a mesh", forceLog: true, level: LogLevel.Warning);
        }

        return filter ? filter.transform : null;
    }

    /// <summary>
    /// Looks an item up in the item database. The database is recreated when a
    /// world is loaded, the old one is destroyed by then but still knows the
    /// items, so the null check must not use the destroyed check of Unity.
    /// </summary>
    private static GameObject GetItemPrefab(string name) => ObjectDB.instance?.GetItemPrefab(name);

    /// <summary>
    /// Resolves a visual name to a prefab. Game prefabs and the prefabs of the
    /// mod are looked up first, items are looked up in the ObjectDB.
    /// </summary>
    private static GameObject ResolveVisual(string name)
    {
        if (prefabRefs.TryGetValue(name, out GameObject prefab) && prefab) return prefab;

        GameObject item = GetItemPrefab(name);

        if (item) return item;

        return null;
    }

    private static GameObject ResolvePrefab(string name, ZNetScene instance)
    {
        if (prefabRefs.TryGetValue(name, out GameObject prefab) && prefab) return prefab;

        if (instance)
        {
            GameObject scenePrefab = instance.GetPrefab(name);

            if (scenePrefab) return scenePrefab;
        }

        return null;
    }

    /// <summary>
    /// Copies the template without running any of its Awake methods, so that
    /// the copy does not create a ZDO of its own. The copy is parented to an
    /// inactive object, which is what keeps it from waking up. The template
    /// itself is never touched, it can be a prefab the whole game shares.
    /// </summary>
    private static GameObject Clone(GameObject template, string name)
    {
        GameObject clone = UnityEngine.Object.Instantiate(template, Holder().transform);

        clone.name = name;
        clone.SetActive(true);

        return clone;
    }

    /// <summary>
    /// The inactive object the spawner prefabs are parented to. It is kept
    /// across scene loads so that the prefabs survive a world reload.
    /// </summary>
    private static GameObject Holder()
    {
        if (holder) return holder;

        holder = new("PE_SpawnerHolder");
        UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.SetActive(false);

        return holder;
    }

    private static void Register(GameObject prefab, ZNetScene instance)
    {
        if (!instance || !prefab) return;

        int hash = prefab.name.GetStableHashCode();

        if (!instance.m_prefabs.Contains(prefab))
        {
            instance.m_prefabs.Add(prefab);
        }

        instance.m_namedPrefabs[hash] = prefab;
    }

    private static void Unregister(GameObject prefab, ZNetScene instance)
    {
        if (!instance || !prefab) return;

        instance.m_prefabs.Remove(prefab);
        instance.m_namedPrefabs.Remove(prefab.name.GetStableHashCode());
    }

    private static void Replace(string name, GameObject prefab, ZNetScene instance)
    {
        if (spawners.TryGetValue(name, out GameObject previous) && previous)
        {
            Unregister(previous, instance);

            UnityEngine.Object.Destroy(previous);
        }

        spawners[name] = prefab;
    }

    private static string BuildSignature(ExtraResource er) => $"{er.prefabName}|{er.dropItem}|{er.respawnTime}|{er.amount}|{er.fullVisual}|{er.emptyVisual}";
}
