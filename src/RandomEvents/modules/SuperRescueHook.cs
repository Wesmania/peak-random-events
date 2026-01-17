using System;
using System.ComponentModel;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Photon.Pun;
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
            var k = d.RegisterNewEntry<IntItemData>(DataEntryKey.ItemUses);
            k.Value = -1;

            ItemInstanceDataHandler.AddInstanceData(d);
            if (p.AddItem(item_id, d, out ItemSlot s))
            {
                // Done, TODO remember what item it was?
                continue;
            }
            Vector3 spawnPos = player.Center + Vector3.up * 0.2f + Vector3.forward * 0.1f;
            GameObject rh = PhotonNetwork.Instantiate("0_Items/" + item_name, spawnPos, Quaternion.identity);
            RescueHook h = rh.GetComponent<RescueHook>();
            h.item.totalUses = -1;
        }
    }

    // Called by master.
    public static void CleanupAllHooks()
    {
        void cleanSlot(ItemSlot itemSlot, Action<ItemSlot> clean)
        {
            if (!itemSlot.IsEmpty() && itemSlot.prefab != null && itemSlot.prefab.itemID == item_id && itemSlot.data != null)
            {
                if (itemSlot.data.TryGetDataEntry<IntItemData>(DataEntryKey.ItemUses, out IntItemData v))
                {
                    if (v.Value == -1)
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
            if (((ItemSlot)p.backpackSlot).data.TryGetDataEntry<BackpackData>((DataEntryKey)7, out BackpackData b) && b?.itemSlots != null)
            {
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
        foreach (Item i in UnityEngine.Object.FindObjectsByType<Item>(FindObjectsSortMode.None))
        {
            if (i.itemID == item_id && i.totalUses == -1)
            {
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
            usage *= 2.0f;
        }
    }
}
public class SuperRescueHookEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        SuperRescueHook.CleanupAllHooks();
        CharacterUseStaminaPatch.increase_cost = false;
    }

    public void Enable(EventInterface eintf)
    {
        eintf.AddEnableLine(new NiceText()
        {
            s = "Infinite rescue hooks.",
            c = Color.green,
        });
        eintf.AddEnableLine(new NiceText()
        {
            s = "Climbing costs much more stamina.",
            c = Color.red,
        });
        SuperRescueHook.GiveEveryoneHooks();
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
            New = () => new SuperRescueHookEvent(),
            FromJson = _ => new SuperRescueHookEvent(),
        };
    }
}