using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RandomEvents;

public static class MegaboostData
{
    public static bool enabled = false;
    private static readonly ushort rope_id = 65;
    private static readonly string rope_name = "RopeSpool";

    public static void GiveEveryoneRope()
    {
        foreach (var player in Character.AllCharacters)
        {
            var p = player.player;
            ItemInstanceData d = new(Guid.NewGuid());
            // Double the normal length
            var k = d.RegisterNewEntry<FloatItemData>(DataEntryKey.Fuel);
            k.Value = 120f;

            GiveItem.Do(player, rope_id, rope_name, d);
        }
    }
}

[HarmonyPatch(typeof(CharacterMovement), "JumpRpc")]
class MegaboostJumpPatch
{
    private static IEnumerator resetJump(CharacterMovement __instance, float __state)
    {
        yield return new WaitForSeconds(0.5f);
        __instance.jumpImpulse = __state;
    }
    private static void Prefix(CharacterMovement __instance, bool isPalJump, out float __state)
    {
        __state = __instance.jumpImpulse;
        if (!MegaboostData.enabled) return;

        if (isPalJump)
        {
            if (__instance.character.data.GetTargetRagdollControll() < 0.9f)
            {
                __instance.jumpImpulse *= 1.5f;
            }
            else
            {
                __instance.jumpImpulse *= 3;
            }
            __instance.StartCoroutine(resetJump(__instance, __state));
        }
    }
}

class MegaboostRopeOnly : ItemBan
{
    private static HashSet<string> BannedItems = [
        "BounceShroom",
        "ChainShooter",
        "ClimbingSpike",
        "MagicBean",
        "RescueHook",
        "RopeShooter",
        "RopeShooterAnti",
        "ScoutCannonItem",
        "ShelfShroom",
    ];
    public static MegaboostRopeOnly? instance = null;
    public override bool BanPrimary(Item i, string name)
    {
        return BannedItems.Contains(name);
    }

    public override bool BanSecondary(Item i, string name)
    {
        return BannedItems.Contains(name);
    }
}

public class MegaboostEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        MegaboostData.enabled = false;
        MegaboostRopeOnly.instance?.Dispose();
        MegaboostRopeOnly.instance = null;
    }

    public void Enable(EventInterface eintf)
    {
        eintf.AddEnableLine("You can boost scouts much higher.");
        eintf.AddEnableLine("Free rope.");
        eintf.AddEnableLine(new NiceText
        {
            c = Color.red,
            s = "No other climbing tools allowed."
        });
        MegaboostData.enabled = true;
        MegaboostRopeOnly.instance = new();
        AllBans.RegisterBan(MegaboostRopeOnly.instance);
    }
    public void LateEnable(EventInterface eintf)
    {
        MegaboostData.GiveEveryoneRope();
    }

    public JObject to_json()
    {
        return [];
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = _ => new MegaboostEvent(),
            FromJson = _ => new MegaboostEvent(),
        };
    }
    public HashSet<AllEvents> ExcludeEvents()
    {
        return [AllEvents.SUPER_RESCUE_HOOKS];
    }
}
