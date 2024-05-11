namespace Advize_PlantEverything;

using HarmonyLib;

[HarmonyPatch(typeof(Pickable))]
static class ShowPickableSpawnerPatches
{
    [HarmonyPatch(nameof(Pickable.Awake))]
    static void Postfix(Pickable __instance) => TogglePickedMesh(__instance, __instance.m_picked);

    [HarmonyPatch(nameof(Pickable.SetPicked))]
    static void Postfix(Pickable __instance, bool picked) => TogglePickedMesh(__instance, picked);

    static void TogglePickedMesh(Pickable instance, bool picked) => instance.transform.root.Find("PE_Picked")?.gameObject.SetActive(picked);
}
