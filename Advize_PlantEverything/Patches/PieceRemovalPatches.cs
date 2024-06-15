namespace Advize_PlantEverything;

using HarmonyLib;
using UnityEngine;
using static PlantEverything;
using static PluginUtils;
using static StaticContent;

[HarmonyPatch]
static class PieceRemovalPatches
{
    [HarmonyPatch(typeof(Player), nameof(Player.CheckCanRemovePiece))]
    static bool Prefix(Piece piece, ref bool __result) => !IsModdedPrefab(piece) || (__result = false);

    [HarmonyPatch(typeof(Player), nameof(Player.RemovePiece))]
    static class PlayerRemovePiece
    {
        static bool Prefix(Player __instance, ref bool __result)
        {
            if (__instance.GetRightItem().m_shared.m_name == "$item_cultivator")
            {
                Transform t = GameCamera.instance.transform;
                if (Physics.Raycast(t.position, t.forward, out var hitInfo, 50f, LayerMask.GetMask(layersForPieceRemoval)) && Vector3.Distance(hitInfo.point, __instance.m_eye.position) < __instance.m_maxPlaceDistance)
                {
                    Piece piece = hitInfo.collider.GetComponentInParent<Piece>();

                    if (IsModdedPrefab(piece))
                    {
                        if (!CanRemove(piece, __instance)) return false;

                        RemoveObject(piece, __instance);
                        __result = true;
                    }
                }

                return false;
            }

            return true;
        }

        static bool CanRemove(Piece piece, Player instance)
        {
            bool canRemove = piece.m_canBeRemoved || (config.CanRemoveFlora && piece.IsPlacedByPlayer() && !piece.m_name.Contains("sapling"));

            if (canRemove && !PrivateArea.CheckAccess(piece.transform.position))
            {
                instance.Message(MessageHud.MessageType.Center, "$msg_privatezone");
                canRemove = false;
            }

            return canRemove;
        }

        static void RemoveObject(Piece piece, Player player)
        {
            ZNetView znv = piece.m_nview;
            WearNTear wnt = piece.GetComponent<WearNTear>();

            if (wnt)
            {
                player.m_removeEffects.Create(piece.transform.position, Quaternion.identity);
                wnt.Remove();
            }
            else
            {
                znv.ClaimOwnership();
                piece.DropResources();
                piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation);

                if (piece.GetComponent<Pickable>())
                {
                    znv.InvokeRPC("RPC_Pick");
                }

                ZNetScene.instance.Destroy(piece.gameObject);
            }

            player.FaceLookDirection();
            player.m_zanim.SetTrigger(player.GetRightItem().m_shared.m_attack.m_attackAnimation);
        }
    }

    [HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
    static class PieceDropResources
    {
        static void Prefix(Piece __instance, ref Piece.Requirement[] __state)
        {
            if (!config.RecoverResources || !IsModdedPrefab(__instance) || !__instance.TryGetComponent(out Pickable pickable)) return;

            __state = __instance.m_resources;
            __instance.m_resources = RemovePickableDropFromRequirements(__instance.m_resources, pickable);
        }

        static void Postfix(Piece __instance, Piece.Requirement[] __state)
        {
            if (__state != null)
            {
                // Restore resources if they were changed
                __instance.m_resources = __state;
            }
        }

        static Piece.Requirement[] RemovePickableDropFromRequirements(Piece.Requirement[] requirements, Pickable pickable)
        {
            ItemDrop.ItemData pickableDrop = pickable.m_itemPrefab.GetComponent<ItemDrop>().m_itemData;

            // Check if pickable is included in piece build requirements
            for (int i = 0; i < requirements.Length; i++)
            {
                Piece.Requirement req = requirements[i];
                if (req.m_resItem.m_itemData.m_shared.m_name == pickableDrop.m_shared.m_name)
                {
                    // Make a copy before altering drops
                    Piece.Requirement[] pickedRequirements = new Piece.Requirement[requirements.Length];
                    requirements.CopyTo(pickedRequirements, 0);

                    // Get amount returned on picking based on world modifiers
                    int pickedAmount = GetScaledPickableDropAmount(pickable);

                    // Reduce drops by the amount that picking the item gave.
                    // This is to prevent infinite resource exploits.
                    pickedRequirements[i].m_amount = Mathf.Clamp(req.m_amount - pickedAmount, 0, req.m_amount);
                    return pickedRequirements;
                }
            }

            // If no pickable item, return the requirements array unchanged.
            return requirements;
        }

        static int GetScaledPickableDropAmount(Pickable pickable)
        {
            return pickable.m_dontScale ? pickable.m_amount : Mathf.Max(pickable.m_minAmountScaled, Game.instance.ScaleDrops(pickable.m_itemPrefab, pickable.m_amount));
        }
    }
}
