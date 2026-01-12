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
    private Canvas? SoulmatePrompt;
    private TextMeshProUGUI? text;
    private TMP_FontAsset? darumaDropOneFont;
    private GameObject[] activeText = [];

    public void Init(TMP_FontAsset? ft, Transform _pt)
    {
        darumaDropOneFont = ft;
        transform_ = _pt;
    }

    public void Awake()
    {
        var textChatCanvasObj = new GameObject("SoulmatePrompt");
        textChatCanvasObj.transform.SetParent(transform_, false);
        SoulmatePrompt = textChatCanvasObj.AddComponent<Canvas>();
        SoulmatePrompt.renderMode = RenderMode.ScreenSpaceCamera;

        CanvasScaler canvasScaler = SoulmatePrompt.GetComponent<CanvasScaler>() ?? SoulmatePrompt.gameObject.AddComponent<CanvasScaler>(); ;
        canvasScaler.referencePixelsPerUnit = 100;
        canvasScaler.matchWidthOrHeight = 1;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.scaleFactor = 1;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        try
        {
            darumaDropOneFont = GUIManager.instance?.itemPromptDrop?.font;
        }
        catch { }
    }

    public void PlaceText(List<String> lines)
    {
        foreach (var t in activeText)
        {
            Destroy(t);
        }
        var objs = lines.Select((l, i) =>
        {
            var textChatObj = new GameObject("TextChat");
            textChatObj.transform.SetParent(SoulmatePrompt!.transform, false);
            text = textChatObj.AddComponent<TextMeshProUGUI>();

            text.text = l;
            if (darumaDropOneFont != null)
            {
                text.font = darumaDropOneFont;
            }
            text.alignment = TextAlignmentOptions.Top;
            text.fontSize = 24;

            var ct = textChatObj.GetComponent<RectTransform>();
            ct.anchorMin = new Vector2(0.2f, 0.8f);
            ct.anchorMax = new Vector2(0.8f, 0.8f);
            var down = text.fontSize * i * 1.5f;
            ct.offsetMin = new Vector2(0, text.fontSize * 1.5f);
            ct.offsetMax = new Vector2(0, -down);
            return textChatObj;
        });
        activeText = [.. objs];
    }

    public void Show()
    {
        foreach (var t in activeText)
        {
            t.SetActive(true);
        }
    }
    public void Hide()
    {
        foreach (var t in activeText)
        {
            t.SetActive(false);
        }
    }
}

[HarmonyPatch(typeof(GUIManager))]
public static class SoulmateTextPatch
{
    public static DoText? t;
    public static TextMeshProUGUI? text;
    public static TMP_FontAsset? darumaDropOneFont;
    public static TextSetter? setter;

    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    public static void StartPostfix(GUIManager __instance)
    {
        if (t != null) return;
        try
        {
            darumaDropOneFont = GUIManager.instance?.itemPromptDrop?.font;
        }
        catch { }
        t = __instance.gameObject.AddComponent<DoText>();
        t.Init(darumaDropOneFont, __instance.transform);

        t.PlaceText(["One", "Two", "Three"]);
        setter = __instance.gameObject.AddComponent<TextSetter>();
        setter.Blink(t);
    }
}
public class TextSetter : MonoBehaviour
{
    public void Blink(DoText t)
    {
        StartCoroutine(DoBlink());
        IEnumerator DoBlink()
        {
            while(true) {
                yield return new WaitForSeconds(1f);
                t?.Show();
                yield return new WaitForSeconds(1f);
                t?.Hide();
            }
        }
    }
}