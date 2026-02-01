using System;
using System.Linq;
using Photon.Pun;
using UnityEngine;

namespace RandomEvents;

public static class GiveItem
{
    public static void Do(Character player, ushort item_id, string item_name, ItemInstanceData d)
    {
        var p = player.player;

        ItemInstanceDataHandler.AddInstanceData(d);
        if (p.itemSlots.Any(s => s.IsEmpty()))
        {
            if (p.AddItem(item_id, d, out ItemSlot s))
            {
                return;
            }
        }
        // Otherwise, spawn it for the player.
        var pos = player.GetBodypart(BodypartType.Hip).transform;
        Vector3 spawnPos = pos.position + pos.forward * 0.6f;
        GameObject rh = PhotonNetwork.Instantiate("0_Items/" + item_name, spawnPos, Quaternion.identity, 0);
        var pv = rh.GetComponent<PhotonView>();
        pv.RPC("SetItemInstanceDataRPC", RpcTarget.All, d);
    }
}