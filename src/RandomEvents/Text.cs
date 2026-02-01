using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RandomEvents;

public class NiceText
{
    public String s = "";
    public Color c;
}
public class DoText : MonoBehaviour
{
    private Transform? transform_;
    private Canvas? c;
    private TMP_FontAsset? f;
    private GameObject[] at = [];

    private GameObject? co;
    public void Init(TMP_FontAsset? ft, Transform _pt)
    {
        f = ft;
        transform_ = _pt;
    }

    public void ResetTransform(Transform _pt)
    {
        transform_ = _pt;
        co?.transform.SetParent(transform_, false);
    }
    public void Awake()
    {
        if (co == null)
        {
            co = new GameObject("SoulmatePrompt");
            UnityEngine.GameObject.DontDestroyOnLoad(co);
            co.transform.SetParent(transform_, false);
            c = co.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceCamera;

            CanvasScaler cs = c.GetComponent<CanvasScaler>() ?? c.gameObject.AddComponent<CanvasScaler>(); ;
            cs.referencePixelsPerUnit = 100;
            cs.matchWidthOrHeight = 1;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.scaleFactor = 1;
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        try
        {
            f = GUIManager.instance?.itemPromptDrop?.font;
        }
        catch { }
    }

    public void PlaceText(List<NiceText> lines)
    {
        foreach (var t in at)
        {
            Destroy(t);
        }
        var objs = lines.Select((l, i) =>
        {
            var textChatObj = new GameObject("TextChat");
            textChatObj.transform.SetParent(c!.transform, false);
            var t = textChatObj.AddComponent<TextMeshProUGUI>();

            t.text = l.s;
            t.color = l.c;
            if (f != null)
            {
                t.font = f;
            }
            t.alignment = TextAlignmentOptions.Top;
            t.fontSize = 32;

            var ct = textChatObj.GetComponent<RectTransform>();
            ct.anchorMin = new Vector2(0.2f, 0.8f);
            ct.anchorMax = new Vector2(0.8f, 0.8f);
            var down = t.fontSize * i * 1.5f;
            ct.offsetMin = new Vector2(0, t.fontSize * 1.5f);
            ct.offsetMax = new Vector2(0, -down);
            return textChatObj;
        });
        at = [.. objs];
    }

    public void Show()
    {
        foreach (var t in at)
        {
            t.SetActive(true);
        }
    }
    public void Hide()
    {
        foreach (var t in at)
        {
            t.SetActive(false);
        }
    }
}

[HarmonyPatch(typeof(GUIManager))]
public static class SoulmateTextPatch
{
    public static DoText? t;
    public static TextSetter? setter;

    public static GameObject? coro;

    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    public static void StartPostfix(GUIManager __instance)
    {
        if (t != null)
        {
            t.ResetTransform(__instance.transform);
            return;
        }
        ;

        coro = new GameObject("coro_1");
        UnityEngine.Object.DontDestroyOnLoad(coro);
        TMP_FontAsset? f = null;
        try
        {
            f = GUIManager.instance?.itemPromptDrop?.font;
        }
        catch
        {
        }
        t = coro.AddComponent<DoText>();
        t.Init(f, __instance.transform);

        setter = coro.AddComponent<TextSetter>();
        setter.Init(t);
    }
}

public class TextSetter : MonoBehaviour
{

    DoText? t;
    bool keyShow = false;

    public void Init(DoText _t)
    {
        t = _t;
    }
    public void ShowCard(List<NiceText> start, float delay)
    {
        var _start = new List<NiceText>(start);
        StartCoroutine(DoShow());
        IEnumerator DoShow()
        {
            t!.Hide();
            yield return new WaitForSeconds(delay);
            t!.PlaceText(_start);
            yield return new WaitForSeconds(10f);
            t!.Hide();
        }
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            keyShow = !keyShow;
            if (keyShow)
            {
                t?.Show();
            }
            else
            {
                t?.Hide();
            }
        }
    }
}