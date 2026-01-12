using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace RandomEvents;

public class EventInterface()
{
    private List<String> start_texts = [];
    private List<String> finish_texts = [];

    public void AddEnableLine(String line)
    {
        Plugin.Log.LogInfo($"Added enable {line}");
        start_texts.Add(line);
    }
    public void AddDisableLine(String line)
    {
        finish_texts.Add(line);
    }
    public void RunInterface(float delay_end, float delay_start)
    {
        Plugin.Log.LogInfo($"Running interface");
        SoulmateTextPatch.setter?.ShowCard(finish_texts, delay_end, start_texts, delay_start);
        start_texts.Clear();
        finish_texts.Clear();
    } 
}