using UnityEngine;

namespace Advize_PlantEverything
{
  public static class Extensions
  {
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
      T component = go.GetComponent<T>();
      if (component == null)
        component = go.AddComponent<T>();
      return component;
    }
  }
}
