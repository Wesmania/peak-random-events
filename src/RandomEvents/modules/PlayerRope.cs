using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using UnityEngine;

namespace RandomEvents;

class PlayerRopeData
{
    public static bool enabled = false;
    private static GameObject? rope = null;
    static public void enable()
    {
        enabled = true;
        var p = Character.localCharacter;
        rope = PhotonNetwork.Instantiate("RopeAnchorForRopeShooter", p.Center, Quaternion.identity, 0, [(object)"Random Events", (object)p.photonView.OwnerActorNr]);
        var rawr = rope.GetComponent<RopeAnchorWithRope>();
        rawr.SpawnRope();
    }
    static public void disable()
    {
        enabled = false;
        if (rope != null)
        {
            var rawr = rope.GetComponent<RopeAnchorWithRope>();
            PhotonNetwork.Destroy(rawr.ropeInstance);
            PhotonNetwork.Destroy(rope);
            rope = null;
        }
    }
}

public class PlayerRopeAddedInfo : MonoBehaviour
{
    public int actor = -1;
}

[HarmonyPatch(typeof(PhotonNetwork), "NetworkInstantiate", [typeof(Photon.Pun.InstantiateParameters), typeof(bool), typeof(bool)])]
public static class PersonalRopeMaker
{
    public static void Postfix(Photon.Pun.InstantiateParameters parameters, bool roomObject, bool instantiateEvent, ref GameObject __result)
    {
        var _parameters = parameters;

        var name = AllBans.stripName(__result.name);
        if (name != "RopeAnchorForRopeShooter") return;

        if (_parameters.data.Length == 0) return;
        string s = (string)_parameters.data[0];
        if (s == null || s != "Random Events") return;
        int pcid = (int)_parameters.data[1];

        var rawr = __result.GetComponent<RopeAnchorWithRope>();
        rawr.ropeSegmentLength = 12.0f;

        foreach (var c in Character.AllCharacters) {
            if (c.photonView.OwnerActorNr == pcid) {
                rawr.anchor.gameObject.transform.parent = c.GetBodypart(BodypartType.Torso).transform;
                rawr.anchor.gameObject.transform.localScale = Vector3.zero;
                var i = rawr.anchor.gameObject.AddComponent<PlayerRopeAddedInfo>();
                i.actor = pcid;
                break;
            }
        }
    }
}

[HarmonyPatch(typeof(Rope), "AttachToAnchor_Rpc")]
public static class RopeAnchorer
{
    public static void Postfix(Rope __instance, PhotonView anchorView, float ropeLength)
    {
        if (!__instance.photonView.IsMine) return;

        var info = __instance.attachedToAnchor?.GetComponent<PlayerRopeAddedInfo>();
        if (info == null) return;

        Character? att = null;
        foreach (var c in Character.AllCharacters)
        {
            if (c.photonView.OwnerActorNr == info.actor)
            {
                att = c;
            }
        }
        if (att == null) return;

        List<Transform> ropeSegments = __instance.GetRopeSegments();
        if (ropeSegments.Count == 0)
        {
            __instance.AddSegment();
            ropeSegments = __instance.GetRopeSegments();
        }

        var s = ropeSegments[0].GetComponent<RopeSegment>();
        var joint = s.gameObject.GetComponent<ConfigurableJoint>();
        if (joint == null) return;

        joint.connectedBody = att!.GetBodypartRig(BodypartType.Torso);
    }
}

[HarmonyPatch(typeof(CharacterClimbing), "Update")]
public static class RopeStaminaDrain
{
    public static void Prefix(CharacterClimbing __instance)
    {
        if (!__instance.view.IsMine) return;
        if (!PlayerRopeData.enabled) return;

        if (__instance.character.data.isRopeClimbing)
        {
            __instance.character.UseStamina(0.05f * Time.deltaTime);
        }
    }
}

public class PlayerRopeEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        PlayerRopeData.disable();
    }

    public void Enable(EventInterface eintf)
    {
        eintf.AddEnableLine("Every scout ties a rope around his waist.");
        eintf.AddEnableLine(new NiceText
        {
            c = Color.red,
            s = "Holding on to rope is more tiring."
        });
    }
    public void LateEnable(EventInterface eintf)
    {
        PlayerRopeData.enable();
    }
    public JObject to_json()
    {
        return [];
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = _ => new PlayerRopeEvent(),
            FromJson = _ => new PlayerRopeEvent(),
        };
    }
}