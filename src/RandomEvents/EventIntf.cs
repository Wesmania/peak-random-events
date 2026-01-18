using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RandomEvents;

public class EventInterface()
{
    private List<NiceText> start_texts = [];
    private List<NiceText> finish_texts = [];

    public void AddEnableLine(NiceText line)
    {
        start_texts.Add(line);
    }
    public void AddEnableLine(String line)
    {
        AddEnableLine(new NiceText
        {
            s = line,
            c = Color.white,
        });
    }
    public void AddDisableLine(NiceText line)
    {
        finish_texts.Add(line);
    }
    public void AddDisableLine(String line)
    {
        AddDisableLine(new NiceText
        {
            s = line,
            c = Color.white,
        });
    }
    public void RunInterface(float delay_end, float delay_start)
    {
        SoulmateTextPatch.setter?.ShowCard(finish_texts, delay_end, start_texts, delay_start);
        start_texts.Clear();
        finish_texts.Clear();
    }
}