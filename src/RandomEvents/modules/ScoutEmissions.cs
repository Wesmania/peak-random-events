using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.WSA;
using Zorro.Core;

namespace RandomEvents;

public enum EmissionType
{
    HEAT = 0,
    COLD = 1,
    ENERGY = 2
}

public class PlayerEmission
{
    public EmissionType emitting_e;

    private static Dictionary<Color, EmissionType>? colors = null;
    private static Dictionary<SkinColor, EmissionType> s2e = new(){
        { SkinColor.RED, EmissionType.HEAT },
        { SkinColor.ORANGE, EmissionType.HEAT },
        { SkinColor.PINK, EmissionType.HEAT },
        { SkinColor.BLUE, EmissionType.COLD },
        { SkinColor.PURPLE, EmissionType.COLD },
        { SkinColor.CYAN, EmissionType.COLD },
        { SkinColor.GREEN, EmissionType.ENERGY },
        { SkinColor.LIME, EmissionType.ENERGY },
        { SkinColor.YELLOW, EmissionType.ENERGY },
    };
    public PlayerEmission(EmissionType _e)
    {
        emitting_e = _e;
    }

    private Dictionary<Color, EmissionType> GetColors()
    {
        colors ??= Singleton<Customization>.Instance.skins.Select((x, i) => (x.color, s2e[(SkinColor)i])).ToDictionary(x => x.Item1, x => x.Item2);
        return colors!;
    }
    public EmissionType? CheckEmit(Character emitter, Character emittee)
    {
        var c = GetColors();
        Color pcolor = emitter.refs.customization.PlayerColor;
        if (!c.ContainsKey(pcolor))
        {
            return null;
        }
        var e = c[pcolor];

        pcolor = emittee.refs.customization.PlayerColor;
        if (!c.ContainsKey(pcolor))
        {
            return null;
        }
        var ee = c[pcolor];

        // Type is not the enabled type
        if (e != emitting_e)
        {
            return null;
        }
        // Same type doesn't hurt each other
        if (e == ee)
        {
            return null;
        }
        return e;
    }
}

[HarmonyPatch(typeof(CharacterHeatEmission))]
public static class HeatEmissionsPatch
{
    public static PlayerEmission? activeEmission;

    [HarmonyPostfix]
    [HarmonyPatch("Update")]
    public static void UpdatePostfix(CharacterHeatEmission __instance)
    {
        // If counter is 0, then we rolled over and triggered proximity
        if (__instance.counter != 0f)
        {
            return;
        }
        if (activeEmission == null)
        {
            return;
        }

        var emitter = __instance.character;

        foreach (Character allCharacter in Character.AllCharacters)
        {
            if (Vector3.Distance(__instance.transform.position, allCharacter.Center) < __instance.radius)
            {
                var emittee = allCharacter;
                var emit = activeEmission?.CheckEmit(emitter, emittee);
                if (emit == null)
                {
                    return;
                }
                switch (emit!)
                {
                    case EmissionType.HEAT:
                        allCharacter.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Hot, __instance.heatAmount);
                        break;
                    case EmissionType.COLD:
                        // Times two to counteract heating up
                        allCharacter.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Cold, __instance.heatAmount * 2);
                        break;
                    case EmissionType.ENERGY:
                        allCharacter.AddExtraStamina(-__instance.heatAmount);
                        break;
                }
            }
        }
    }
}

public class HeatEmissionEvent : IEvent
{
    private PlayerEmission e;
    public HeatEmissionEvent(EmissionType et)
    {
        this.e = new PlayerEmission(et);
    }

    public HeatEmissionEvent()
    {

        var et = (EmissionType) UnityEngine.Random.Range(0, 3);
        this.e = new PlayerEmission(et);
    }

    public void Disable(EventInterface eintf)
    {
        HeatEmissionsPatch.activeEmission = null;
    }

    public void Enable(EventInterface eintf)
    {
        string line = "";
        Color c = Color.white;
        switch (e.emitting_e)
        {
            case EmissionType.HEAT:
                line = "Reddish scouts are hot.";
                c = Color.red;
                break;
            case EmissionType.COLD:
                line = "Blueish scouts are cold.";
                c = Color.blue;
                break;
            case EmissionType.ENERGY:
                line = "Greenish scouts eat your energy.";
                c = Color.green;
                break;
        }
        eintf.AddEnableLine(new NiceText
        {
            s = line,
            c = c,
        });
        HeatEmissionsPatch.activeEmission = e;
    }

    public JObject to_json()
    {
        JObject o = [];
        o.Add("et", (int)e.emitting_e);
        return o;
    }

    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = _ => new HeatEmissionEvent(),
            FromJson = o =>
            {
                EmissionType et = (EmissionType)((int?)o.GetValue("et")).GetValueOrDefault((int) EmissionType.HEAT);
                return new HeatEmissionEvent(et);
            }
        };
    }
}