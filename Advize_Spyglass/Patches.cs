using HarmonyLib;
using UnityEngine;

namespace Advize_Spyglass
{
    public partial class Spyglass
    {
        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public static class ObjectDBAwake
        {
            public static void Prefix(ObjectDB __instance)
            {
                //Dbgl("ObjectDB.Awake() Prefix");
                PrefabInit();
                if (!ZNetScene.instance) return;
                
                if (!__instance.m_items.Contains(prefab)) __instance.m_items.Add(prefab);
            }
            public static void Postfix(ObjectDB __instance)
            {
                //Dbgl("ObjectDB.Awake() Postfix");
                if (!ZNetScene.instance) return;

                if (!__instance.m_recipes.Contains(recipe)) AddRecipe(__instance);
            }
        }

        //Add items and recipes when save games are deserialized and loaded at start scene
        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        public static class ObjectDBCopyOtherDB
        {
            public static void Postfix(ObjectDB __instance)
            {
                //Dbgl("ObjectDB.CopyOtherDB() Postfix");
                if (!__instance.m_items.Contains(prefab))
                {
                    __instance.m_items.Add(prefab);
                    __instance.m_itemByHash.Add(prefab.name.GetStableHashCode(), prefab);
                    //__instance.UpdateItemHashes();
                }
                if (!__instance.m_recipes.Contains(recipe)) AddRecipe(__instance);
            }
        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetSceneAwake
        {
            public static void Postfix(ZNetScene __instance)
            {
                //Dbgl("ZNetScene.Awake() Postfix");
                if (stringDictionary.Count > 0)
                    InitLocalization();

                if (!__instance.m_prefabs.Contains(prefab))
                {
                    __instance.m_prefabs.Add(prefab);
                    __instance.m_namedPrefabs.Add(__instance.GetPrefabHash(prefab), prefab);
                }
            }
        }

        [HarmonyPatch(typeof(Player), "Update")]
        public static class PlayerUpdate
        {
            public static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer) return;
                
                if (isZooming && (!IsSpyglassEquipped(__instance) || ZInput.GetButtonDown("Attack") || config.RemoveZoomKey.IsDown()))
                {
                    StopZoom();
                    return;
                }

                if (!IsSpyglassEquipped(__instance) || InventoryGui.IsVisible()) return;

                if (ZInput.GamepadActive && ZInput.GetButtonDown("JoyLTrigger"))
                {
                    ChangeZoom(ZInput.GetButton("JoyLBumper") ? -1 : 1);
                }

                if (config.IncreaseZoomKey.IsDown())
                    ChangeZoom(1);

                if (config.DecreaseZoomKey.IsDown())
                    ChangeZoom(-1);
            }
        }

        [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
        public static class GameCameraUpdateCamera
        {
            [HarmonyPriority(Priority.Last)]
            public static void Prefix(GameCamera __instance)
            {
                if (isZooming) __instance.m_fov = currentFov;
            }

            [HarmonyAfter(new string[] { "Azumatt.FirstPersonMode" })]
            public static void Postfix(ref GameCamera __instance)
            {
                if (!isZooming) return;

                Vector3 scopeLevel = Vector3.forward * zoomLevel * config.ZoomMultiplier;

                // Try to prevent zooming through things
                if (Physics.Raycast(__instance.transform.position, __instance.transform.forward, out var hitInfo, scopeLevel.magnitude, __instance.m_blockCameraMask))
                {
                    scopeLevel = Vector3.forward * Vector3.Distance(hitInfo.point, __instance.transform.position) * 0.75f;
                }

                __instance.transform.position += __instance.transform.TransformVector(scopeLevel);

                // Keep camera above the ground hopefully
                if (ZoneSystem.instance.GetGroundHeight(__instance.transform.position, out float num) && __instance.transform.position.y < num +1f)
                {
                    Vector3 position = __instance.transform.position;
                    position.y = num + 1f;
                    __instance.transform.position = position;
                }
            }
        }
    }
}
