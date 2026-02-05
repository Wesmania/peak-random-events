using System;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using UnityEngine;

namespace RandomEvents;

// TODO:
// Figure out how to get a bigger mushroom bounce
// Figure out non-global cannon modifier

public class RescaleData
{
    public bool scale_after_creation;
    public float scale_multi;
    public float? weight_multi = null;
    public Action<Item>? enable = null;
    public Action<Item>? disable = null;
}

public class HeavyDutyData
{
    public static bool enabled = false;
    public static Dictionary<string, RescaleData> RescaleItems = new(){
        { "Anti-Rope Spool", new RescaleData {
            scale_after_creation = true,
            scale_multi = 1.5f,
            weight_multi = 1.5f,
            enable = i =>
            {
                var s = i.GetComponent<RopeSpool>();
                s.RopeFuel *= 2;
                s.ropeStartFuel *= 2;
            },
            disable = i =>
            {
                var s = i.GetComponent<RopeSpool>();
                s.RopeFuel /= 2;
                s.ropeStartFuel /= 2;
            }
        } },
        { "BounceShroom", new RescaleData {
            scale_after_creation = true,
            scale_multi = 2.0f,
            weight_multi = 1.5f,
        } },
        { "BounceShroomSpawn", new RescaleData {    // TODO how to get a bigger bounce?
            scale_after_creation = false,
            scale_multi = 2.0f,
        } },
        { "ChainShooter", new RescaleData {
            scale_after_creation = true,
            scale_multi = 2.0f,
            weight_multi = 2.0f,
            enable = i =>
            {
                var s = i.GetComponent<VineShooter>();
                s.maxLength *= 2;
            },
            disable = i =>
            {
                var s = i.GetComponent<VineShooter>();
                s.maxLength /= 2;
            }
        } },
        { "CloudFungus", new RescaleData {
            scale_after_creation = true,
            scale_multi = 2.0f,
            weight_multi = 2.0f,
        } },
        { "CloudFungusPlaced", new RescaleData {
            scale_after_creation = false,
            scale_multi = 2.5f,
        } },
        { "MagicBean", new RescaleData {
            scale_after_creation = true,
            scale_multi = 3.0f,
            weight_multi = 2.5f,
            enable = i =>
            {
                var s = i.GetComponent<MagicBean>();
                s.plantPrefab.maxLength = 40f;
                s.plantPrefab.maxWidth = 3f;
            },
            disable = i =>
            {
                var s = i.GetComponent<MagicBean>();
                s.plantPrefab.maxLength = 20f;
                s.plantPrefab.maxWidth = 1.5f;
            }
        } },
        { "RopeShooter", new RescaleData {
            scale_after_creation = true,
            scale_multi = 2.0f,
            weight_multi = 2.5f,
            enable = i =>
            {
                var s = i.GetComponent<RopeShooter>();
                s.length *= 2;
                s.maxLength *= 2;
            },
            disable = i =>
            {
                var s = i.GetComponent<RopeShooter>();
                s.length /= 2;
                s.maxLength /= 2;
            }
        } },
        { "RopeAnchorForRopeShooter", new RescaleData {
            scale_after_creation = false,
            scale_multi = 2.0f,
        } },
        { "RopeShooterAnti", new RescaleData {
            scale_after_creation = true,
            scale_multi = 2.0f,
            weight_multi = 2.5f,
            enable = i =>
            {
                var s = i.GetComponent<RopeShooter>();
                s.length *= 2;
                s.maxLength *= 2;
            },
            disable = i =>
            {
                var s = i.GetComponent<RopeShooter>();
                s.length /= 2;
                s.maxLength /= 2;
            }
        } },
        { "RopeAnchorForRopeShooterAnti", new RescaleData {
            scale_after_creation = false,
            scale_multi = 2.0f,
        } },
        { "RopeSpool", new RescaleData {
            scale_after_creation = true,
            scale_multi = 1.5f,
            weight_multi = 1.5f,
            enable = i =>
            {
                var s = i.GetComponent<RopeSpool>();
                s.RopeFuel *= 2;
                s.ropeStartFuel *= 2;
            },
            disable = i =>
            {
                var s = i.GetComponent<RopeSpool>();
                s.RopeFuel /= 2;
                s.ropeStartFuel /= 2;
            }
        } },
        { "ScoutCannonItem", new RescaleData {
            scale_after_creation = true,
            scale_multi = 1.5f,
            weight_multi = 1.5f,
        } },
        { "ScoutCannon_Placed", new RescaleData {       // TODO ghost
            scale_after_creation = true,
            scale_multi = 1.5f,
        } },
        { "ShelfShroom", new RescaleData {
            scale_after_creation = true,
            scale_multi = 1.5f,
            weight_multi = 2.0f,
        } },
        { "ShelfShroomSpawn", new RescaleData {
            scale_after_creation = false,
            scale_multi = 3.0f,
        } },
    };

    public static RescaleData? GetScale(Item i)
    {
        var name = AllBans.stripName(i.name);
        if (!RescaleItems.ContainsKey(name)) return null;
        return RescaleItems[name];
    }
    public static RescaleData? GetScale(GameObject i)
    {
        var name = AllBans.stripName(i.name);
        if (!RescaleItems.ContainsKey(name)) return null;
        return RescaleItems[name];
    }
    public static void enable()
    {
        Item[] items = UnityEngine.Object.FindObjectsByType<Item>(FindObjectsSortMode.None);
        Item[] i2 = items;
        foreach (Item i in i2)
        {
            var name = AllBans.stripName(i.name);

            var v = GetScale(i);
            if (v == null) continue;

            if (v.scale_after_creation)
            {
                i.transform.localScale *= v.scale_multi;
            }
            v.enable?.Invoke(i);
        }
    }
    public static void disable()
    {
        Item[] items = UnityEngine.Object.FindObjectsByType<Item>(FindObjectsSortMode.None);
        Item[] i2 = items;
        foreach (Item i in i2)
        {
            var name = AllBans.stripName(i.name);

            var v = GetScale(i);
            if (v == null) continue;

            if (v.scale_after_creation)
            {
                i.transform.localScale /= v.scale_multi;
            }
            v.disable?.Invoke(i);
        }
    }
}

[HarmonyPatch(MethodType.Getter)]
[HarmonyPatch(typeof(Item), "CarryWeight")]
public static class HeavierItems
{
    private static void Postfix(Item __instance, ref int __result)
    {
        if (!HeavyDutyData.enabled) return;
        var d = HeavyDutyData.GetScale(__instance);
        if (d == null || !d.weight_multi.HasValue) return;
        __result = (int) Math.Floor(__result * d.weight_multi.Value);
    }
}

[HarmonyPatch(MethodType.Getter)]
[HarmonyPatch(typeof(Rope), "MaxSegments")]
public static class LongerRopeLimit
{
    private static bool Prefix(Rope __instance, ref int __result)
    {
        if (!HeavyDutyData.enabled) return true;
        __result = 60;
        return false;
    }
}

[HarmonyPatch(typeof(RopeBoneVisualizer), "LateUpdate")]
public static class LongRopeFix
{
    public static void Prefix(RopeBoneVisualizer __instance)
    {
        bool rope_is_long = __instance.rope.Segments > 40;
        bool bones_are_duplicated = __instance.bones.Count > 50;
        if (rope_is_long && !bones_are_duplicated)
        {
            List<Transform> db = [];
            foreach (var v in __instance.bones)
            {
                db.Add(v);
                db.Add(v);
            }
            __instance.bones = db;
        }
        if (!rope_is_long && bones_are_duplicated)
        {
            List<Transform> db = [];
            for (int i = 0; i < __instance.bones.Count; i += 2)
            {
                db.Add(__instance.bones[i]);
            }
            __instance.bones = db;
        }
    }
}

[HarmonyPatch(typeof(PhotonNetwork), "NetworkInstantiate", [typeof(Photon.Pun.InstantiateParameters), typeof(bool), typeof(bool)])]
public static class HeavyDutyMaker
{
    public static void Postfix(Photon.Pun.InstantiateParameters parameters, bool roomObject, bool instantiateEvent, ref GameObject __result)
    {
        if (!HeavyDutyData.enabled) return;
        var _parameters = parameters;

        var v = HeavyDutyData.GetScale(__result);
        if (v == null) return;
        __result.transform.localScale *= v.scale_multi;
        v.enable?.Invoke(__result.GetComponent<Item>());
    }
}

[HarmonyPatch(typeof(Item), "SetState")]
public static class HandScaleFix
{
    private static void Postfix(Item __instance, ItemState setState, Character character)
    {
        if (__instance.forceScale && HeavyDutyData.enabled)
        {
            var v = HeavyDutyData.GetScale(__instance);
            if (v == null) return;
            __instance.transform.localScale *= v.scale_multi;
        }
    }
}

[HarmonyPatch(typeof(ScoutCannon), "FixedUpdate")]
public static class ScoutCannonHack
{
    // FIXME no idea how to identify which scout cannons are the large ones. Good enough...
    private static void Prefix(ScoutCannon __instance)
    {
        if (HeavyDutyData.enabled)
        {
            __instance.launchForce = 3500f;
        }
        else
        {
            __instance.launchForce = 2000f;
        }
    }
}
public class HeavyDutyEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        HeavyDutyData.disable();
        HeavyDutyData.enabled = false;
        Character.localCharacter.refs.afflictions.UpdateWeight();
    }

    public void Enable(EventInterface eintf)
    {
        HeavyDutyData.enable();
        HeavyDutyData.enabled = true;
        Character.localCharacter.refs.afflictions.UpdateWeight();

        eintf.AddEnableLine(new NiceText
        {
            s = "Climbing gear is heavy-duty.",
            c = Color.green
        });
        eintf.AddEnableLine(new NiceText
        {
            s = "Climbing gear is heavy.",
            c = Color.red
        });
    }
    public JObject to_json()
    {
        return [];
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = _ => new HeavyDutyEvent(),
            FromJson = _ => new HeavyDutyEvent(),
        };
    }

    public HashSet<AllEvents> ExcludeEvents()
    {
        return [AllEvents.MEGABOOST];
    }
}