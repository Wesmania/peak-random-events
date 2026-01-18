using System;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Steamworks;
using UnityEngine;

namespace RandomEvents;


[HarmonyPatch(typeof(CharacterAfflictions))]
public class FrostbitePatch
{
    public static FrostbitePatch instance = new();
    public bool enabled = false;
    public CharacterAfflictions.STATUSTYPE kind = CharacterAfflictions.STATUSTYPE.Cold;
    private float lastTick = Time.time;
    static float threshold = 0.5f;

    private float damageChance()
    {
        float now = Time.time;
        float then = lastTick;
        // We could make exponential stuff, but linearly from 0 to 30% over 2 seconds is good enough.
        return Math.Min(now - then, 2f) * 0.15f;
    }

    [HarmonyPrefix]
    [HarmonyPatch("SubtractStatus", typeof(CharacterAfflictions.STATUSTYPE), typeof(float), typeof(bool), typeof(bool))]
    public static void SubtractStatusPrefix(CharacterAfflictions __instance, CharacterAfflictions.STATUSTYPE statusType, float amount, bool fromRPC, bool decreasedNaturally, out float __state)
    {
        if (!instance.enabled)
        {
            __state = -1f;
            return;
        }
        __state = __instance.GetCurrentStatus(instance.kind);
    }

    [HarmonyPostfix]
    [HarmonyPatch("SubtractStatus", typeof(CharacterAfflictions.STATUSTYPE), typeof(float), typeof(bool), typeof(bool))]
    public static void SubtractStatusPostfix(CharacterAfflictions __instance, CharacterAfflictions.STATUSTYPE statusType, float amount, bool fromRPC, bool decreasedNaturally, float __state)
    {
        if (__state == -1f) return;
        var current = __instance.GetCurrentStatus(instance.kind);
        if (current < threshold) return;
        var diff = __state - __instance.GetCurrentStatus(instance.kind);
        if (diff < 0.0249f || diff > 0.0251f) return;
        Plugin.Log.LogInfo($"Chance {instance.damageChance()}");
        if (UnityEngine.Random.Range(0f, 1f) <= instance.damageChance())
        {
            __instance.AddStatus(CharacterAfflictions.STATUSTYPE.Injury, 0.025f, false, false);
        }
        instance.lastTick = Time.time;
    }

    [HarmonyPrefix]
    [HarmonyPatch("AddStatus", typeof(CharacterAfflictions.STATUSTYPE), typeof(float), typeof(bool), typeof(bool))]
    public static void AddStatusPrefix(CharacterAfflictions __instance, ref CharacterAfflictions.STATUSTYPE statusType, ref float amount, bool fromRPC, ref bool playEffects)
    {
        if (!instance.enabled) return;
        if (statusType != instance.kind) return;
        var current = __instance.GetCurrentStatus(instance.kind);
        if (current < threshold && current + amount >= threshold)
        {
            instance.lastTick = Time.time;
        }
    }
}

public enum FrostbiteType
{
    FROSTBITE = 0,
    BURNS = 1,
}

public class FrostbiteEvent : IEvent
{
    private FrostbiteType f;

    FrostbiteEvent(FrostbiteType _f)
    {
        f = _f;
    }
    public HashSet<OurBiome> ZoneLimit()
    {
        return
        [
            OurBiome.Alpine,
            OurBiome.Mesa,
            OurBiome.Caldera,
            OurBiome.Kiln,
        ];
    }
    FrostbiteEvent(OurBiome b)
    {
        f = b switch
        {
            OurBiome.Alpine => FrostbiteType.FROSTBITE,
            _ => FrostbiteType.BURNS,
        };
    }
    private static (String, String, Color) Fstr(FrostbiteType f)
    {
        return f switch
        {
            FrostbiteType.FROSTBITE => ("Cold", "frostbite", Color.cyan),
            _ => ("Heat", "burns", Color.red),
        };
    }

    private static CharacterAfflictions.STATUSTYPE ToStatType(FrostbiteType f)
    {
        switch (f)
        {
            case FrostbiteType.FROSTBITE:
                return CharacterAfflictions.STATUSTYPE.Cold;
            case FrostbiteType.BURNS:
            default:
                return CharacterAfflictions.STATUSTYPE.Hot;
        }
    }

    public void Disable(EventInterface eintf)
    {
        FrostbitePatch.instance.enabled = false;
    }

    public void Enable(EventInterface eintf)
    {
        FrostbitePatch.instance.kind = ToStatType(f);
        FrostbitePatch.instance.enabled = true;
        var (s1, s2, c) = Fstr(f);
        eintf.AddEnableLine(new NiceText
        {
            s = $"{s1} might cause {s2}.",
            c = c,
        });
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = b => new FrostbiteEvent(b),
            FromJson = o =>
            {
                FrostbiteType f = (FrostbiteType)((int?)o.GetValue("f")).GetValueOrDefault((int)SkinColor.RANDOM);
                return new FrostbiteEvent(f);
            }
        };
    }
    public JObject to_json()
    {
        JObject o = [];
        o.Add("f", (int)f);
        return o;
    }
}