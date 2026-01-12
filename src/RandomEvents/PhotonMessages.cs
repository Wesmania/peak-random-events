using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace RandomEvents;

public enum MessageType
{
    NEW_EVENTS = 1,
    STOP_EVENTS = 2,
}

public class Messages
{
    private static byte CODE = 197;
    private Action<EventData> cb;

    public Messages(Action<EventData> _cb)
    {
        cb = _cb;
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    private void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == CODE)
        {
            cb(photonEvent);
        }
    }
    public static bool IsMaster()
    {
        return PhotonNetwork.IsMasterClient;
    }
    public void SendEvent(MessageType t, string e, ReceiverGroup who, bool reliable = false)
    {
        Plugin.Log.LogInfo($"Sending event {t}");
        object[] content = [(int)t, e];
        RaiseEventOptions raiseEventOptions = new() { Receivers = who };
        var r = reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable;
        PhotonNetwork.RaiseEvent(CODE, content, raiseEventOptions, r);
    }
    public void SendEventTo(MessageType t, string e, int[] targets, bool reliable = false)
    {
        object[] content = [(int)t, e];
        RaiseEventOptions raiseEventOptions = new() { TargetActors = targets };
        var r = reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable;
        PhotonNetwork.RaiseEvent(CODE, content, raiseEventOptions, r);
    }
}
