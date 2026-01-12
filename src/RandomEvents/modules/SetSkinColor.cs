using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Zorro.Core;

namespace RandomEvents;

enum SkinColor
{
    RED = 0,
    ORANGE = 1,
    YELLOW = 2,
    LIME = 3,
    GREEN = 4,
    CYAN = 5,
    BLUE = 6,
    PURPLE = 7,
    PINK = 8,
    RANDOM = 9,
}

class DelayedMagic : MonoBehaviour
{
    public void DelayedSetColor(int c)
    {
        StartCoroutine(Do());
        IEnumerator Do()
        {
            yield return new WaitForSeconds(10.0f);
            CharacterCustomization.SetCharacterSkinColor(c);
        }
    }
}

public class GreatMagicianEvent : IEvent
{
    private SkinColor sc;
    private GameObject o;
    int original_color = -1;
    private static System.Random rng = new();

    void SetGo()
    {
        o = new GameObject("Great Magician");
        o.AddComponent<DelayedMagic>();
    }
    GreatMagicianEvent(SkinColor _sc)
    {
        sc = _sc;
        SetGo();
    }
    GreatMagicianEvent()
    {
        sc = (SkinColor)(rng.Next() % 10);
        SetGo();
    }
    private static String ScStr(SkinColor sc)
    {
        switch (sc)
        {
            case SkinColor.RED:
                return "red";
            case SkinColor.ORANGE:
                return "orange";
            case SkinColor.YELLOW:
                return "yellow";
            case SkinColor.LIME:
                return "lime";
            case SkinColor.GREEN:
                return "green";
            case SkinColor.CYAN:
                return "cyan";
            case SkinColor.BLUE:
                return "blue";
            case SkinColor.PURPLE:
                return "purple";
            case SkinColor.PINK:
                return "pink";
            case SkinColor.RANDOM:
            default:
                return "random";
        }
    }

    private static int ToSkinColor(SkinColor sc)
    {
        if (sc == SkinColor.RANDOM)
        {
            return rng.Next() % 9;
        }
        else
        {
            return (int)sc;
        }
    }
    public void Disable(EventInterface eintf)
    {
        CharacterCustomization.SetCharacterSkinColor(original_color);
    }

    public void Enable(EventInterface eintf)
    {
        int i = ToSkinColor(sc);
        Color c = Singleton<Customization>.Instance.skins[i].color;
        eintf.AddEnableLine(new NiceText
        {
            s = $"I am a great magician. Your scout is {ScStr(sc)}.",
            c = c,
        });

        CharacterCustomizationData customizationData = CharacterCustomization.GetCustomizationData(Photon.Pun.PhotonNetwork.LocalPlayer);
        original_color = customizationData.currentSkin;

        var dc = o.GetComponent<DelayedMagic>();
        dc.DelayedSetColor(i);
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = () => new GreatMagicianEvent(),
            FromJson = o =>
            {
                SkinColor sc = (SkinColor)((int?)o.GetValue("sc")).GetValueOrDefault((int)SkinColor.RANDOM);
                return new GreatMagicianEvent(sc);
            }
        };
    }


    public JObject to_json()
    {
        JObject o = [];
        o.Add("sc", (int)sc);
        return o;
    }
}