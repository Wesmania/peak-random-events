using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RandomEvents;

static class SlapInfo
{
    static public bool enabled = false;
    static public bool is_doom = false;

    static private bool DrankEnergol(Character c)
    {
        return c.refs.afflictions.afflictionList.Any(x => x.GetAfflictionType() == Peak.Afflictions.Affliction.AfflictionType.FasterBoi);
    }

    static public void SlapPlayer(Character slapper, Character slappee)
    {
        if (slapper.data.sinceGrabFriend < 0.2f)
        {
            return;
        }
        if (slappee.IsStuck() && slappee.IsLocal)
        {
            slappee.UnStick();
        }
        var a = slappee.refs.afflictions;

        a.SubtractStatus(CharacterAfflictions.STATUSTYPE.Web, 1f);
        foreach (CharacterAfflictions.STATUSTYPE s in new[] {
            CharacterAfflictions.STATUSTYPE.Cold,
            CharacterAfflictions.STATUSTYPE.Hot,
            CharacterAfflictions.STATUSTYPE.Drowsy,
            CharacterAfflictions.STATUSTYPE.Poison,
            CharacterAfflictions.STATUSTYPE.Spores,
            })
        {
            a.SubtractStatus(s, 0.075f);
        }

        var isStrong = UnityEngine.Random.value < 0.1666;
        var punch_s = 4000.0f;
        var fall_s = 0.1f;
        var hurt = 0f;
        if (isStrong)
        {
            punch_s *= 2;
            fall_s *= 5;
            hurt += 0.025f;
        }

        if (DrankEnergol(slapper))
        {
            punch_s *= 4;
            fall_s += 2f;
            hurt *= 4;
        }

        if (is_doom)
        {
            punch_s *= 2;
        }

        if (hurt > 0.0f)
        {
            a.AddStatus(CharacterAfflictions.STATUSTYPE.Injury, hurt);
        }

        slappee.Fall(fall_s);
        var to = slapper.refs.animationLookTransform.TransformDirection(new Vector3(-1.0f, 0.5f, 1.0f));
        slappee.GetBodypart(BodypartType.Head).AddForce(to * punch_s, ForceMode.Force);

        slapper.data.sinceGrabFriend = 0f;
    }

    public static IEnumerator SlapPlayerLater(Character puncher, Character punchee, float delay)
    {
        yield return new WaitForSeconds(delay);
        SlapPlayer(puncher, punchee);
    }
}


[HarmonyPatch(typeof(Bodypart), "Animate")]
class SlapSpeedPatch
{
    private static void Prefix(Bodypart __instance, ref float force, ref float torque)
    {
        if (!SlapInfo.enabled) return;
        var character = __instance.character;
        Dictionary<BodypartType, float> pts = new()
        {
            { BodypartType.Torso, 2.0f},
            { BodypartType.Hand_R, 3.0f},
            { BodypartType.Arm_R, 10.0f},
            { BodypartType.Elbow_R, 10.0f},
            { BodypartType.Shoulder_R, 10.0f},
        };
        if (character.data.isReaching && character.data.sincePressReach < 0.5f && pts.ContainsKey(__instance.partType))
        {
            force *= pts[__instance.partType];
            torque *= pts[__instance.partType];
        }
    }
}

[HarmonyPatch(typeof(CharacterAnimations), "ConfigureIK")]
class SlapLocationPatch
{
    private static void Postfix(CharacterAnimations __instance)
    {
        if (!SlapInfo.enabled) return;
        var character = __instance.character;
        var ReachHandPos = __instance.ReachHandPos;

        if (!(character.refs.IKHandTargetLeft == null))
        {
            if ((bool)character.data.currentItem)
            {
            }
            else if (__instance.ReachIK())
            {
                character.refs.IKHandTargetRight.position = character.refs.animationHeadTransform.position + character.refs.animationLookTransform.TransformDirection(new Vector3(-1.0f, 1.0f, 1.0f));
                //character.refs.IKHandTargetRight.localEulerAngles = new Vector3(ReachHandPos.x, ReachHandPos.y, ReachHandPos.z + character.data.lookValues.y + 90f);
            }
        }
    }
}

[HarmonyPatch(typeof(Character), "UpdateVariablesFixed")]
class SlapReset
{
    private static void Prefix(Character __instance, out float __state)
    {
        __state = __instance.data.sincePressReach;
    }
    private static void Postfix(Character __instance, float __state)
    {
        if (!SlapInfo.enabled) return;
        if (__state < 0.7f)
        {
            __instance.data.sincePressReach = __state + Time.fixedDeltaTime;
        }
    }
}


[HarmonyPatch(typeof(CharacterGrabbing), "Reach")]
class SlapAction
{
    private static bool Prefix(CharacterGrabbing __instance)
    {
        if (!SlapInfo.enabled) return true;

        // Replace grabbing logic with slapping logic.
        var character = __instance.character;

        Action<Character> do_stuff = allCharacter =>
        {
            float num = Vector3.Distance(character.Center, allCharacter.Center);
            if (!(num > 3f) && !(Vector3.Angle(character.data.lookDirection, allCharacter.Center - character.Center) > 60f))
            {
                if (allCharacter.refs.view.IsMine)
                {
                    character.StartCoroutine(SlapInfo.SlapPlayerLater(character, allCharacter, 0.2f));
                }
            }
        };
        foreach (Character allCharacter in Character.AllCharacters)
        {
            if (allCharacter.photonView.Owner.ActorNumber == character.photonView.Owner.ActorNumber) continue;
            do_stuff(allCharacter);
        }
        foreach (Character allCharacter in Character.AllBotCharacters)
        {
            do_stuff(allCharacter);
        }
        return false;
    }
}


public class SlapEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        SlapInfo.enabled = false;
        SlapInfo.is_doom = false;
    }

    public void Enable(EventInterface eintf)
    {
        eintf.AddEnableLine(new NiceText
        {
            c = Color.red,
            s = "You can't grab scouts."
        });
        eintf.AddEnableLine("You can slap scouts. Slapped scouts get their shit together.");

        SlapInfo.enabled = true;
        SlapInfo.is_doom = eintf.ActiveEvents().Contains(AllEvents.DOOM_MODE);
    }

    public JObject to_json()
    {
        return [];
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = _ => new SlapEvent(),
            FromJson = _ => new SlapEvent(),
        };
    }
}