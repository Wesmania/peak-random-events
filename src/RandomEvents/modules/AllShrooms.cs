using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RandomEvents;

public class AllShroomsGlobals
{
    public static bool enabled = false;
    public static bool in_check = false;

    private static List<string> prefab_names = [
        "Shroomberry_Red",
        "Shroomberry_Yellow",
        "Shroomberry_Green",
        "Shroomberry_Blue",
        "Shroomberry_Purple",
    ];

    private static List<GameObject>? shroom_objs = null;
    private static List<GameObject> get_shroom_objs()
    {
        shroom_objs ??= prefab_names.Select(name =>
            {
                var o = new GameObject();
                // All users only care about the name.
                o.name = name;
                return o;
            }).ToList();
        return shroom_objs;
    }

    public static List<GameObject> getShrooms(int count, bool canRepeat)
    {
        var so = get_shroom_objs();

        List<GameObject> list = [.. so];
        List<GameObject> list2 = [];
        for (int i = 0; i < count; i++)
        {
            GameObject spawnEntry = list.RandomSelection(_ => 1);
            list2.Add(spawnEntry);
            if (!canRepeat)
            {
                if (list.Count <= 1)
                {
                    list = [.. so];
                }

                list.Remove(spawnEntry);
            }
        }
        return list2;
    }

}

[HarmonyPatch(typeof(SpawnList), "GetSpawns")]
public class AllShroomSpawnPatch
{
    private static bool Prefix(SpawnList __instance, int count, bool canRepeat, ref List<GameObject> __result)
    {
        if (AllShroomsGlobals.enabled && AllShroomsGlobals.in_check)
        {
            __result = AllShroomsGlobals.getShrooms(count, canRepeat);
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(LootData), "GetRandomItem")]
public class AllShroomsLootPatch
{
    public static bool Prefix(SpawnPool spawnPool, ref GameObject __result)
    {
        if (AllShroomsGlobals.enabled && AllShroomsGlobals.in_check)
        {
            var shrooms = AllShroomsGlobals.getShrooms(1, true);
            __result = shrooms[0];
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(GroundPlaceSpawner), "SpawnItems")]
public class AllShroomsGroundHook
{
    private static void Prefix(GroundPlaceSpawner __instance, List<Transform> spawnSpots)
    {
        AllShroomsGlobals.in_check = true;
    }
    private static void Postfix(GroundPlaceSpawner __instance, List<Transform> spawnSpots)
    {
        AllShroomsGlobals.in_check = false;
    }
}

[HarmonyPatch(typeof(BerryBush), "SpawnItems")]
public class AllShroomsBerryBushHook
{
    private static void Prefix(BerryBush __instance, List<Transform> spawnSpots)
    {
        AllShroomsGlobals.in_check = true;
    }
    private static void Postfix(BerryBush __instance, List<Transform> spawnSpots)
    {
        AllShroomsGlobals.in_check = false;
    }
}

[HarmonyPatch(typeof(BerryVine), "SpawnItems")]
public class AllShroomsBerryVineHook
{
    private static void Prefix(BerryVine __instance, List<Transform> spawnSpots)
    {
        AllShroomsGlobals.in_check = true;
    }
    private static void Postfix(BerryVine __instance, List<Transform> spawnSpots)
    {
        AllShroomsGlobals.in_check = false;
    }
}
public class AllShroomsEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        AllShroomsGlobals.enabled = false;
    }

    public void Enable(EventInterface eintf)
    {
        eintf.AddEnableLine("All natural food is shroomberries.");
        AllShroomsGlobals.enabled = true;
    }

    public JObject to_json()
    {
        return [];
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = _ => new AllShroomsEvent(),
            FromJson = _ => new AllShroomsEvent(),
        };
    }

    public HashSet<OurBiome> ZoneLimit()
    {
        return [
            OurBiome.Shore,
            OurBiome.Tropics,
            OurBiome.Roots,
            OurBiome.Alpine,
            OurBiome.Mesa,
        ];
    }
}