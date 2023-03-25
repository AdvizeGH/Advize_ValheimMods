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
					modActive = !modActive;
					Dbgl($"modActive was {!modActive} setting to {modActive}");
					if (__instance.GetRightItem()?.m_shared.m_name == "$item_cultivator")
					{
						typeof(Player).GetMethod("SetupPlacementGhost", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[0]);
					}
				}
				if (Input.GetKeyUp(config.EnableSnappingKey))
				{
					snapActive = !snapActive;
					Dbgl($"snapActive was {!snapActive} setting to {snapActive}");
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

				if (!modActive || !___m_placementGhost)
					return;

				if (___m_placementGhost.GetComponent<Piece>().m_repairPiece)
					return;

				if (!___m_placementGhost.GetComponent<Plant>() && !___m_placementGhost.GetComponent<Pickable>())
					return;

				placementGhost = ___m_placementGhost;

				if (!(config.Rows == 1 && config.Columns == 1))
				{
					CreateGhosts(___m_placementGhost);
				}
			}
		}
		
		[HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
		public class PlayerUpdatePlacementGhost
		{
			public static void Postfix(Player __instance, ref GameObject ___m_placementGhost)
			{
				if (!modActive || !___m_placementGhost || (!___m_placementGhost.GetComponent<Plant>() && !___m_placementGhost.GetComponent<Pickable>()))
					return;

				if (extraGhosts.Count == 0 && !(config.Rows == 1 && config.Columns == 1)) //If there are no extra ghosts but there is supposed to be
					typeof(Player).GetMethod("SetupPlacementGhost", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[0]);

				for (int i = 0; i < extraGhosts.Count; i++)
				{
					//Dbgl($"Calling SetPlacementGhostStatus index is: {i} extraGhosts.Count is: {extraGhosts.Count}");
					GameObject extraGhost = extraGhosts[i];
					extraGhost.SetActive(___m_placementGhost.activeSelf);
					SetPlacementGhostStatus(extraGhost, i + 1, 0);
				}

				Vector3 basePosition = ___m_placementGhost.transform.position;
				Quaternion baseRotation = ___m_placementGhost.transform.rotation;

				float colliderRadius = 0f;
				Plant plant = ___m_placementGhost.GetComponent<Plant>();

				//Find collider with largest radius within the piece to be placed.
				//Include grownPrefabs just in case. Still doesn't seem to work on saplings even when multiplied against max growth scale of the tree.
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
							colliderRadius = Mathf.Max(colliderRadius, collider.radius/* * plant.m_maxScale*/);
							//Dbgl($"colliderRadius is now: {colliderRadius}");
						}
					}
					// Add 10% to that radius because for some reason some crops or saplings still collide and wither without it
					// ~~~~This may no longer be required, test this when time allows~~~~
					colliderRadius *= 1.1f; // Maybe edit this later, see what works best
				}

				float growRadius = plant?.m_growRadius ?? PickableSnapRadius(___m_placementGhost.name);
				float pieceSpacing = growRadius + colliderRadius;

				// Takes position of ghost, subtracts position of player to get vector between the two and facing out from the player, normalizes that vector to have a magnitude of 1.0f
				Vector3 rowDirection = (basePosition - __instance.transform.position).normalized;
				// Cross product of a vertical vector and the forward facing normalized vector, producing a perpendicular lateral vector
				Vector3 columnDirection = Vector3.Cross(Vector3.up, rowDirection);

				bool foundSnaps = false;
				if (snapActive)
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
								//break;
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

						Heightmap heightmap = Heightmap.FindHeightmap(ghostPosition);
						
						Heightmap.GetHeight(ghostPosition, out float height);
						ghostPosition.y = height;

						ghost.transform.position = ghostPosition;
						ghost.transform.rotation = ___m_placementGhost.transform.rotation;

						SetPlacementGhostStatus(ghost, ghostIndex, CheckPlacementStatus(ghost, ghostPosition, heightmap));

						if (__instance.GetInventory().CountItems(resource.m_resItem.m_itemData.m_shared.m_name) < currentCost)
						{
							SetPlacementGhostStatus(ghost, ghostIndex, 1);
						}
					}
				}
			}
		}

		[HarmonyPatch(typeof(Player), "PlacePiece")]
		public class PlayerPlacePiece
		{
			public static bool Prefix(Piece piece, bool ___m_noPlacementCost, ref bool __result/*, out bool __state*/)
            {
				Dbgl("Player.PlacePiece Prefix");
				if (!piece || (!piece.GetComponent<Plant>() && !piece.GetComponent<Pickable>())/* || ___m_noPlacementCost*/)
					return true;

				if (config.PreventPartialPlanting)
				{
					foreach (int i in ghostPlacementStatus)
					{
						if (i != 0)
						{
							if (i == 1 && ___m_noPlacementCost)
                            {
								continue;
                            }
							Dbgl("Preventing partially valid placements, returning");
							for (int j = 0; j < ghostPlacementStatus.Count; j++)
								ghostPlacementStatus[j] = 1;
							__result = false;
							return false;
						}
					}
				}
				return true;
			}
			public static void Postfix(Player __instance, Piece piece, bool ___m_noPlacementCost, int ___m_placementStatus)
			{
				Dbgl("Player.PlacePiece Postfix");
				if (!piece || (!piece.GetComponent<Plant>() && !piece.GetComponent<Pickable>()))
					return;

				//This doesn't apply to the root placement ghost.
				if (___m_placementStatus == 0) // With this, root Ghost must be valid (can be fixed)
				{
					ItemDrop.ItemData rightItem = __instance.GetRightItem();
					int count = extraGhosts.Count;

					for (int i = 0; i < extraGhosts.Count; i++)
					{
						if (ghostPlacementStatus[i + 1] != 0 && (!___m_noPlacementCost && ghostPlacementStatus[i + 1] == 1))
						{
							Dbgl($"extraGhost[{i}] placementStatus is invalid. Skipping placement.");
							count--;
							continue;
                        }
						Vector3 position = extraGhosts[i].transform.position;
						Quaternion rotation = config.RandomizeRotation ? Quaternion.Euler(0f, 22.5f * Random.Range(0, 16), 0f) : extraGhosts[i].transform.rotation;
						GameObject gameObject = piece.gameObject;
						TerrainModifier.SetTriggerOnPlaced(trigger: true);
						GameObject gameObject2 = Instantiate(gameObject, position, rotation);
						TerrainModifier.SetTriggerOnPlaced(trigger: false);
						Piece component = gameObject2.GetComponent<Piece>();
						if (component)
						{
							component.SetCreator(__instance.GetPlayerID());
						}
						PrivateArea component2 = gameObject2.GetComponent<PrivateArea>();
						if (component2)
						{
							component2.Setup(Game.instance.GetPlayerProfile().GetName());
						}
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
