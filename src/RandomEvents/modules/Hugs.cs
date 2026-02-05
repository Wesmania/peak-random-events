using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RandomEvents;

public static class HugsData
{
    public static bool enabled = false;
    public static bool IsHugging(Character hugger, Character huggee)
    {
        if (Vector3.Distance(hugger.Center, huggee.Center) > 3) return false;
        if (Vector3.Distance(hugger.Center, huggee.Center) < 0.1) return false;     // Filter myself

        var hugger_neck = hugger.GetBodypart(BodypartType.Torso).transform.position;
        var hugger_hand_l = hugger.GetBodypart(BodypartType.Hand_L).transform.position;
        var hugger_hand_r = hugger.GetBodypart(BodypartType.Hand_R).transform.position;

        var huggee_neck = huggee.GetBodypart(BodypartType.Torso).transform.position;

        var a1 = Vector3.Angle(hugger_neck - huggee_neck, hugger_hand_l - huggee_neck);
        var a2 = Vector3.Angle(hugger_neck - huggee_neck, hugger_hand_r - huggee_neck);

        List<float> angles = [a1, a2];

        // Picked experimentally.
        return angles.All(a => a >= 70.0f) && angles.Any(a => a >= 85.0f);
    }
}

[HarmonyPatch(typeof(CharacterHeatEmission), "Update")]
public static class HugsPatch
{
    public static void Postfix(CharacterHeatEmission __instance)
    {
        if (__instance.counter != 0f) return;
        if (!HugsData.enabled) return;

        var l = Character.localCharacter;
        bool is_being_hugged = Character.AllCharacters.Any(c => HugsData.IsHugging(c, l) || HugsData.IsHugging(l, c));
        if (is_being_hugged)
        {
            l.AddExtraStamina(0.05f);
        }
        else
        {
            l.AddExtraStamina(-0.01f);
        }
    }
}

public class HugsEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        HugsData.enabled = false;
    }

    public void Enable(EventInterface eintf)
    {
        HugsData.enabled = true;
        eintf.AddEnableLine(new NiceText
        {
            c = Color.red,
            s = "Extra stamina keeps draining."
        });
        eintf.AddEnableLine(new NiceText
        {
            c = new Color(1.0f, 105.0f / 256.0f, 180f / 256f),  // pink
            s = "Hugs restore extra stamina."
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
            New = _ => new HugsEvent(),
            FromJson = _ => new HugsEvent(),
        };
    }
}