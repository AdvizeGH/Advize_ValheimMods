namespace Advize_Armoire;

using System.Collections.Generic;

public static class HashCache
{
    private static readonly Dictionary<string, int> Cache = [];

    public static int Get(string name)
    {
        if (!Cache.TryGetValue(name, out int hash))
        {
            hash = name.GetStableHashCode();
            Cache[name] = hash;
        }

        return hash;
    }
}
