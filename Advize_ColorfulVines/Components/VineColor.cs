namespace Advize_ColorfulVines;

using System;
using System.Collections.Generic;
using UnityEngine;
using static StaticMembers;

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
        _cacheIndex = -1;
        _nView = GetComponent<ZNetView>();

        if (!_nView || !_nView.IsValid()) return;

        VineColorCache.Add(this);
        _cacheIndex = VineColorCache.Count - 1;
        CacheRenderers();
        ApplyColor();
    }

    private void CacheRenderers()
    {
        // Is a sapling
        if (gameObject.layer == saplingLayer)
        {
            Array.ForEach(SaplingChildren, s => _vineRenderers.Add(transform.Find(s).GetComponent<MeshRenderer>()));
        }
        // Is a vine
        else
        {
            Array.ForEach(VineChildren, s => _vineRenderers.Add(transform.Find(s).Find("default").GetComponent<MeshRenderer>()));
            _berryRenderers = [.. transform.Find("Berries").GetComponentsInChildren<MeshRenderer>(true)];
        }
    }

    internal void ApplyColor()
    {
        if (!_nView.m_zdo.GetBool(ModdedVineHash)) return; // This early return has a side effect of ensuring I can't color placement ghosts, but it was finicky to do with MaterialMan(ager) and piece highlighting

        if (_vineColorProperty.isEmpty)
            SetColors(GetColorsFromZDO(_nView.m_zdo));

        ApplyVineColor();
        ApplyBerryColor();

        //if (_nView.m_zdo.GetBool(ModdedVineHash))
        //{
        //    if (_vineColorProperty.isEmpty)
        //        SetColors(GetColorsFromZDO(_nView.m_zdo));
        //}

        //ApplyVineColor();
        //ApplyBerryColor();
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
            //MaterialMan.instance.SetValue(gameObject, ShaderProps._Color, _configuredVineColorProperty.GetColor("_Color"));
            for (int i = 0; i < _vineRenderers.Count; i++)
            {
                _vineRenderers[i].SetPropertyBlock(_configuredVineColorProperty, i / 3);
                //    /* Begin longwinded explanation comment because I looked back at this and had to re-research why I chose i / 3. */

                //    // If this component is attached to a sapling, _vineRenderers has a count of 2
                //    // 0 / 3 and 1 / 3 both equal the targeted materialIndex of 0 for those mesh renderers

                //    // If this component is attached to a vine, _vineRenderers has a count of 5
                //    // The targeted materialIndex on the first 3 renderers is 0 while the targeted materialIndex is 1 on the final 2

                //    // No idea why IG ordered the materials this way on these last two children, but i / 3 coincidentally works for all situations at present
            }
        }
        else
        {
            //MaterialMan.instance.SetValue(gameObject, ShaderProps._Color, _vineColorProperty.GetColor("_Color"));
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
        if (_cacheIndex >= 0 && _cacheIndex < VineColorCache.Count)
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

    internal void CopyZDOs(ZDO source) => SetColorZDOs(GetColorsFromZDO(source));

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
