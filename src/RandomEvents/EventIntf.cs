using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RandomEvents;

public class EventInterface()
{
    private List<NiceText> start_texts = [];
    public bool is_first = false;

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
    public void RunInterface(float delay)
    {
        SoulmateTextPatch.setter?.ShowCard(start_texts, delay);
        start_texts.Clear();
    }
}