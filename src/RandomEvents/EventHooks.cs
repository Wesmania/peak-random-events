
using HarmonyLib;
using Photon.Realtime;

namespace RandomEvents;

static class Stuff
{
    public static void NewEvents(bool is_first)
    {
        var new_events = Plugin.pick_events.PickNewEvents(is_first);
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
        Stuff.NewEvents(true);
    }
}

[HarmonyPatch(typeof(Campfire))]
public static class RecalculateSoulmatesPatch2
{
    [HarmonyPostfix]
    [HarmonyPatch("Light_Rpc")]
    public static void LightPostfix(Campfire __instance)
    {
        Stuff.NewEvents(false);
    }
}