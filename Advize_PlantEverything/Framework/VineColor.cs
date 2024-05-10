using Advize_PlantEverything;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Advize_PlantEverything.Framework.StaticContent;

public sealed class VineColor : MonoBehaviour
{
	// Static fields
	internal static readonly List<VineColor> VineColorCache = new();
	internal static MaterialPropertyBlock _configuredVineColorProperty = new();
	internal static List<MaterialPropertyBlock> _configuredBerryColorProperties = new() { new(), new(), new()};
	private static readonly string[] VineChildren = { "VineFull", "VineTop", "VineBottom", "VineRight", "VineLeft" };
	private static readonly string[] SaplingChildren = { "healthy", "healthy_grown"};
	private static readonly int saplingLayer = LayerMask.NameToLayer("piece_nonsolid");

	// Instance fields
	private ZNetView _nView;
	internal ZNetView NView { get { return _nView; } }
	private int _cacheIndex;
	private readonly List<MeshRenderer> _vineRenderers = new();
	private List<MeshRenderer> _berryRenderers = new();
	private readonly MaterialPropertyBlock _vineColorProperty = new();
	private readonly List<MaterialPropertyBlock> _berryColorProperties = new() { new(), new(), new() };

	//This is for updating existing vines
	internal static void UpdateColors(bool overrideVines = false, bool overrideBerries = false)
	{
		_configuredVineColorProperty.SetColor("_Color", VineColorFromConfig);

		for (int i = 0; i < 3; i++)
		{
			_configuredBerryColorProperties[i].SetColor("_Color", BerryColorsFromConfig[i]);
		}

		VineColorCache.ForEach(vc => vc.ApplyColor(overrideVines, overrideBerries));
	}

	internal void Awake()
	{
		//PlantEverything.Dbgl("Awake called");
		_nView = GetComponent<ZNetView>();

		if (!_nView || !_nView.IsValid()) return;

		_cacheIndex = VineColorCache.Count;
		//PlantEverything.Dbgl(_cacheIndex.ToString());
		VineColorCache.Add(this);

		CacheRenderers();
		ApplyColor(fromAwake: true);
	}

	internal void CacheRenderers()
	{
		if (gameObject.layer == saplingLayer)
		{
			//PlantEverything.Dbgl("Sapling: true");
			Array.ForEach(SaplingChildren, s => _vineRenderers.Add(transform.Find(s).GetComponent<MeshRenderer>()));
		}
		else
		{
			Array.ForEach(VineChildren, s => _vineRenderers.Add(transform.Find(s).Find("default").GetComponent<MeshRenderer>()));
			_berryRenderers = transform.Find("Berries").GetComponentsInChildren<MeshRenderer>(true).ToList();
		}
	}

	internal void ApplyColor(bool overrideVines = false, bool overrideBerries = false, bool fromAwake = false)
	{
		if (!fromAwake && _nView != null && !_nView.GetZDO().GetBool(ModdedVineHash))
		{
			_vineRenderers.Clear();
			_berryRenderers.Clear();
			return;
		}

		if (_nView != null && _nView.GetZDO().GetBool(ModdedVineHash))
		{
			if (_vineColorProperty.isEmpty)
			{
				SetColors(GetColorsFromZDO(_nView.GetZDO()));
			}

			ApplyVineColor(overrideVines);
			ApplyBerryColor(overrideBerries);
		}
	}

	internal void SetColors(List<Color> colors)
	{
		_vineColorProperty.SetColor("_Color", colors[0]);
		_berryColorProperties[0].SetColor("_Color", colors[1]);
		_berryColorProperties[1].SetColor("_Color", colors[2]);
		_berryColorProperties[2].SetColor("_Color", colors[3]);
	}

	internal void ApplyVineColor(bool overrideVines = false)
	{
		if ((overrideVines && !_vineColorProperty.isEmpty) || _vineColorProperty.isEmpty)
		{
			for (int i = 0; i < _vineRenderers.Count; i++)
			{
				_vineRenderers[i].SetPropertyBlock(_configuredVineColorProperty, i / 3);
			}
		}
		else
		{
			for (int i = 0; i < _vineRenderers.Count; i++)
			{
				_vineRenderers[i].SetPropertyBlock(_vineColorProperty, i / 3);
			}
		}
	}

	private void ApplyBerryColor(bool overrideBerries = false)
	{
		if ((overrideBerries && !_vineColorProperty.isEmpty) || _vineColorProperty.isEmpty)
		{
			for (int i = 0; i < _berryRenderers.Count; i++)
			{
				_berryRenderers[i].SetPropertyBlock(_configuredBerryColorProperties[i], 0);
			}
		}
		else
		{
			for (int i = 0; i < _berryRenderers.Count; i++)
			{
				_berryRenderers[i].SetPropertyBlock(_berryColorProperties[i], 0);
			}
		}
	}

	internal void OnDestroy()
	{
		if (_cacheIndex > 0 && _cacheIndex < VineColorCache.Count)
		{
			VineColorCache[_cacheIndex] = VineColorCache[VineColorCache.Count - 1];
			VineColorCache[_cacheIndex]._cacheIndex = _cacheIndex;
			VineColorCache.RemoveAt(VineColorCache.Count - 1);
		}

		_vineRenderers.Clear();
		_berryRenderers.Clear();
	}

	internal List<Color> GetColorsFromZDO(ZDO zdo)
	{
		zdo.GetVec3(VineColorHash, out var color1);
		zdo.GetVec3(BerryColor1Hash, out var color2);
		zdo.GetVec3(BerryColor2Hash, out var color3);
		zdo.GetVec3(BerryColor3Hash, out var color4);

		return new() { Vector3ToColor(color1), Vector3ToColor(color2), Vector3ToColor(color3), Vector3ToColor(color4) };
	}

	internal void CopyZDOs(ZDO source)
	{
		SetColorZDOs(GetColorsFromZDO(source));
	}

	internal void SetColorZDOs(List<Color> colors)
	{
		ZDO zdo = _nView.GetZDO();
		zdo.Set(ModdedVineHash, true);
		zdo.Set(VineColorHash, ColorToVector3(colors[0]));
		zdo.Set(BerryColor1Hash, ColorToVector3(colors[1]));
		zdo.Set(BerryColor2Hash, ColorToVector3(colors[2]));
		zdo.Set(BerryColor3Hash, ColorToVector3(colors[3]));
	}
}
