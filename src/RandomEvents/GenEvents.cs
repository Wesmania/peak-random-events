using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using pworld.Scripts.Extensions;

namespace RandomEvents;

public struct IEventFactory
{
    public Func<IEvent> New;
    public Func<JObject, IEvent> FromJson;
}

public enum AllEvents
{
    GREAT_MAGICIAN = 1,
    EMISSIONS = 2,
    CURSE_DAMAGE = 3,
    FREE_BALLOON = 4,
    NO_SCUTTLING = 5,
    SUPER_RESCUE_HOOKS = 6,
};
public interface IEvent
{
    public JObject to_json();
    public void Enable(EventInterface eintf);
    public void Disable(EventInterface eintf);

    public static Dictionary<AllEvents, IEventFactory> all_events = new()
    {
        { AllEvents.GREAT_MAGICIAN, GreatMagicianEvent.factory() },
        { AllEvents.EMISSIONS, HeatEmissionEvent.factory() },
        { AllEvents.CURSE_DAMAGE, CurseDamageEvent.factory() },
        { AllEvents.FREE_BALLOON, FreeBalloonEvent.factory() },
        { AllEvents.NO_SCUTTLING, NoScuttlingEvent.factory() },
        { AllEvents.SUPER_RESCUE_HOOKS, SuperRescueHookEvent.factory() },
    };
}
public struct EnableMessage
{
    public Dictionary<AllEvents, JObject> events;
    public bool is_first;
}

public class PickEvents
{
    static int EVENT_COUNT = 2;
    List<IEvent> events = [];
    public bool is_first = true;

    public String? PickNewEvents(bool is_first)
    {
        if (!Messages.IsMaster())
        {
            return null;
        }
        Plugin.Log.LogInfo($"Sending new messages");
        List<AllEvents> all = [.. Enum.GetValues(typeof(AllEvents)).Cast<AllEvents>()];
        all.Shuffle();
        Dictionary<AllEvents, JObject> e = all.Take(Math.Min(EVENT_COUNT, all.Count()))
                                              .Select(e => (e, IEvent.all_events[e].New().to_json()))
                                              .ToDictionary(e => e.Item1, e => e.Item2);
        EnableMessage em = new()
        {
            events = e,
            is_first = is_first
        };
        return JsonConvert.SerializeObject(em);
    }
    public void UnloadEvents(EventInterface eintf)
    {
        Plugin.Log.LogInfo($"Unloading events");
        foreach (var ev in events)
        {
            ev.Disable(eintf);
        }
        events = [];
    }
    public void LoadNewEvents(String s, EventInterface eintf)
    {
        Plugin.Log.LogInfo($"Loading new events");
        // FIXME handle errors
        EnableMessage em = JsonConvert.DeserializeObject<EnableMessage>(s)!;
        is_first = em.is_first;
        events = em.events.Select(kv => IEvent.all_events[kv.Key].FromJson(kv.Value)).ToList();
        foreach (var ev in events)
        {
            ev.Enable(eintf);
        }
        Plugin.Log.LogInfo($"Loaded new events");
    }
}

