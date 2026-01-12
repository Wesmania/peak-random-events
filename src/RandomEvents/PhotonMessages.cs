using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace RandomEvents;

public enum MessageType
{
    NEW_EVENTS = 1,
}

public static class Messages
{
    private static byte CODE = 197;
    private static void SendEvent(MessageType t, string e, ReceiverGroup who, bool reliable = false)
    {
        object[] content = [(int)t, e];
        RaiseEventOptions raiseEventOptions = new() { Receivers = who };
        var r = reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable;
        PhotonNetwork.RaiseEvent(CODE, content, raiseEventOptions, r);
    }
    private static void SendEventTo(MessageType t, string e, int[] targets, bool reliable = false)
    {
        object[] content = [(int)t, e];
        RaiseEventOptions raiseEventOptions = new() { TargetActors = targets };
        var r = reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable;
        PhotonNetwork.RaiseEvent(CODE, content, raiseEventOptions, r);
    }
}
