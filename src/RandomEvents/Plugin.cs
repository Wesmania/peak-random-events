using BepInEx;
using BepInEx.Logging;
using ExitGames.Client.Photon;
using HarmonyLib;

namespace RandomEvents;


[HarmonyPatch(typeof(GUIManager))]
public static class GlobalBehaviours
{
    public static DoomMusic? doom_music;
    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    public static void StartPostfix(GUIManager __instance)
    {
        doom_music = __instance.gameObject.AddComponent<DoomMusic>();
    }
}

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    public static ManualLogSource Log = null!;
    public static PickEvents pick_events = new();
    public static Messages m = new(Plugin.HandleMessages);
    public static EventInterface eintf = new();
    private void Awake()
    {

        Harmony harmony = new("com.github.Wesmania.RandomEvents");
        try
        {
            harmony.PatchAll();
        }
        catch (System.Exception ex)
        {
            Log.LogError($"Failed to load mod: {ex}");
        }

        Log = Logger;

        Log.LogInfo($"Plugin {Name} is loaded!");
    }

    private static void HandleMessages(EventData photonEvent)
    {
        var data = (object[])photonEvent.CustomData;
        var t = (MessageType)(int)data[0];
        var c = (string)data[1];
        Plugin.Log.LogInfo($"Receiving event {t}");
        switch (t)
        {
            case MessageType.NEW_EVENTS:
                Plugin.pick_events.LoadNewEvents(c, eintf);
                float delay_end = Plugin.pick_events.is_first ? 10f : 0f;
                float delay_start = 10f;
                eintf.RunInterface(delay_end, delay_start);
                break;
            case MessageType.STOP_EVENTS:
                Plugin.pick_events.UnloadEvents(eintf);
                break;
        }
    }
}
