using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Advize_PlantEverything
{
    public partial class PlantEverything
    {
        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public static class ObjectDBAwake
        {
            public static void Postfix(ObjectDB __instance)
            {
                Dbgl("ObjectDBAwake");

                if (ZNetScene.instance == null) return;
                Dbgl("& ZNetScene not null");
                InitItems(__instance);
            }
        }

        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        public static class ObjectDBCopyOtherDB
        {
            public static void Postfix(ObjectDB other)
            {
                Dbgl("ObjectDBCopyOtherDB");
                InitPrefabRefs();
                InitItems(other);
            }
        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetSceneAwake
        {
            public static void Postfix(ZNetScene __instance)
            {
                Dbgl("ZNetSceneAwake");
                Dbgl("Performing final mod initialization");
                FinalInit(__instance);
            }
        }

        [HarmonyPatch(typeof(ObjectDB), "GetItemPrefab", new Type[] { typeof(int) })]
        public static class ObjectDBGetItemPrefab
        {
            public static void Postfix(int hash, ref GameObject __result, ObjectDB __instance)
            {
                if (__result == null)
                {
                    List<GameObject> prefabs = new List<GameObject>
                    {
                        prefabRefs["BirchCone"],
                        prefabRefs["OakSeeds"],
                        prefabRefs["AncientSeeds"]
                    };

                    foreach (GameObject prefab in prefabs)
                    {
                        if (hash == __instance.GetPrefabHash(prefab))
                        {
                            __result = prefab;
                            break;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ZNetScene), "GetPrefab", new Type[] { typeof(int) })]
        public static class ZNetSceneGetPrefab
        {
            public static void Postfix(int hash, ref GameObject __result, ZNetScene __instance)
            {
                if (__result == null)
                {
                    List<GameObject> prefabs = new List<GameObject>
                    {
                        prefabRefs["BirchCone"],
                        prefabRefs["OakSeeds"],
                        prefabRefs["AncientSeeds"],
                        prefabRefs["Birch_Sapling"],
                        prefabRefs["Oak_Sapling"],
                        prefabRefs["Ancient_Sapling"]
                    };

                    foreach (GameObject prefab in prefabs)
                    {
                        if (hash == __instance.GetPrefabHash(prefab))
                        {
                            __result = prefab;
                            break;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), "RemovePiece")]
        public static class PlayerRemovePiece
        {
            public static bool Prefix(Player __instance, ZSyncAnimation ___m_zanim, ref bool __result)
            {
                ItemDrop.ItemData rightItem = __instance.GetRightItem();
                if (rightItem.m_shared.m_name == "$item_cultivator")
                {
                    if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out var hitInfo, 50f, LayerMask.GetMask("item", "piece_nonsolid", "terrain")) && Vector3.Distance(hitInfo.point, __instance.m_eye.position) < __instance.m_maxPlaceDistance)
                    {
                        Pickable pickable = hitInfo.collider.GetComponentInParent<Pickable>();
                        if (pickable == null) return true;

                        ZNetView component = pickable.GetComponent<ZNetView>();
                        if (component == null) return true;

                        component.ClaimOwnership();
                        __instance.m_removeEffects.Create(pickable.transform.position, Quaternion.identity);
                        ZNetScene.instance.Destroy(pickable.gameObject);
                        __instance.FaceLookDirection();
                        ___m_zanim.SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
                        __result = true;
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Pickable), "SetPicked")]
        public static class PickableSetPicked
        {
            public class PickState
            {
                public long picked_time;
                public bool picked;
            }

            public static void Prefix(ZNetView ___m_nview, bool ___m_picked, ref PickState __state)
            {
                __state = new PickState
                {
                    picked_time = ___m_nview.GetZDO().GetLong("picked_time", 0L),
                    picked = ___m_picked
                };
            }

            public static void Postfix(ZNetView ___m_nview, bool ___m_picked, ref PickState __state)
            {
                if (__state != null && __state.picked == ___m_picked && ___m_nview.IsOwner())
                {
                    ___m_nview.GetZDO().Set("picked_time", __state.picked_time);
                }
            }
        }

        [HarmonyPatch(typeof(Plant), "Awake")]
        public static class PlantAwake
        {
            public static void Postfix(Plant __instance)
            {
                if (!config.EnforceBiomesVanilla)
                {
                    __instance.m_biome = (Heightmap.Biome)895;
                }
            }
        }

        [HarmonyPatch(typeof(Localization), "SetupLanguage")]
        public static class LocalizationSetupLanguage
        {
            private static Dictionary<string, string> m_translations;
            public static void Postfix(ref Dictionary<string, string> ___m_translations)
            {
                m_translations = ___m_translations;
                foreach (KeyValuePair<string, string> kvp in stringDictionary)
                {
                    AddWord(kvp.Key, kvp.Value);
                }

                stringDictionary.Clear();
            }

            private static void AddWord(string key, string value)
            {
                m_translations.Remove($"pe{key}");
                m_translations.Add($"pe{key}", value);
            }
        }
    }
}
