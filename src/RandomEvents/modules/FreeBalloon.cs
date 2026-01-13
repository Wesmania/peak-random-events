
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using UnityEngine;

namespace RandomEvents;

[HarmonyPatch(typeof(TiedBalloon))]
public class TiedBalloonPatch
{
    // Balloon owner controls his balloon.
    private static TiedBalloon? my_balloon;
    private static System.Random rng = new();

    [HarmonyPrefix]
    [HarmonyPatch("Pop")]
    private static bool PopPrefix(TiedBalloon __instance)
    {
        if (__instance == my_balloon)
        {
            __instance.popHeight += 100f;
            __instance.popTime += 100f;
            return false;
        }
        return false;
    }

    public static void AddPermaBalloonToMe()
    {
        if (my_balloon != null)
        {
            return;
        }
        var c = Character.localCharacter;
        if (c == null)
        {
            return;
        }
        Character character = c!;
        var balloons = character.refs.balloons;
        var balloonTie = balloons.balloonTie;
        var colorIndex = rng.Next() % balloons.balloonColors.Length;

        // Borrowed from balloon tying function
        my_balloon = PhotonNetwork.Instantiate("TiedBalloon", character.Center, Quaternion.identity, 0).GetComponent<TiedBalloon>();
        my_balloon.Init(balloons, character.Center.y, colorIndex);
        for (int i = 0; i < balloonTie.Length; i++)
        {
            balloonTie[i].Play(character.Center);
        }
    }
    public static void RemovePermaBalloonFromMe()
    {
        if (my_balloon == null) return;
        var mb = my_balloon!;
        my_balloon = null;
        mb.Pop();
    }
}

public class FreeBalloonEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        TiedBalloonPatch.RemovePermaBalloonFromMe();
    }

    public void Enable(EventInterface eintf)
    {
        eintf.AddEnableLine("Everyone gets a free balloon :)");
        TiedBalloonPatch.AddPermaBalloonToMe();
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
            New = () => new FreeBalloonEvent(),
            FromJson = _ => new FreeBalloonEvent(),
        };
    }
}