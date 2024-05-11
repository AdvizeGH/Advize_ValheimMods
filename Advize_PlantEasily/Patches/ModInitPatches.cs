namespace Advize_PlantEasily;

using HarmonyLib;
using static PlantEasily;

[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
static class ModInitPatches
{
    static void Postfix() => InitPrefabRefs();
}
