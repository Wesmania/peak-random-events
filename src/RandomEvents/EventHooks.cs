
using System.Net.WebSockets;
using HarmonyLib;
using Photon.Realtime;
using Unity.Mathematics;
using Zorro.Core;

namespace RandomEvents;

static class Stuff
{
    static string? new_events;
    public static void NewEvents(bool is_first, OurBiome biome)
    {
        if (!Messages.IsMaster())
        {
            return;
        }
        new_events = Plugin.pick_events.PickNewEvents(is_first, biome);

        if (new_events != null)
        {
            Plugin.m.SendEvent(MessageType.STOP_EVENTS, "", ReceiverGroup.All, true);
            Plugin.m.SendEvent(MessageType.NEW_EVENTS, new_events, ReceiverGroup.All, true);
        }
    }
    public static void ResendEvent(Photon.Realtime.Player target)
    {
        if (!Messages.IsMaster())
        {
            return;
        }
        if (new_events != null)
        {
            Plugin.m.SendEventTo(MessageType.STOP_EVENTS, "", [target.ActorNumber], true);
            Plugin.m.SendEventTo(MessageType.NEW_EVENTS, new_events, [target.ActorNumber], true);
        }
    }
}

[HarmonyPatch(typeof(AirportCheckInKiosk), "LoadIslandMaster")]
public static class ChangeMapPatch1
{
    public static void Postfix(AirportCheckInKiosk __instance, int ascent)
    {
        Stuff.NewEvents(true, OurBiome.Shore);
    }
}

[HarmonyPatch(typeof(Campfire))]
public static class ChangeMapPatch2
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