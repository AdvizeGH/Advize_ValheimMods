namespace Advize_PlantEverything;

using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using System.Reflection;

public static class SeasonsCompatibility
{
    private static bool _isInitialized;
    private static bool _isReady;

    private const string GUID = "shudnal.Seasons";
    private static Assembly _assembly;
    private static FieldInfo _seasonState;
    private static MethodInfo _getSecondsToRespawnPickable;
    private static MethodInfo _getSecondsToGrowPlant;

    public static bool IsReady
    {
        get
        {
            if (!_isInitialized)
                Initialize();

            return _isReady;
        }
    }

    public static double GetSecondsToRespawnPickable(Pickable pickable) => (double)_getSecondsToRespawnPickable.Invoke(_seasonState.GetValue(_seasonState), [pickable]);

    public static double GetSecondsToGrowPlant(Plant plant) => (double)_getSecondsToGrowPlant.Invoke(_seasonState.GetValue(_seasonState), [plant]);

    private static void Initialize()
    {
        _isInitialized = true;

        if (Chainloader.PluginInfos.TryGetValue(GUID, out PluginInfo seasons))
        {
            _assembly = Assembly.GetAssembly(seasons.Instance.GetType());
            if (_assembly != null)
            {
                _seasonState = AccessTools.Field(_assembly.GetType("Seasons.Seasons"), "seasonState");
                _getSecondsToRespawnPickable = AccessTools.Method(_seasonState.FieldType, "GetSecondsToRespawnPickable");
                _getSecondsToGrowPlant = AccessTools.Method(_seasonState.FieldType, "GetSecondsToGrowPlant");
            }
        }

        _isReady = _seasonState != null && _getSecondsToRespawnPickable != null && _getSecondsToGrowPlant != null;
    }
}
