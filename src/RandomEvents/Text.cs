using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace RandomEvents;

public class DoText : MonoBehaviour
{
    private Transform? transform_;
    private Canvas? c;
    private TMP_FontAsset? f;
    private GameObject[] at = [];

    public void Init(TMP_FontAsset? ft, Transform _pt)
    {
        f = ft;
        transform_ = _pt;
    }

    public void Awake()
    {
        var co = new GameObject("SoulmatePrompt");
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

        try
        {
            f = GUIManager.instance?.itemPromptDrop?.font;
        }
        catch { }
    }

    public void PlaceText(List<String> lines)
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

            t.text = l;
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

    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    public static void StartPostfix(GUIManager __instance)
    {
        if (t != null) return;
        TMP_FontAsset? f = null;
        try
        {
            f = GUIManager.instance?.itemPromptDrop?.font;
        }
        catch { }
        t = __instance.gameObject.AddComponent<DoText>();
        t.Init(f, __instance.transform);

        setter = __instance.gameObject.AddComponent<TextSetter>();
        setter.Init(t);
    }
}
public class TextSetter : MonoBehaviour
{

    DoText? t;

    public void Init(DoText _t)
    {
        t = _t;
    }
    public void ShowCard(List<String> end, float delay_end, List<String> start, float delay_start)
    {
        var _start = new List<String>(start);
        var _end = new List<String>(end);
        StartCoroutine(DoShow());
        IEnumerator DoShow()
        {
            while(true) {
                t!.PlaceText(_end);
                yield return new WaitForSeconds(delay_end);
                t!.Hide();
                yield return new WaitForSeconds(1f);
                t!.PlaceText(_start);
                yield return new WaitForSeconds(delay_start);
                t!.Hide();
            }
        }
    }
}