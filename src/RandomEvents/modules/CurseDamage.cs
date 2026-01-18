using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RandomEvents;


public static class Extensions
{
    private static HashSet<CharacterAfflictions.STATUSTYPE> NonCursables = new()
    {
        CharacterAfflictions.STATUSTYPE.Weight,
        CharacterAfflictions.STATUSTYPE.Thorns,
        CharacterAfflictions.STATUSTYPE.Hunger,
        CharacterAfflictions.STATUSTYPE.Curse,
        CharacterAfflictions.STATUSTYPE.Web,
    };
    public static bool isCursable(this CharacterAfflictions.STATUSTYPE t)
    {
        return !NonCursables.Contains(t);
    }
}

[HarmonyPatch(typeof(CharacterAfflictions))]
public class SharedDamagePatch
{
    public static bool enabled = false;
    static float multiplier = 0.05f;

    static float too_much_curse = 0.5f;

    [HarmonyPrefix]
    [HarmonyPatch("SetStatus", typeof(CharacterAfflictions.STATUSTYPE), typeof(float), typeof(bool))]
    public static void SetStatusPrefix(CharacterAfflictions __instance, ref CharacterAfflictions.STATUSTYPE statusType, ref float amount, bool pushStatus)
    {
        if (!enabled) return;
        if (!statusType.isCursable()) return;
        var current = __instance.GetCurrentStatus(statusType);
        if (current > amount)
        {
            // Decrease. Don't touch.
            return;
        }
        float diff = (amount - current) * multiplier;
        var newCurse = __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Curse) + diff;

        if (newCurse > too_much_curse) return;
        statusType = CharacterAfflictions.STATUSTYPE.Curse;
        amount = newCurse;
    }

    [HarmonyPrefix]
    [HarmonyPatch("AddStatus", typeof(CharacterAfflictions.STATUSTYPE), typeof(float), typeof(bool), typeof(bool))]
    public static void AddStatusPrefix(CharacterAfflictions __instance, ref CharacterAfflictions.STATUSTYPE statusType, ref float amount, bool fromRPC, ref bool playEffects)
    {
        if (!enabled) return;
        if (!statusType.isCursable()) return;
        if (amount < 0) return;
        var newCurse = __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Curse) + amount;
        if (newCurse > too_much_curse) return;
        statusType = CharacterAfflictions.STATUSTYPE.Curse;
        amount *= multiplier;
        if (amount < 0.025)
        {
            playEffects = false;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("SubtractStatus", typeof(CharacterAfflictions.STATUSTYPE), typeof(float), typeof(bool), typeof(bool))]
    public static void SubtractStatusPrefix(CharacterAfflictions __instance, ref CharacterAfflictions.STATUSTYPE statusType, ref float amount, bool fromRPC, bool decreasedNaturally)
    {
        if (!enabled) return;
        if (!statusType.isCursable()) return;
        if (amount > 0) return;
        var newCurse = __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Curse) - amount;
        if (newCurse > too_much_curse) return;
        statusType = CharacterAfflictions.STATUSTYPE.Curse;
        amount *= multiplier;
    }
}

public class CurseDamageEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        SharedDamagePatch.enabled = false;
    }

    public void Enable(EventInterface eintf)
    {
        eintf.AddEnableLine(new NiceText
        {
            s = "All damage is reduced by 95%.",
            c = Color.green,
        });
        eintf.AddEnableLine(new NiceText
        {
            s = "All damage is curse damage.",
            c = Color.red,
        });
        SharedDamagePatch.enabled = true;
    }

    public JObject to_json()
    {
        return [];
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = _ => new CurseDamageEvent(),
            FromJson = _ => new CurseDamageEvent(),
        };
    }
}