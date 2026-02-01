using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RandomEvents;

class Ramadan: ItemBan
{
    public static bool enabled = false;
    public static Ramadan? instance = null;
    private static HashSet<string> HaramItems = [
        "Airline Food",
        "Antidote",
        "Apple Berry Green",
        "Apple Berry Red",
        "Apple Berry Yellow",
        "Berrynana Blue",
        "Berrynana Brown",
        "Berrynana Pink",
        "Berrynana Yellow",
        "Bugfix",   // Tick
        "Clusterberry Black",
        "Clusterberry Red",
        "Clusterberry Yellow",
        "Clusterberry_UNUSED",
        "Cure-All",
        "Egg",
        "EggTurkey",
        "Energy Drink",
        "Fortified Milk",
        "Glizzy",   // Hot dog
        "Granola Bar",
        // See islamqa.info question 38023.
        //
        // "The fourth of the things that invalidate the fast is anything that
        // is regarded as coming under the same heading as eating and drinking.
        // This includes two things:
        // 2. Receiving via a needle (as in the case of a drip) nourishing
        // substances which take the place of food and drink, because this is
        // the same as food and drink. Shaykh Ibn 'Uthaymin, Majalis Shahr
        // Ramadan, p/ 70.".
        //
        // Since a blowgun provides nourishment, this rule seems to apply.
        "HealingDart Variant",
        "Item_Coconut_half",
        "Item_Honeycomb",
        "Kingberry Green",
        "Kingberry Purple",
        "Kingberry Yellow",
        "Lollipop",
        "Mandrake",
        "Mandrake_Hidden",
        "Marshmallow",
        "MedicinalRoot",
        "Mushroom Chubby",
        "Mushroom Cluster",
        "Mushroom Cluster Poison",
        "Mushroom Glow",
        "Mushroom Lace",
        "Mushroom Lace Poison",
        "Mushroom Normie",
        "Mushroom Normie Poison",
        "Napberry",
        "PandorasBox",
        "Pepper Berry",
        "Prickleberry_Gold",
        "Prickleberry_Red",
        "Scorpion",
        "ScoutCookies",
        "Shroomberry_Blue",
        "Shroomberry_Green",
        "Shroomberry_Purple",
        "Shroomberry_Red",
        "Shroomberry_Yellow",
        "Sports Drink",
        "TrailMix",
        "Winterberry Orange",
        "Winterberry Yellow",
    ];
    private static bool BreaksFast(string item)
    {
        return HaramItems.Contains(item);
    }

    private static bool IsDay()
    {
        return DayNightManager.instance.isDay == 1.0f;
    }

    public static bool IsHaram(string item)
    {
        return enabled && IsDay() && BreaksFast(item);
    }
    public static bool CanBreakFast()
    {
        // See islamqa.info question 23296 for reference.
        var c = Character.localCharacter;
        if (c == null) return false;
        // It is permissible to break fast if you are ill and not eating is a risk to your health.
        if (c!.refs.afflictions.shouldPassOut)
        {
            return true;
        }
        // Intense hunger may allow one to break the fast.
        // TODO: technically it should only happen once per day and you should be allowed to eat as much as you want for that moment.
        if (c!.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Hunger) > 0.7f)
        {
            return true;
        }
        return false;
    }

    public override bool BanPrimary(Item i, string name)
    {
        return IsHaram(name) && !CanBreakFast();
    }

    public override bool BanSecondary(Item i, string name)
    {
        return IsHaram(name);
    }
}

public class RamadanDelay : MonoBehaviour
{
    public void DelayedEnableRamadan()
    {
        StartCoroutine(DoDelayedEnableRamadan());
        static IEnumerator DoDelayedEnableRamadan()
        {
            yield return new WaitForSeconds(15.0f);
            Ramadan.enabled = true;
            Ramadan.instance = new();
            AllBans.RegisterBan(Ramadan.instance);
        }
    }
}
public class RamadanEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        Ramadan.enabled = false;
        Ramadan.instance?.Dispose();
        Ramadan.instance = null;
    }

    public void Enable(EventInterface eintf)
    {
        GlobalBehaviours.ramadan?.DelayedEnableRamadan();
        eintf.AddEnableLine("It is Ramadan and all scouts are Muslim.");
    }

    public JObject to_json()
    {
        return [];
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = _ => new RamadanEvent(),
            FromJson = _ => new RamadanEvent(),
        };
    }
    public HashSet<OurBiome> ZoneLimit()
    {
        return
        [
            OurBiome.Shore,
            OurBiome.Tropics,
            OurBiome.Roots,
            OurBiome.Alpine,
            OurBiome.Mesa,
        ];
    }
}