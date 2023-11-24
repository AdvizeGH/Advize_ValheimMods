using Advize_PlantEverything.Configuration;
using BepInEx.Logging;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using PE = Advize_PlantEverything.PlantEverything;

namespace Advize_PlantEverything.Framework
{
	sealed class PluginHelper
	{
		internal ModConfig config;

		internal PluginHelper(ModConfig config) => this.config = config;

		private readonly Dictionary<string, Texture2D> cachedTextures = new();
		private readonly Dictionary<Texture2D, Sprite> cachedSprites = new();

		internal Piece GetOrAddPieceComponent(GameObject go) => go.GetComponent<Piece>() ?? go.AddComponent<Piece>();

		internal string GetPrefabName(Component c) => c.transform.root.name.Replace("(Clone)", "");

		internal bool IsModdedPrefab(Component c) => c && PE.prefabRefs.ContainsKey(GetPrefabName(c));

		internal LayerMask GetRemovalMask() => LayerMask.GetMask(StaticContent.layersForPieceRemoval);

		//internal void AddExtraResource()
		//{

		//}

		internal Piece CreatePiece(PieceDB pdb)
		{
			Piece piece = GetOrAddPieceComponent(PE.prefabRefs[pdb.key]);

			piece.m_name = pdb.extraResource ? pdb.pieceName : $"$pe{pdb.Name}Name";
			piece.m_description = pdb.extraResource ? pdb.pieceDescription : $"$pe{pdb.Name}Description";
			piece.m_category = Piece.PieceCategory.Misc;
			piece.m_cultivatedGroundOnly = (pdb.key.Contains("berryBush") || pdb.key.Contains("Pickable")) && config.RequireCultivation;
			piece.m_groundOnly = piece.m_groundPiece = pdb.isGrounded ?? !config.PlaceAnywhere;
			piece.m_canBeRemoved = pdb.canBeRemoved ?? true;
			piece.m_targetNonPlayerBuilt = false;
			piece.m_randomTarget = config.EnemiesTargetPieces;

			return piece;
		}

		internal Sprite CreateSprite(string fileName, Rect spriteSection)
		{
			try
			{
				Sprite result;
				Texture2D texture = LoadTexture(fileName);

				if (cachedSprites.ContainsKey(texture))
				{
					result = cachedSprites[texture];
				}
				else
				{
					result = Sprite.Create(texture, spriteSection, Vector2.zero);
					cachedSprites.Add(texture, result);
				}
				return result;
			}
			catch
			{
				PE.Dbgl("Unable to load texture", true, LogLevel.Error);
			}

			return null;
		}

		internal Texture2D LoadTexture(string fileName)
		{
			Texture2D result;

			if (cachedTextures.ContainsKey(fileName))
			{
				result = cachedTextures[fileName];
			}
			else
			{
				Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Advize_{PE.PluginName}.Assets.{fileName}");
				byte[] array = new byte[manifestResourceStream.Length];
				manifestResourceStream.Read(array, 0, array.Length);
				Texture2D texture = new(0, 0);
				ImageConversion.LoadImage(texture, array);
				result = texture;
				cachedTextures.Add(fileName, result);
			}

			return result;
		}
	}
}
