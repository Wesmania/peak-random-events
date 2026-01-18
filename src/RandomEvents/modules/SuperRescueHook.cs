using System;
using System.ComponentModel;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using pworld.Scripts.Extensions;
using UnityEngine;
using Zorro.Core;

namespace RandomEvents;

class SuperRescueHook
{
    private static String item_name = "RescueHook";
    private static ushort item_id = 100;

    public static void GiveEveryoneHooks()
    {
        foreach (var player in Character.AllCharacters)
        {
            var p = player.player;
            ItemInstanceData d = new(Guid.NewGuid());

            // Infinite uses
            var k = d.RegisterNewEntry<OptionableIntItemData>(DataEntryKey.ItemUses);
            k.HasData = false;
            ItemInstanceDataHandler.AddInstanceData(d);
            if (p.AddItem(item_id, d, out ItemSlot s))
            {
                continue;
            }
            // Otherwise, spawn it for the player.
            var pos = player.GetBodypart(BodypartType.Hip).transform;
            Vector3 spawnPos = pos.position + pos.forward * 0.6f;
            GameObject rh = PhotonNetwork.Instantiate("0_Items/" + item_name, spawnPos, Quaternion.identity, 0);
            var pv = rh.GetComponent<PhotonView>();
            pv.RPC("SetItemInstanceDataRPC", RpcTarget.All, d);
        }
    }
    // Called by master.
    public static void CleanupAllHooks()
    {
        void cleanSlot(ItemSlot itemSlot, Action<ItemSlot> clean)
        {
            if (!itemSlot.IsEmpty() && itemSlot.prefab != null && itemSlot.prefab.itemID == item_id && itemSlot.data != null)
            {
                if (itemSlot.data.TryGetDataEntry<OptionableIntItemData>(DataEntryKey.ItemUses, out OptionableIntItemData v))
                {
                    if (v != null && !v.HasData)
                    {
                        clean(itemSlot);
                    }
                }
            }
        }
        foreach (var player in Character.AllCharacters)
        {
            var p = player.player;

            // Clean player slots.
            ItemSlot[] array = p.itemSlots;
            foreach (ItemSlot itemSlot in array)
            {
                cleanSlot(itemSlot, s => p.EmptySlot(Optionable<byte>.Some(s.itemSlotID)));
            }
            cleanSlot(p.tempFullSlot, s => p.EmptySlot(Optionable<byte>.Some(s.itemSlotID)));

            // Clean any worn backpacks.
            if (p.backpackSlot.IsEmpty())
            {
                continue;
            }
            if (p.backpackSlot.data.TryGetDataEntry<BackpackData>(DataEntryKey.BackpackData, out BackpackData b))
            {
                if (b?.itemSlots == null)
                {
                    continue;
                }
                foreach (ItemSlot itemSlot in b.itemSlots)
                {
                    cleanSlot(itemSlot, s =>
                    {
                        // TODO sync inventories?
                        s.prefab = null;
                        s.data = new ItemInstanceData(Guid.NewGuid());
                    });
                }
            }
        }

        // Clean any loose hooks.
        Item[] items = UnityEngine.Object.FindObjectsByType<Item>(FindObjectsSortMode.None);
        Item[] i2 = items;
        foreach (Item i in i2)
        {
            if (i.itemID != item_id) {
                continue;
            }
            var data = i.GetData<OptionableIntItemData>(DataEntryKey.ItemUses);
            if (!data.HasData) {
                PhotonNetwork.Destroy(i.photonView);
            }
        }

        // Clean any hooks in loose backpacks.
        foreach (Backpack i in UnityEngine.Object.FindObjectsByType<Backpack>(FindObjectsSortMode.None))
        {
            BackpackData backpackData = i.GetData<BackpackData>(DataEntryKey.BackpackData);
            if (backpackData?.itemSlots != null)
            {
                foreach (ItemSlot itemSlot3 in backpackData.itemSlots)
                {
                    cleanSlot(itemSlot3, s =>
                    {
                        // TODO sync inventories?
                        s.prefab = null;
                        s.data = new ItemInstanceData(Guid.NewGuid());
                    });
                }
            }
        }
    }
}

[HarmonyPatch(typeof(Character), "UseStamina")]
public class CharacterUseStaminaPatch
{
    public static bool increase_cost = false;
    private static void Prefix(Character __instance, ref float usage, bool useBonusStamina)
    {
        if (increase_cost)
        {
            usage *= 2.5f;
        }
    }
}
public class SuperRescueHookEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SuperRescueHook.CleanupAllHooks();
        }
        CharacterUseStaminaPatch.increase_cost = false;
    }

    public void Enable(EventInterface eintf)
    {
        eintf.AddEnableLine(new NiceText()
        {
            s = "Infinite rescue claws.",
            c = Color.green,
        });
        eintf.AddEnableLine(new NiceText()
        {
            s = "Climbing costs much more stamina.",
            c = Color.red,
        });
        if (PhotonNetwork.IsMasterClient)
        {
            SuperRescueHook.GiveEveryoneHooks();
        }
        CharacterUseStaminaPatch.increase_cost = true;
    }

    public JObject to_json()
    {
        return [];
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = _ => new SuperRescueHookEvent(),
            FromJson = _ => new SuperRescueHookEvent(),
        };
    }
}