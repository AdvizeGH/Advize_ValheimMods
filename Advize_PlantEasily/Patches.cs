using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

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
                if (OverrideGamepadInput() && Player.m_localPlayer && !InventoryGui.IsVisible() && !Menu.IsVisible() && !GameCamera.InFreeFly() && !Minimap.IsOpen())
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
                
                if (!config.ModActive || !___m_placementGhost)
                    return;
                
                if (___m_placementGhost.GetComponent<Piece>().m_repairPiece)
                    return;
                
                if (!___m_placementGhost.GetComponent<Plant>() && !___m_placementGhost.GetComponent<Pickable>())
                    return;
                
                CreateGhosts(___m_placementGhost);
            }
        }
        
        [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
        public class PlayerUpdatePlacementGhost
        {
            public static void Postfix(Player __instance, bool ___m_noPlacementCost, ref GameObject ___m_placementGhost)
            {
                if (!config.ModActive || !___m_placementGhost || (!___m_placementGhost.GetComponent<Plant>() && !___m_placementGhost.GetComponent<Pickable>()))
                    return;
                
                if (ghostPlacementStatus.Count == 0 || (extraGhosts.Count == 0 && !(config.Rows == 1 && config.Columns == 1))) //If there are no extra ghosts but there is supposed to be
                    typeof(Player).GetMethod("SetupPlacementGhost", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[0]);
                
                for (int i = 0; i < extraGhosts.Count; i++)
                {
                    GameObject extraGhost = extraGhosts[i];
                    extraGhost.SetActive(___m_placementGhost.activeSelf);
                    SetPlacementGhostStatus(extraGhost, i + 1, Status.Healthy);
                }
                SetPlacementGhostStatus(___m_placementGhost, 0, Status.Healthy);

                Vector3 basePosition = ___m_placementGhost.transform.position;
                Quaternion baseRotation = ___m_placementGhost.transform.rotation;

                float colliderRadius = 0f;
                Plant plant = ___m_placementGhost.GetComponent<Plant>();
                
                //Find collider with largest radius within the piece to be placed.
                //Include grownPrefabs just in case. Still doesn't seem to work on saplings even when multiplied against max growth scale of the tree. (Cause might be Status.NoSun)
                if (plant)
                {
                    List<GameObject> colliderRoots = new();
                    colliderRoots.Add(___m_placementGhost);
                    colliderRoots.AddRange(plant.m_grownPrefabs);
                    
                    for (int i = 0; i < colliderRoots.Count; i++)
                    {
                        foreach (CapsuleCollider collider in colliderRoots[i].GetComponentsInChildren<CapsuleCollider>())
                        {
                            //Dbgl($"colliderRadius was: {colliderRadius}");
                            colliderRadius = Mathf.Max(colliderRadius, collider.radius);
                            //Dbgl($"colliderRadius is now: {colliderRadius}");
                        }
                    }
                    // Add 10% to that radius because for some reason some crops or saplings still collide and wither without it
                    // No longer needed. Issue was due to player height relative to ground level. Higher altitudes led to wildly different vector magnitudes.
                    //colliderRadius *= 1.1f;
                }

                float growRadius = plant?.m_growRadius ?? PickableSnapRadius(___m_placementGhost.name);
                float pieceSpacing = growRadius + colliderRadius;

                // Takes position of ghost, subtracts position of player to get vector between the two and facing out from the player, normalizes that vector to have a magnitude of 1.0f
                Vector3 rowDirection = Utils.DirectionXZ((basePosition - __instance.transform.position));
                    
                // Cross product of a vertical vector and the forward facing normalized vector, producing a perpendicular lateral vector
                Vector3 columnDirection = Vector3.Cross(Vector3.up, rowDirection);
                
                bool foundSnaps = false;
                if (config.SnapActive)
                {
                    List<Vector3> snapPoints = new();
                    Collider[] obstructions = Physics.OverlapSphere(___m_placementGhost.transform.position, pieceSpacing, snapCollisionMask);
                    
                    if (obstructions?.Length > 0)
                    {
                        foreach (Collider collider in obstructions)
                        {
                            if (foundSnaps) break;
                            if (!collider.GetComponent<Plant>() && !collider.GetComponentInParent<Pickable>()) continue;
                            
                            Collider[] secondaryObstructions = Physics.OverlapSphere(collider.transform.position, pieceSpacing, snapCollisionMask);
                            if (secondaryObstructions?.Length > 0)
                            {
                                foreach (Collider secondaryCollider in secondaryObstructions)
                                {
                                    if (!secondaryCollider.GetComponent<Plant>() && !secondaryCollider.GetComponentInParent<Pickable>()) continue;
                                    if (secondaryCollider.transform.root == collider.transform.root) continue;
                                    
                                    // Note to self:
                                    // Make rows and columns consistant
                                    // Determine whether row or column should be the cross vector based on position relative to the player (rows should extend away from or towards player).
                                    // Consider whether this could be more easily facilitated by adjusting snap point priority
                                    
                                    Vector3 normalizedVector = (secondaryCollider.transform.position - collider.transform.position).normalized;
                                    if (normalizedVector.magnitude == 0) continue;
                                    
                                    rowDirection = normalizedVector;
                                    columnDirection = Vector3.Cross(Vector3.up, rowDirection);
                                    
                                    rowDirection = baseRotation * rowDirection * pieceSpacing;
                                    columnDirection = baseRotation * columnDirection * pieceSpacing;
                                    
                                    snapPoints.Add(collider.transform.position + rowDirection);
                                    snapPoints.Add(collider.transform.position + columnDirection);
                                    snapPoints.Add(collider.transform.position - rowDirection);
                                    snapPoints.Add(collider.transform.position - columnDirection);
                                    
                                    foundSnaps = true;
                                    //continue;
                                    break;
                                }
                            }
                            if (!foundSnaps)
                            {
                                Vector3 normalizedVector = baseRotation * rowDirection * pieceSpacing;
                                if (normalizedVector.magnitude == 0) continue;
                                
                                rowDirection = normalizedVector;
                                columnDirection = baseRotation * columnDirection * pieceSpacing;
                                
                                snapPoints.Add(collider.transform.position + rowDirection);
                                snapPoints.Add(collider.transform.position + columnDirection);
                                snapPoints.Add(collider.transform.position - rowDirection);
                                snapPoints.Add(collider.transform.position - columnDirection);
                                
                                foundSnaps = true;
                                //continue;
                                break;
                            }
                        }
                        
                        if (foundSnaps)
                        {
                            Vector3 firstSnapPos = snapPoints.OrderBy(o => snapPoints.Where(w => w != o).Min(m => Utils.DistanceXZ(m, o)) + (o - basePosition).magnitude).First();
                            basePosition = ___m_placementGhost.transform.position = firstSnapPos;
                        }
                    }
                }
                
                if (!foundSnaps)
                {
                    rowDirection *= pieceSpacing;
                    columnDirection *= pieceSpacing;
                }
                
                Piece.Requirement resource = ___m_placementGhost.GetComponent<Piece>().m_resources[0];
                int cost = resource.m_amount;
                int currentCost = 0;
                
                for (int row = 0; row < config.Rows; row++)
                {
                    for (int column = 0; column < config.Columns; column++)
                    {
                        currentCost += cost;
                        bool isRoot = (column == 0 && row == 0);
                        int ghostIndex = row * config.Columns + column;
                        GameObject ghost = isRoot ? ___m_placementGhost : extraGhosts[ghostIndex - 1];
                        
                        Vector3 ghostPosition = isRoot ? basePosition : basePosition + rowDirection * row + columnDirection * column;
                        
                        Heightmap.GetHeight(ghostPosition, out float height);
                        ghostPosition.y = height;
                        
                        ghost.transform.position = ghostPosition;
                        ghost.transform.rotation = ___m_placementGhost.transform.rotation;

                        if (!___m_noPlacementCost && __instance.GetInventory().CountItems(resource.m_resItem.m_itemData.m_shared.m_name) < currentCost)
                        {
                            SetPlacementGhostStatus(ghost, ghostIndex, Status.LackResources);
                        }

                        SetPlacementGhostStatus(ghost, ghostIndex, CheckPlacementStatus(ghost, ghostPlacementStatus[ghostIndex]));
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(Player), "PlacePiece")]
        public class PlayerPlacePiece
        {
            public static bool Prefix(Piece piece, bool ___m_noPlacementCost, GameObject ___m_placementGhost, ref bool __result)
            {
                //Dbgl("Player.PlacePiece Prefix");
                if (!piece || (!piece.GetComponent<Plant>() && !piece.GetComponent<Pickable>()))
                    return true;

                if (config.PreventInvalidPlanting)
                {
                    if ((int)CheckPlacementStatus(___m_placementGhost) > 1)
                    {
                        SetPlacementGhostStatus(___m_placementGhost, 0, Status.Invalid);
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
                            if (i == 1 && ___m_noPlacementCost)
                                continue;

                            SetPlacementGhostStatus(___m_placementGhost, 0, Status.Invalid);
                            __result = false;
                            return false;
                        }
                    }
                }
                return true;
            }
            
            public static void Postfix(Player __instance, Piece piece, bool ___m_noPlacementCost)
            {
                //Dbgl("Player.PlacePiece Postfix");
                if (!piece || (!piece.GetComponent<Plant>() && !piece.GetComponent<Pickable>()))
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
                            if (ghostPlacementStatus[i + 1] == Status.LackResources && ___m_noPlacementCost)
                                count--;
                            else
                                continue;
                        }

                        Vector3 position = extraGhosts[i].transform.position;
                        Quaternion rotation = config.RandomizeRotation ? Quaternion.Euler(0f, 22.5f * Random.Range(0, 16), 0f) : extraGhosts[i].transform.rotation;
                        GameObject gameObject = piece.gameObject;

                        TerrainModifier.SetTriggerOnPlaced(trigger: true);
                        GameObject gameObject2 = Instantiate(gameObject, position, rotation);
                        TerrainModifier.SetTriggerOnPlaced(trigger: false);

                        gameObject2.GetComponent<Piece>()?.SetCreator(__instance.GetPlayerID());
                        gameObject2.GetComponent<PrivateArea>()?.Setup(Game.instance.GetPlayerProfile().GetName());

                        piece.m_placeEffect.Create(position, rotation, gameObject2.transform, 1f);
                        __instance.AddNoise(50f);

                        Game.instance.GetPlayerProfile().m_playerStats.m_builds++;
                        ZLog.Log("Placed " + gameObject.name);
                        Gogan.LogEvent("Game", "PlacedPiece", gameObject.name, 0L);
                    }
                    for (int i = 0; i < count; i++)
                    {
                        __instance.ConsumeResources(piece.m_resources, 0);
                    }
                    if (rightItem.m_shared.m_useDurability && config.UseDurability)
                    {
                        rightItem.m_durability -= rightItem.m_shared.m_useDurabilityDrain * count;
                    }
                }
            }
        }
    }
}
