
using System.Net.WebSockets;
using HarmonyLib;
using Photon.Realtime;
using Zorro.Core;

namespace RandomEvents;

static class Stuff
{
    public static void NewEvents(bool is_first, OurBiome biome)
    {
        if (!Photon.Pun.PhotonNetwork.IsMasterClient)
        {
            return;
        }
        var new_events = Plugin.pick_events.PickNewEvents(is_first, biome);
        if (new_events != null)
        {
            Plugin.m.SendEvent(MessageType.STOP_EVENTS, "", ReceiverGroup.All, true);
            Plugin.m.SendEvent(MessageType.NEW_EVENTS, new_events, ReceiverGroup.All, true);
        }
    }
}

[HarmonyPatch(typeof(Character))]
public static class RecalculateSoulmatesPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("StartPassedOutOnTheBeach")]
    public static void StartPassedOutOnTheBeachPostfix(Character __instance)
    {
        if (!__instance.IsLocal)
        {
            return;
        }
        Stuff.NewEvents(true, OurBiome.Shore);
    }
}

[HarmonyPatch(typeof(Campfire))]
public static class RecalculateSoulmatesPatch2
{
    [HarmonyPostfix]
    [HarmonyPatch("Light_Rpc")]
    public static void LightPostfix(Campfire __instance)
    {
        var map = Singleton<MapHandler>.Instance;
        if (map == null) return;
        var this_seg = __instance.advanceToSegment;
        var biome = map.segments[(int)this_seg].biome;
        var our_biome = BiomeConv.FromSegmentBiome(this_seg, biome);
        Stuff.NewEvents(false, our_biome);
    }
}