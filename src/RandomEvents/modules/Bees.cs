using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using UnityEngine;
using Zorro.Core;

namespace RandomEvents;

class BeeSwarmData
{
    public static bool enabled = false;

    private static HashSet<string> AllBugles = [
        "Bugle",
        "Bugle_Magic",
        "Bugle_Scoutmaster Variant",
    ];
    public static readonly string SwarmName = "BeeSwarm";
    public static GameObject MakeSuperBees(Character c)
    {
        return PhotonNetwork.Instantiate(SwarmName, c.Center + 15.0f * Vector3.up, Quaternion.identity, 0, [(object)"Random Events"]);
    }

    public static void DestroyBees()
    {
        BeeSwarm[] items = UnityEngine.Object.FindObjectsByType<BeeSwarm>(FindObjectsSortMode.None);
        BeeSwarm[] i2 = items;
        foreach (var i in i2)
        {
            if (i.photonView.IsMine)
            {
                PhotonNetwork.Destroy(i.gameObject);
            }
        }
    }
    private static readonly string bugle_name = "Bugle";
    private static readonly ushort bugle_id = 15;
    public static void GiveBugle(Character c)
    {
        ItemInstanceData d = new(Guid.NewGuid());
        GiveItem.Do(c, bugle_id, bugle_name, d);
    }
    public static bool TootsBugle(Character c)
    {
        var i = c.data.currentItem;
        var n = AllBans.stripName(i.name);
        if (!AllBugles.Contains(n)) return false;
        return i.isUsingPrimary;
    }
}


[HarmonyPatch(typeof(PhotonNetwork), "NetworkInstantiate", [typeof(Photon.Pun.InstantiateParameters), typeof(bool), typeof(bool)])]
public static class SuperSwarmMaker
{
    public static void Postfix(Photon.Pun.InstantiateParameters parameters, bool roomObject, bool instantiateEvent, ref GameObject __result)
    {
        var _parameters = parameters;
        if (_parameters.prefabName != BeeSwarmData.SwarmName) return;
        if (_parameters.data.Length == 0) return;
        string s = (string)_parameters.data[0];
        if (s == null || s != "Random Events") return;

        var bees = __result.GetComponent<BeeSwarm>();
        bees.deAggroDistance = 10000f;
        bees.hiveAggroDistance = 10000f;
        bees.beesAngry = true;
        bees.beehiveDangerTick = 3600f;
        bees.beesDispersalTime = 3600f;
        bees.movementForceAngry *= 0.65f;
        if (bees.photonView.IsMine)
        {
            bees.currentAggroCharacter = Character.localCharacter;
        }
    }
}

[HarmonyPatch(typeof(BeeSwarm), "Update")]
public static class BeePush
{
    private static void Postfix(BeeSwarm __instance)
    {
        if (!BeeSwarmData.enabled) return;
        if (!__instance.photonView.IsMine) return;

        Vector3? pb_Vector = null;

        foreach (var c in Character.AllCharacters)
        {
            if (!BeeSwarmData.TootsBugle(c))
            {
                continue;
            }

            var p = c.Center;
            var q = __instance.gameObject.transform.position;
            var d = Vector3.Distance(p, q);

            var ld = c.data.lookDirection.normalized;

            var line_dist = Vector3.Cross(ld, q - p).magnitude;

            if (d < 10.0f || (d < 30.0f && line_dist < 5.0f))
            {
                pb_Vector = (q - p).normalized;
                break;
            }
        }

        if (pb_Vector.HasValue)
        {
            __instance.rb.AddForce(pb_Vector.Value * (450.0f * Time.fixedDeltaTime), ForceMode.Acceleration);
        }

        // Despawn checks. We disable bees in the kiln for simplicity.
        var cf = MapHandler.CurrentCampfire;
        if (cf != null)
        {
            var cp = cf.gameObject.transform.position;
            if ((Character.localCharacter.Center - cp).magnitude < 20.0f)
            {
                __instance.deAggroDistance = 0f;
                __instance.hiveAggroDistance = 0f;
                __instance.beesAngry = false;
                __instance.beehiveDangerTick = 0f;
                __instance.beesDispersalTime = 0f;
            }
        }
    }
}

public class BeesEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        BeeSwarmData.enabled = false;
        BeeSwarmData.DestroyBees();
    }

    public static IEnumerator DelayedBees()
    {
        yield return new WaitForSeconds(15.0f);
        BeeSwarmData.MakeSuperBees(Character.localCharacter);
    }
    public void Enable(EventInterface eintf)
    {
        eintf.AddEnableLine(new NiceText {
            s = "Bees.",
            c = Color.yellow * 0.8f,
        });
        eintf.AddEnableLine("(Hint: bees dislike noise.)");
    }

    public void LateEnable(EventInterface eintf)
    {
        BeeSwarmData.enabled = true;
        BeeSwarmData.GiveBugle(Character.localCharacter);
        GlobalBehaviours.late_events?.StartCoroutine(DelayedBees());
    }

    public JObject to_json()
    {
        return [];
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = _ => new BeesEvent(),
            FromJson = _ => new BeesEvent(),
        };
    }
    public HashSet<OurBiome> ZoneLimit()
    {
        return
        [
            OurBiome.Shore,
            OurBiome.Tropics,
            OurBiome.Roots,
            OurBiome.Alpine,
            OurBiome.Mesa,
            OurBiome.Caldera,
        ];
    }
}