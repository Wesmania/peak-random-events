using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using UnityEngine;

namespace RandomEvents;


class GrabPingsData
{
    public static bool enabled = false;
}

[HarmonyPatch(typeof(PointPinger), "ReceivePoint_Rpc")]
class PingGrabber
{
    public static readonly string FakePitonName = "0_Items/PickAxeHammered_Shitty";
    private static Dictionary<int, GameObject> FakePitons = [];
    private static void Postfix(PointPinger __instance, Vector3 point, Vector3 hitNormal)
    {
        if (!GrabPingsData.enabled) return;
        if (!PhotonNetwork.IsMasterClient) return;

        var hn = hitNormal;
        GameObject go = PhotonNetwork.Instantiate(FakePitonName, point + hn.normalized * 0.3f, Quaternion.LookRotation(-hitNormal, Vector3.up), 0, [(object) "Random Events"]);
        var pid = __instance.character.photonView.OwnerActorNr;
        if (FakePitons.ContainsKey(pid))
        {
            FakePitons.Remove(pid, out var OldPiton);
            PhotonNetwork.Destroy(OldPiton);
        }
        FakePitons.Add(pid, go);
        GlobalBehaviours.late_events?.PhotonDestroyDelayed(go, 1f);
    }
}

[HarmonyPatch(typeof(PhotonNetwork), "NetworkInstantiate", [typeof(Photon.Pun.InstantiateParameters), typeof(bool), typeof(bool)])]
public static class PickaxeHider
{
    public static void Postfix(Photon.Pun.InstantiateParameters parameters, bool roomObject, bool instantiateEvent, ref GameObject __result)
    {
        var _parameters = parameters;
        if (_parameters.prefabName != PingGrabber.FakePitonName) return;
        if (_parameters.data == null) return;
        if (_parameters.data.Length == 0) return;
        string s = (string)_parameters.data[0];
        if (s == null || s != "Random Events") return;

        __result.transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);
    }
}

[HarmonyPatch(typeof(ClimbHandle), "GetName")]
public static class PickaxeLabel
{
    public static void Postfix(ClimbHandle __instance, ref string __result)
    {
        if (__instance.isPickaxe && __instance.gameObject.transform.localScale == new Vector3(0f, 0f, 0f))
        {
            __result = "Hand";
        }
    }
}
public class GrabPingsEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        GrabPingsData.enabled = false;
    }

    public void Enable(EventInterface eintf)
    {
        GrabPingsData.enabled = true;
        eintf.AddEnableLine("You can grab onto scout pings.");
    }
    public JObject to_json()
    {
        return [];
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = _ => new GrabPingsEvent(),
            FromJson = _ => new GrabPingsEvent(),
        };
    }
}