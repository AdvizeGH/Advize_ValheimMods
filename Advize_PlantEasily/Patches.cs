using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Advize_PlantEasily
{
    public partial class PlantEasily
    {
        [HarmonyPatch(typeof(Player), "UpdateBuildGuiInput")]
        public class PlayerUpdateBuildGuiInput
        {
            public static void Prefix(Player __instance)
            {
                if (Input.GetKeyUp(config.EnableModKey))
                {
                    config.ModActive = !config.ModActive;
                    Dbgl($"modActive was {!config.ModActive} setting to {config.ModActive}");
                    if (__instance.GetRightItem()?.m_shared.m_name == "$item_cultivator")
                    {
                        typeof(Player).GetMethod("SetupPlacementGhost", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[0]);
                    }
                }
                if (Input.GetKeyUp(config.EnableSnappingKey))
                {
                    config.SnapActive = !config.SnapActive;
                    Dbgl($"snapActive was {!config.SnapActive} setting to {config.SnapActive}");
                }
                if (Input.GetKeyUp(config.ToggleAutoReplantKey))
                {
                    config.ReplantOnHarvest = !config.ReplantOnHarvest;
                    Dbgl($"replantOnHarvest was {!config.ReplantOnHarvest} setting to {config.ReplantOnHarvest}");
                }

                if (Input.GetKey(config.KeyboardModifierKey) || Input.GetKey(config.GamepadModifierKey))
                {
                    if (Input.GetKeyUp(config.IncreaseXKey) || ZInput.GetButtonDown("JoyDPadRight"))
                        config.Columns += 1;
                    
                    if (Input.GetKeyUp(config.IncreaseYKey) || ZInput.GetButtonDown("JoyDPadUp"))
                        config.Rows += 1;
                    
                    if (Input.GetKeyUp(config.DecreaseXKey) || ZInput.GetButtonDown("JoyDPadLeft"))
                        config.Columns -= 1;
                    
                    if (Input.GetKeyUp(config.DecreaseYKey) || ZInput.GetButtonDown("JoyDPadDown"))
                        config.Rows -= 1;
                }
            }
        }
        
        [HarmonyPatch(typeof(HotkeyBar), "Update")]
        public class HotKeyBarUpdate
        {
            public static void Prefix(ref bool __runOriginal)
            {
                if (OverrideGamepadInput() && Player.m_localPlayer && !InventoryGui.IsVisible() && !Menu.IsVisible() && !GameCamera.InFreeFly() && !Minimap.IsOpen() && !Hud.IsPieceSelectionVisible() && !StoreGui.IsVisible() && !Console.IsVisible() && !Chat.instance.HasFocus()/* && !PlayerCustomizaton.IsBarberGuiVisible()*/)
                {
                    __runOriginal = false;
                }
            }
        }
        
        [HarmonyPatch(typeof(Player), "StartGuardianPower")]
        public class PlayerStartGuardianPower
        {
            public static void Prefix(ref bool __runOriginal)
            {
                __runOriginal = !OverrideGamepadInput();
            }
        }
        
        [HarmonyPatch(typeof(Player), "SetupPlacementGhost")]
        public class PlayerSetupPlacementGhost
        {
            public static void Postfix(ref GameObject ___m_placementGhost)
            {
                //Dbgl("SetupPlacementGhost");
                DestroyGhosts();
                
                if (!config.ModActive || !___m_placementGhost || NotPlantOrPickable(___m_placementGhost) || Player.m_localPlayer?.GetRightItem()?.m_shared.m_name != "$item_cultivator")
                    return;
                
                CreateGhosts(___m_placementGhost);
            }
        }
        
        [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
        public class PlayerUpdatePlacementGhost
        {
            private class SnapPosition
            {
                internal Vector3 rowDirection;
                internal Vector3 columnDirection;
                internal Vector3 position;

                internal SnapPosition(Vector3 rd, Vector3 cd, Vector3 p)
                {
                    rowDirection = rd;
                    columnDirection = cd;
                    position = p;
                }
            }

            public static void Postfix(Player __instance, ref GameObject ___m_placementGhost, ref int ___m_placementStatus)
            {
                if (!config.ModActive || !___m_placementGhost || NotPlantOrPickable(___m_placementGhost) || __instance.GetRightItem()?.m_shared.m_name != "$item_cultivator")
                    return;
                
                if (ghostPlacementStatus.Count == 0 || (extraGhosts.Count == 0 && !(config.Rows == 1 && config.Columns == 1))) //If there are no extra ghosts but there is supposed to be
                    typeof(Player).GetMethod("SetupPlacementGhost", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[0]);
                
                for (int i = 0; i < extraGhosts.Count; i++)
                {
                    GameObject extraGhost = extraGhosts[i];
                    extraGhost.SetActive(___m_placementGhost.activeSelf);
                }

                Vector3 basePosition = ___m_placementGhost.transform.position;
                Quaternion baseRotation, fixedRotation;
                baseRotation = fixedRotation = ___m_placementGhost.transform.rotation;

                if (config.StandardizeGridRotations)
                {
                    Vector3 vec = fixedRotation.eulerAngles;
                    vec.x = Mathf.Round(vec.x / 90) * 90;
                    vec.y = Mathf.Round(vec.y / 90) * 90;
                    vec.z = Mathf.Round(vec.z / 90) * 90;
                    fixedRotation.eulerAngles = vec;
                }

                float pieceSpacing = GetPieceSpacing(___m_placementGhost);

                // Takes position of ghost, subtracts position of player to get vector between the two and facing out from the player, normalizes that vector to have a magnitude of 1.0f
                Vector3 rowDirection = config.GloballyAlignGridDirections ? basePosition + Vector3.forward - basePosition : Utils.DirectionXZ(basePosition - __instance.transform.position);
                // Cross product of a vertical vector and the forward facing normalized vector, producing a perpendicular lateral vector
                Vector3 columnDirection = Vector3.Cross(Vector3.up, rowDirection);
                
                bool foundSnaps = false;
                if (config.SnapActive)
                {
                    List<SnapPosition> snapPoints = new();
                    Plant plant = ___m_placementGhost.GetComponent<Plant>();

                    Collider[] obstructions = Physics.OverlapSphere(___m_placementGhost.transform.position, pieceSpacing, snapCollisionMask);
                    int validFirstOrderCollisions = 0;
                    
                    foreach (Collider collider in obstructions)
                    {
                        if (NotPlantOrPickable(collider.transform.root.gameObject)) continue;
                        validFirstOrderCollisions++;
                        if (validFirstOrderCollisions > 8) break;
                        
                        Collider[] secondaryObstructions = Physics.OverlapSphere(collider.transform.position, pieceSpacing, snapCollisionMask);
                        int validSecondOrderCollisions = 0;
                        
                        foreach (Collider secondaryCollider in secondaryObstructions)
                        {
                            if (NotPlantOrPickable(secondaryCollider.transform.root.gameObject)) continue;
                            if (secondaryCollider.transform.root == collider.transform.root) continue;
                            validSecondOrderCollisions++;
                            if (validSecondOrderCollisions > 8) break;
                            
                            rowDirection = Utils.DirectionXZ(secondaryCollider.transform.position - collider.transform.position);
                            columnDirection = Vector3.Cross(Vector3.up, rowDirection);
                            
                            rowDirection = (config.StandardizeGridRotations ? fixedRotation : baseRotation) * rowDirection * pieceSpacing;
                            columnDirection = (config.StandardizeGridRotations ? fixedRotation : baseRotation) * columnDirection * pieceSpacing;

                            Vector3[] positions = new Vector3[]
                            {
                                collider.transform.position + rowDirection,
                                collider.transform.position + columnDirection,
                                collider.transform.position - rowDirection,
                                collider.transform.position - columnDirection
                            };

                            foreach (Vector3 pos in positions)
                            {
                                bool invertRowDirection = false;
                                bool invertColumnDirection = false;
                                if (!PositionHasCollisions(pos))
                                {
                                    if (plant && !HasGrowSpace(plant, pos)) continue;
                                    if (config.GridSnappingStyle == 0)
                                    {
                                        if (config.Rows > 1 && PositionHasCollisions(pos + rowDirection))
                                            invertRowDirection = true;
                                        if (config.Columns > 1 && PositionHasCollisions(pos + columnDirection))
                                            invertColumnDirection = true;
                                    }

                                    snapPoints.Add(new SnapPosition(!invertRowDirection ? rowDirection : -rowDirection, !invertColumnDirection ? columnDirection : -columnDirection, pos));
                                    foundSnaps = true;
                                }
                            }
                        }

                        if (!foundSnaps)
                        {
                            rowDirection = baseRotation * rowDirection * pieceSpacing;
                            columnDirection = baseRotation * columnDirection * pieceSpacing;

                            Vector3[] positions = new Vector3[]
                            {
                                collider.transform.position + rowDirection,
                                collider.transform.position + columnDirection,
                                collider.transform.position - rowDirection,
                                collider.transform.position - columnDirection
                            };

                            foreach (Vector3 pos in positions)
                            {
                                bool invertRowDirection = false;
                                bool invertColumnDirection = false;
                                if (!PositionHasCollisions(pos))
                                {
                                    if (plant && !HasGrowSpace(plant, pos)) continue;
                                    if (config.GridSnappingStyle == 0)
                                    {
                                        if (config.Rows > 1 && PositionHasCollisions(pos + rowDirection))
                                            invertRowDirection = true;
                                        if (config.Columns > 1 && PositionHasCollisions(pos + columnDirection))
                                            invertColumnDirection = true;
                                    }

                                    snapPoints.Add(new SnapPosition(!invertRowDirection ? rowDirection : -rowDirection, !invertColumnDirection ? columnDirection : -columnDirection, pos));
                                    foundSnaps = true;
                                }
                            }
                        }
                    }
                    
                    if (foundSnaps)
                    {
                        SnapPosition firstSnapPos = snapPoints.OrderBy(o => snapPoints.Min(m => Utils.DistanceXZ(m.position, o.position)) + (o.position - basePosition).magnitude).First();
                        basePosition = ___m_placementGhost.transform.position = firstSnapPos.position;
                        if (config.GridSnappingStyle == 0)
                        {
                            rowDirection = firstSnapPos.rowDirection;
                            columnDirection = firstSnapPos.columnDirection;
                        }
                    }
                }
                
                if (!foundSnaps)
                {
                    if (config.SnapActive)
                    {
                        rowDirection = config.GloballyAlignGridDirections ? basePosition + Vector3.forward - basePosition : Utils.DirectionXZ(basePosition - __instance.transform.position);
                        columnDirection = Vector3.Cross(Vector3.up, rowDirection);
                    }

                    rowDirection = baseRotation * rowDirection * pieceSpacing;
                    columnDirection = baseRotation * columnDirection * pieceSpacing;
                }
                
                Piece piece = ___m_placementGhost.GetComponent<Piece>();
                int cost = piece.m_resources[0].m_amount;
                int currentCost = 0;
                
                for (int row = 0; row < config.Rows; row++)
                {
                    for (int column = 0; column < config.Columns; column++)
                    {
                        currentCost += cost;
                        piece.m_resources[0].m_amount = currentCost;
                        bool isRoot = (column == 0 && row == 0);
                        int ghostIndex = row * config.Columns + column;
                        GameObject ghost = isRoot ? ___m_placementGhost : extraGhosts[ghostIndex - 1];
                        
                        Vector3 ghostPosition = isRoot ? basePosition : basePosition + rowDirection * row + columnDirection * column;
                        
                        Heightmap.GetHeight(ghostPosition, out float height);
                        ghostPosition.y = height;
                        
                        ghost.transform.position = ghostPosition;
                        ghost.transform.rotation = ___m_placementGhost.transform.rotation;

                        Status status = Status.Healthy;
                        if (!__instance.m_noPlacementCost && !__instance.HaveRequirements(piece, Player.RequirementMode.CanBuild))
                            status = Status.LackResources;

                        SetPlacementGhostStatus(ghost, ghostIndex, CheckPlacementStatus(ghost, status), ref ___m_placementStatus);
                    }
                }
                ___m_placementGhost.GetComponent<Piece>().m_resources[0].m_amount = cost;
            }
        }
        
        [HarmonyPatch(typeof(Player), "PlacePiece")]
        public class PlayerPlacePiece
        {
            public static bool Prefix(Player __instance, Piece piece, ref int ___m_placementStatus, ref bool __result)
            {
                //Dbgl("Player.PlacePiece Prefix");
                if (!config.ModActive || !piece || NotPlantOrPickable(piece.gameObject) || __instance.GetRightItem()?.m_shared.m_name != "$item_cultivator")
                    return true;

                if (config.PreventInvalidPlanting)
                {
                    int rootPlacementStatus = (int)CheckPlacementStatus(__instance.m_placementGhost);
                    if (rootPlacementStatus > 1)
                    {
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, statusMessage[rootPlacementStatus]);
                        SetPlacementGhostStatus(__instance.m_placementGhost, 0, Status.Invalid, ref ___m_placementStatus);
                        __result = false;
                        return false;
                    }
                }
                
                if (config.PreventPartialPlanting)
                {
                    foreach (int i in ghostPlacementStatus)
                    {
                        if (i != 0)
                        {
                            if (!(i == 1 && __instance.m_noPlacementCost))
                            {
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, statusMessage[i]);
                                SetPlacementGhostStatus(__instance.m_placementGhost, 0, Status.Invalid, ref ___m_placementStatus);
                                __result = false;
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            
            public static void Postfix(Player __instance, Piece piece)
            {
                //Dbgl("Player.PlacePiece Postfix");
                if (!config.ModActive || !piece || NotPlantOrPickable(piece.gameObject) || __instance.GetRightItem()?.m_shared.m_name != "$item_cultivator")
                    return;
                
                //This doesn't apply to the root placement ghost.
                if (ghostPlacementStatus[0] == Status.Healthy) // With this, root Ghost must be valid (can be fixed)
                {
                    ItemDrop.ItemData rightItem = __instance.GetRightItem();
                    int count = extraGhosts.Count;

                    for (int i = 0; i < extraGhosts.Count; i++)
                    {
                        if (ghostPlacementStatus[i + 1] != Status.Healthy)
                        {
                            count--;
                            if (!(__instance.m_noPlacementCost && ghostPlacementStatus[i + 1] == Status.LackResources))
                                continue;
                        }

                        PlacePiece(__instance, extraGhosts[i], piece);
                    }

                    count = __instance.m_noPlacementCost ? 0 : count;

                    for (int i = 0; i < count; i++)
                        __instance.ConsumeResources(piece.m_resources, 0);

                    if (config.UseStamina)
                        __instance.UseStamina(rightItem.m_shared.m_attack.m_attackStamina * count);

                    if (rightItem.m_shared.m_useDurability && config.UseDurability)
                        rightItem.m_durability -= rightItem.m_shared.m_useDurabilityDrain * count;
                }
            }
        }

        [HarmonyPatch(typeof(Player), "Interact")]
        public class PlayerInteract
        {
            public static void Prefix(Player __instance, GameObject go, bool hold, bool alt)
            {
                if (!config.ModActive || (!config.EnableBulkHarvest && !config.ReplantOnHarvest) || __instance.InAttack() || __instance.InDodge() || (hold && Time.time - __instance.m_lastHoverInteractTime < 0.2f))
                    return;

                Interactable interactable = go.GetComponentInParent<Interactable>();
                if (interactable == null) return;

                if (interactable as Pickable && config.ReplantOnHarvest && pickablesToPlants.ContainsKey(interactable.ToString().Replace("(Clone) (Pickable)", "")))
                    instanceIDS.Add(((Pickable)interactable).GetInstanceID());

                if (!config.EnableBulkHarvest || (!Input.GetKey(config.KeyboardHarvestModifierKey) && !Input.GetKey(config.GamepadModifierKey)))
                    return;

                if (interactable as Pickable || interactable as Beehive)
                {
                    foreach (Interactable extraInteractable in FindResourcesInRadius(go))
                    {
                        if (config.ReplantOnHarvest)
                        {
                            if (pickablesToPlants.ContainsKey(extraInteractable.ToString().Replace("(Clone) (Pickable)", "")))
                                instanceIDS.Add(((Pickable)extraInteractable).GetInstanceID());
                        }
                        extraInteractable.Interact(__instance, hold, alt);
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(InventoryGui), "SetupRequirement")]
        public class InventoryGuiSetupRequirement
        {
            [HarmonyPriority(Priority.Last)]
            public static void Postfix(Transform elementRoot, Piece.Requirement req)
            {
                if (extraGhosts.Count < 1 || !config.ShowCost) return;

                Text component = elementRoot.transform.Find("res_amount").GetComponent<Text>();
                int totalGhosts = ghostPlacementStatus.Count;

                string formattedCost = config.CostDisplayStyle == 0 ? config.CostDisplayLocation == 0 ? 
                    $"{totalGhosts}x" : $"x{totalGhosts}" : $"({req.m_amount * totalGhosts})";

                component.text = config.CostDisplayLocation == 0 ? formattedCost + component.text : component.text + formattedCost;
            }
        }

        [HarmonyPatch(typeof(Pickable), "SetPicked")]
        public class PickableSetPicked
        {
            public static void Prefix(Pickable __instance, bool picked)
            {
                if (!config.ModActive || !config.ReplantOnHarvest || instanceIDS.Count == 0 || !picked) return;

                int instanceID = __instance.GetInstanceID();
                if (!instanceIDS.Contains(instanceID)) return;

                instanceIDS.Remove(instanceID);

                Player player = Player.m_localPlayer;
                GameObject plantObject = prefabRefs[pickablesToPlants[__instance.name.Replace("(Clone)", "")]];
                Piece piece = plantObject.GetComponent<Piece>();

                if (!player.HaveRequirements(piece, Player.RequirementMode.CanBuild)) return;

                PlacePiece(player, __instance.gameObject, piece);
                player.ConsumeResources(piece.m_resources, 0);
            }
        }

        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public static class ObjectDBAwake
        {
            public static void Postfix() => InitPrefabRefs();
        }
    }
}
