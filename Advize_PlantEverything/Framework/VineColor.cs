namespace Advize_PlantEverything;

using System;
using System.Collections.Generic;
using UnityEngine;
using static StaticContent;

public sealed class VineColor : MonoBehaviour
{
    // Static fields
    private static readonly List<VineColor> VineColorCache = [];
    private readonly static MaterialPropertyBlock _configuredVineColorProperty = new();
    private readonly static List<MaterialPropertyBlock> _configuredBerryColorProperties = [new(), new(), new()];
    private static readonly string[] VineChildren = ["VineFull", "VineTop", "VineBottom", "VineRight", "VineLeft"];
    private static readonly string[] SaplingChildren = ["healthy", "healthy_grown"];
    private static readonly int saplingLayer = LayerMask.NameToLayer("piece_nonsolid");

    // Instance fields
    private ZNetView _nView;
    private int _cacheIndex;
    private readonly List<MeshRenderer> _vineRenderers = [];
    private List<MeshRenderer> _berryRenderers = [];
    private MaterialPropertyBlock _vineColorProperty;
    private List<MaterialPropertyBlock> _berryColorProperties;

    //This is for updating existing vines
    internal static void UpdateColors()
    {
        _configuredVineColorProperty.SetColor("_Color", VineColorFromConfig);

        for (int i = 0; i < 3; i++)
        {
            _configuredBerryColorProperties[i].SetColor("_Color", BerryColorsFromConfig[i]);
        }

        VineColorCache.ForEach(vc => vc.ApplyColor());
    }

    internal void Awake()
    {
        _vineColorProperty = new();
        _berryColorProperties = [new(), new(), new()];
        _nView = GetComponent<ZNetView>();

        if (!_nView || !_nView.IsValid()) return;

        _cacheIndex = VineColorCache.Count;
        VineColorCache.Add(this);
        CacheRenderers();
        ApplyColor(fromAwake: true);
    }

    private void CacheRenderers()
    {
        if (gameObject.layer == saplingLayer)
        {
            //PlantEverything.Dbgl("Sapling: true");
            Array.ForEach(SaplingChildren, s => _vineRenderers.Add(transform.Find(s).GetComponent<MeshRenderer>()));
        }
        else
        {
            Array.ForEach(VineChildren, s => _vineRenderers.Add(transform.Find(s).Find("default").GetComponent<MeshRenderer>()));
            _berryRenderers = [.. transform.Find("Berries").GetComponentsInChildren<MeshRenderer>(true)];
        }
    }

    internal void ApplyColor(bool fromAwake = false)
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

            ApplyVineColor();
            ApplyBerryColor();
        }
    }

    private void SetColors(List<Color> colors)
    {
        _vineColorProperty.SetColor("_Color", colors[0]);
        _berryColorProperties[0].SetColor("_Color", colors[1]);
        _berryColorProperties[1].SetColor("_Color", colors[2]);
        _berryColorProperties[2].SetColor("_Color", colors[3]);
    }

    private void ApplyVineColor()
    {
        if ((OverrideVines && !_vineColorProperty.isEmpty) || _vineColorProperty.isEmpty)
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

    private void ApplyBerryColor()
    {
        if ((OverrideBerries && !_vineColorProperty.isEmpty) || _vineColorProperty.isEmpty)
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

    private List<Color> GetColorsFromZDO(ZDO zdo)
    {
        zdo.GetVec3(VineColorHash, out Vector3 color1);
        zdo.GetVec3(BerryColor1Hash, out Vector3 color2);
        zdo.GetVec3(BerryColor2Hash, out Vector3 color3);
        zdo.GetVec3(BerryColor3Hash, out Vector3 color4);

        return [Vector3ToColor(color1), Vector3ToColor(color2), Vector3ToColor(color3), Vector3ToColor(color4)];
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
