namespace Advize_StumpsRegrow;

using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static StumpsRegrow;

public sealed class StumpGrower : SlowUpdate, Hoverable
{
    //Instance fields
    private ZNetView _nView;
    private float _awakeTime;
    private float _updateTime;
    private DateTime _plantedTime;

    //Properties
    private double TimeSincePlanted => (ZNet.instance.GetTime() - _plantedTime).TotalSeconds;

    public override void Awake()
    {
        base.Awake();
        _nView = GetComponent<ZNetView>();

        if (!_nView || !_nView.IsValid()) return;

        long plantTime = _nView.GetZDO().GetLong(ZDOVars.s_plantTime, 0L);
        long currentTicks = ZNet.instance.GetTime().Ticks;
        bool unknownPlantTime = plantTime == 0L;

        if (_nView.IsOwner() && unknownPlantTime)
        {
            _nView.GetZDO().Set(ZDOVars.s_plantTime, currentTicks);
        }

        _plantedTime = new(unknownPlantTime ? currentTicks : plantTime);
        _awakeTime = Time.time;
    }

    public void Start()
    {
        _updateTime = Time.time + 10f;
    }

    public override void SUpdate(float time, Vector2i referenceZone)
    {
        if (_nView.IsValid() && time > _updateTime)
        {
            _updateTime = time + 10f;

            // If conditions are right to regrow the stump...
            if (_nView.IsOwner() && time - _awakeTime > 10f && TimeSincePlanted > (double)config.StumpGrowthTime)
            {
                //Regrow stump into tree
                RegrowStump();
                //Remove the old stump
                StartCoroutine("RemoveStump");
            }
        }
    }

    private void RegrowStump()
    {
        GameObject spawnedTree = Instantiate(GetTreePrefab(), transform.root.position, transform.root.rotation);
        spawnedTree.GetComponent<TreeBase>().Grow();
        spawnedTree.GetComponent<ZNetView>().SetLocalScale(transform.localScale);
    }

    private GameObject GetTreePrefab()
    {
        string treeBasePrefabName = _nView.GetZDO().GetString(HashedZDOName);

        // If name of tree that spawned the stump is not stored in zdo
        if (treeBasePrefabName.IsNullOrWhiteSpace())
        {
            //Get list of tree prefabs known to spawn this stump and select one at random
            List<GameObject> potentialTrees = TreesPerStump[Utils.GetPrefabName(name)];
            return potentialTrees[UnityEngine.Random.Range(0, potentialTrees.Count)];
        }

        return ZNetScene.instance.GetPrefab(treeBasePrefabName);
    }

    internal IEnumerator RemoveStump()
    {
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            yield return null;
        }

        _nView.Destroy();
    }

    public string GetHoverName()
    {
        return Localization.instance.Localize("$prop_treestump");
    }

    public string GetHoverText()
    {
        if (config.EnableStumpTimers && _nView.GetZDO() != null)
        {
            return $"{GetHoverName()}\n{FormatTimeString(config.StumpGrowthTime)}";
        }

        return GetHoverName();
    }

    private string FormatTimeString(float growthTime)
    {
        TimeSpan t = TimeSpan.FromSeconds(growthTime - TimeSincePlanted);

        double remainingMinutes = (growthTime / 60) - (ZNet.instance.GetTime() - _plantedTime).TotalMinutes;
        double remainingRatio = remainingMinutes / (growthTime / 60);
        int growthPercentage = Math.Min((int)((TimeSincePlanted * 100) / growthTime), 100);

        string color = "red";
        if (remainingRatio < 0) color = "#00FFFF"; // cyan
        else if (remainingRatio < 0.25) color = "#32CD32"; // lime
        else if (remainingRatio < 0.5) color = "yellow";
        else if (remainingRatio < 0.75) color = "orange";

        string timeRemaining = t.Hours <= 0 ? t.Minutes <= 0 ?
            $"{t.Seconds:D2}s" : $"{t.Minutes:D2}m {t.Seconds:D2}s" : $"{t.Hours:D2}h {t.Minutes:D2}m {t.Seconds:D2}s";

        string formattedString = config.GrowthAsPercentage ?
            $"(<color={color}>{growthPercentage}%</color>)" : remainingMinutes < 0.0 ?
            $"(<color={color}>About tree fitty</color>)" : $"(Ready in <color={color}>{timeRemaining}</color>)";

        return formattedString;
    }
}
