using Advize_PlantEverything.Framework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Advize_PlantEverything
{
	public partial class PlantEverything
	{
		[HarmonyPatch]
		public static class ModInitPatches
		{
			[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
			public static void Postfix()
			{
				Dbgl("ObjectDBAwake");
				InitPrefabRefs();
			}

			[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
			public static void Postfix(ZNetScene __instance)
			{
				Dbgl("ZNetSceneAwake");
				FinalInit(__instance);
			}

			[HarmonyPostfix]
			[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
			[HarmonyPriority(Priority.Last)]
			public static void LastPostfix(ZNetScene __instance)
			{
				if (!resolveMissingReferences) return;

				Dbgl("ZNetSceneAwake2");
				Dbgl("Performing final attempt to resolve missing references for configured ExtraResources", true);

				resolveMissingReferences = false;

				if (InitExtraResourceRefs(__instance, true))
				{
					Dbgl("One or more missing references for configured ExtraResources were successfully resolved", true);
					PieceSettingChanged(null, null);
				}
					
			}
		}

		[HarmonyPatch(typeof(Player), nameof(Player.CheckCanRemovePiece))]
		public static class PlayerCheckCanRemovePiece
		{
			private static bool Prefix(Piece piece, ref bool __result) => !IsModdedPrefab(piece) || (__result = false);
		}

		[HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
		public static class PieceDropResources
		{
			internal static void Prefix(Piece __instance, out Piece.Requirement[] __state)
			{
				__state = null;
				if (!config.RecoverResources || !IsModdedPrefab(__instance) || !__instance.TryGetComponent(out Pickable pickable)) return;

				__state = __instance.m_resources;
				__instance.m_resources = RemovePickableDropFromRequirements(__instance.m_resources, pickable);
			}

			internal static void Postfix(Piece __instance, Piece.Requirement[] __state)
			{
				if (__state != null)
				{
					// Restore resources if they were changed
					__instance.m_resources = __state;
				}
			}

			private static Piece.Requirement[] RemovePickableDropFromRequirements(Piece.Requirement[] requirements, Pickable pickable)
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

			private static int GetScaledPickableDropAmount(Pickable pickable)
			{
				return pickable.m_dontScale ? pickable.m_amount : Mathf.Max(pickable.m_minAmountScaled, Game.instance.ScaleDrops(pickable.m_itemPrefab, pickable.m_amount));
			}
		}

		[HarmonyPatch(typeof(Player), nameof(Player.RemovePiece))]
		public static class PlayerRemovePiece
		{
			public static bool Prefix(Player __instance, ref bool __result)
			{
				if (__instance.GetRightItem().m_shared.m_name == "$item_cultivator")
				{
					Transform t = GameCamera.instance.transform;
					if (Physics.Raycast(t.position, t.forward, out var hitInfo, 50f, GetRemovalMask()) && Vector3.Distance(hitInfo.point, __instance.m_eye.position) < __instance.m_maxPlaceDistance)
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

			private static LayerMask GetRemovalMask() => LayerMask.GetMask(StaticContent.layersForPieceRemoval);

			private static bool CanRemove(Piece piece, Player instance)
			{
				bool canRemove = piece.m_canBeRemoved;

				if (canRemove && !PrivateArea.CheckAccess(piece.transform.position))
				{
					instance.Message(MessageHud.MessageType.Center, "$msg_privatezone");
					canRemove = false;
				}

				return canRemove;
			}

			private static void RemoveObject(Piece piece, Player player)
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

		[HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
		public static class PieceSetCreator
		{
			public static void Postfix(Piece __instance)
			{
				if (!IsModdedPrefabOrSapling(__instance.m_name)) return;
				
				if (config.ResourcesSpawnEmpty && __instance.GetComponent<Pickable>() && !__instance.m_name.Contains("Stone"))
				{
					__instance.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetPicked", true);
				}

				if (config.PlaceAnywhere && __instance.TryGetComponent(out StaticPhysics sp))
				{
					sp.m_fall = false;
					__instance.m_nview.GetZDO().Set("pe_placeAnywhere", true);
				}
			}
		}

		[HarmonyPatch]
		public static class CheckZDOPatches
		{
			[HarmonyPatch(typeof(Piece), nameof(Piece.Awake))]
			[HarmonyPatch(typeof(TreeBase), nameof(TreeBase.Awake))]
			public static void Postfix(ZNetView ___m_nview)
			{
				if (!___m_nview || ___m_nview.GetZDO() == null || !___m_nview.GetZDO().GetBool("pe_placeAnywhere")) return;

				___m_nview.GetComponent<StaticPhysics>().m_fall = false;
			}
		}

		[HarmonyPatch(typeof(Plant), nameof(Plant.HaveRoof))]
		public static class PlantHaveRoof
		{
			public static bool Prefix(Plant __instance, ref bool __result)
			{
				if ((!config.CropRequireSunlight && __instance.m_name.StartsWith("$piece_sapling")) || (config.PlaceAnywhere && IsModdedPrefabOrSapling(__instance.m_name)))
					return __result = false;

				return true;
			}
		}

		[HarmonyPatch(typeof(Plant), nameof(Plant.HaveGrowSpace))]
		public static class PlantHaveGrowSpace
		{
			public static bool Prefix(Plant __instance, ref bool __result)
			{
				if ((!config.CropRequireGrowthSpace && __instance.m_name.StartsWith("$piece_sapling")) || (config.PlaceAnywhere && IsModdedPrefabOrSapling(__instance.m_name)))
				{
					__result = true;
					return false;
				}

				return true;
			}
		}

		[HarmonyPatch(typeof(Plant), nameof(Plant.Grow))]
		public static class PlantGrow
		{
			private static readonly MethodInfo ModifyGrowMethod = AccessTools.Method(typeof(PlantGrow), nameof(ModifyGrow));

			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				return new CodeMatcher(instructions)
				.MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(TreeBase), nameof(TreeBase.Grow))))
				.Advance(1)
				.InsertAndAdvance(new CodeInstruction[] { new(OpCodes.Ldarg_0), new(OpCodes.Ldloc_1), new(OpCodes.Call, ModifyGrowMethod) })
				.InstructionEnumeration();
			}

			private static void ModifyGrow(Plant plant, GameObject grownTree)
			{
				if (!plant.m_nview || !plant.m_nview.GetZDO().GetBool("pe_placeAnywhere") || !grownTree.TryGetComponent(out TreeBase tb) || !tb.TryGetComponent(out StaticPhysics sp))
					return;

				sp.m_fall = false;
				tb.m_nview.GetZDO().Set("pe_placeAnywhere", true);
			}
		}

		[HarmonyPatch]
		public static class HoverTextPatches
		{
			[HarmonyPatch(typeof(Pickable), nameof(Pickable.GetHoverText))]
			public static void Postfix(Pickable __instance, ref string __result)
			{
				if (__instance.m_picked && config.EnablePickableTimers && __instance.m_nview.GetZDO() != null)
				{
					if (__instance.m_respawnTimeMinutes == 0) return;

					float growthTime = __instance.m_respawnTimeMinutes * 60;
					DateTime pickedTime = new(__instance.m_nview.GetZDO().GetLong(ZDOVars.s_pickedTime, 0L));
					string timeString = FormatTimeString(growthTime, pickedTime);

					__result = Localization.instance.Localize(__instance.GetHoverName()) + $"\n{timeString}";
				}
			}

			[HarmonyPatch(typeof(Plant), nameof(Plant.GetHoverText))]
			public static void Postfix(Plant __instance, ref string __result)
			{
				if (config.EnablePlantTimers && __instance.m_status == 0 && __instance.m_nview.GetZDO() != null)
				{
					float growthTime = __instance.GetGrowTime();
					DateTime plantTime = new(__instance.m_nview.GetZDO().GetLong(ZDOVars.s_plantTime, ZNet.instance.GetTime().Ticks));
					string timeString = FormatTimeString(growthTime, plantTime);

					__result += $"\n{timeString}";
				}
			}

			public static string FormatTimeString(float growthTime, DateTime placedTime)
			{
				TimeSpan timeSincePlaced = ZNet.instance.GetTime() - placedTime;
				TimeSpan t = TimeSpan.FromSeconds(growthTime - timeSincePlaced.TotalSeconds);

				double remainingMinutes = (growthTime / 60) - timeSincePlaced.TotalMinutes;
				double remainingRatio = remainingMinutes / (growthTime / 60);
				int growthPercentage = Math.Min((int)((timeSincePlaced.TotalSeconds * 100) / growthTime), 100);

				string color = "red";
				if (remainingRatio < 0)
					color = "#00FFFF"; // cyan
				else if (remainingRatio < 0.25)
					color = "#32CD32"; // lime
				else if (remainingRatio < 0.5)
					color = "yellow";
				else if (remainingRatio < 0.75)
					color = "orange";

				string timeRemaining = t.Hours <= 0 ? t.Minutes <= 0 ?
					$"{t.Seconds:D2}s" : $"{t.Minutes:D2}m {t.Seconds:D2}s" : $"{t.Hours:D2}h {t.Minutes:D2}m {t.Seconds:D2}s";

				string formattedString = config.GrowthAsPercentage ?
					$"(<color={color}>{growthPercentage}%</color>)" : remainingMinutes < 0.0 ?
					$"(<color={color}>Ready any second now</color>)" : $"(Ready in <color={color}>{timeRemaining}</color>)";

				return formattedString;
			}
		}

		[HarmonyPatch]
		public static class ShowPickableSpawnerPatches
		{
			[HarmonyPatch(typeof(Pickable), nameof(Pickable.Awake))]
			public static void Postfix(Pickable __instance) => TogglePickedMesh(__instance, __instance.m_picked);

			[HarmonyPatch(typeof(Pickable), nameof(Pickable.SetPicked))]
			public static void Postfix(Pickable __instance, bool picked) => TogglePickedMesh(__instance, picked);

			public static void TogglePickedMesh(Pickable instance, bool picked) => instance.transform.root.Find("PE_Picked")?.gameObject.SetActive(picked);
		}

		//Replace this... possibly with separate config option and hopefully with different entry point and implementation
		//[HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
		//public class PlayerUpdatePlacementGhost
		//{
		//    public static void Postfix(ref GameObject ___m_placementGhost)
		//    {
		//        if (!config.CropRequireCultivation && ___m_placementGhost && ___m_placementGhost.GetComponent<Plant>())
		//        {
		//            ___m_placementGhost.GetComponent<Piece>().m_groundOnly = false;
		//        }
		//    }
		//}
	}
}
