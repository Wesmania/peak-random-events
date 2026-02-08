using BepInEx;
using BepInEx.Logging;
using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace RandomEvents;


[HarmonyPatch(typeof(GUIManager))]
public static class GlobalBehaviours
{
    public static GameObject? coro;
    public static DoomMusic? doom_music;
    public static LateEventCaller? late_events;
    public static RamadanDelay? ramadan;

    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    public static void StartPostfix(GUIManager __instance)
    {
        DoStartPostfix();
    }
    public static void DoStartPostfix() {
        if (coro != null) return;
        coro = new GameObject("coro_2");
        UnityEngine.Object.DontDestroyOnLoad(coro);
        doom_music = coro.AddComponent<DoomMusic>();
        late_events = coro.AddComponent<LateEventCaller>();
        ramadan = coro.AddComponent<RamadanDelay>();
    }
}

public class PhotonCallbacks : IInRoomCallbacks
{
    public void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        return;
    }

    public void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Stuff.ResendEvent(newPlayer);
    }

    public void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        return;
    }

    public void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
    {
        return;
    }

    public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        return;
    }
}

[BepInAutoPlugin]
[BepInDependency("com.github.Wesmania.ItemMultiplierBis", BepInDependency.DependencyFlags.SoftDependency)]  // For item multiplier event
public partial class Plugin : BaseUnityPlugin
{
    public static ManualLogSource Log = null!;
    public static PickEvents pick_events = new();
    public static Messages m = new(Plugin.HandleMessages);
    public static EventInterface eintf = new();
    public static PhotonCallbacks pcb = new();

    public static EventsConfig? config = null;
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

        PhotonNetwork.AddCallbackTarget(pcb);

        Log = Logger;

        config ??= new EventsConfig(Config);

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
                eintf.RunInterface(eintf.is_first ? 10f : 5f);
                break;
            case MessageType.STOP_EVENTS:
                Plugin.pick_events.UnloadEvents(eintf);
                break;
        }
    }
}