using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace RandomEvents;
public abstract class ItemBan: IDisposable
{
    public int id;
    public abstract bool BanPrimary(Item i, String name);
    public abstract bool BanSecondary(Item i, String name);

    public void Dispose()
    {
        AllBans.UnregisterBan(this);
    }
}

public static class AllBans
{
    private static int gid = 0;
    private static Dictionary<int, WeakReference<ItemBan>> bans = [];
    public static void RegisterBan(ItemBan ban)
    {
        lock (bans)
        {
            ban.id = gid;
            bans.Add(gid, new WeakReference<ItemBan>(ban));
            gid += 1;
        }
    }
    public static void UnregisterBan(ItemBan ban)
    {
        bans.Remove(ban.id);
    }
    public static bool ForAnyBans(Func<ItemBan, bool> f)
    {
        return bans.Any(v =>
        {
            var r = v.Value.TryGetTarget(out ItemBan tgt);
            if (!r) return false;
            return f(tgt);
        });
    }

    public static string stripName(string s)
    {
        var i = s.IndexOf("(");
        if (i != -1)
        {
            s = s[..i];
        }
        return s;
    }
}

[HarmonyPatch(typeof(Item), "CanUsePrimary")]
class BanItemsMe
{
    private static bool Prefix(Item __instance, ref bool __result)
    {
        // Names of cloned object have "(Clone)" appended.
        var name = AllBans.stripName(__instance.name);

        if (AllBans.ForAnyBans(v => v.BanPrimary(__instance, name))) {
            __result = false;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(Item), "CanUseSecondary")]
class BanItemsOthers
{
    private static bool Prefix(Item __instance, ref bool __result)
    {
        // Names of cloned object have "(Clone)" appended.
        var name = AllBans.stripName(__instance.name);

        if (AllBans.ForAnyBans(v => v.BanSecondary(__instance, name)))
        {
            __result = false;
            return false;
        }
        return true;
    }
}