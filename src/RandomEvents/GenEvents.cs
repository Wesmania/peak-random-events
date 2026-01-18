using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using pworld.Scripts.Extensions;
using Sirenix.Utilities;

namespace RandomEvents;

// Game does not define Caldera in its Biome enum. WTF!?
public enum OurBiome
{
    Shore = 0,
    Tropics = 1,
    Roots = 2,
    Alpine = 3,
    Mesa = 4,
    Caldera = 5,
    Kiln = 6,
    Peak = 7,
}

public struct IEventFactory
{
    // NOTE: this factory should NOT activate the event. Events will be created and discarded at random selection.
    public Func<OurBiome, IEvent> New;
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
    DOOM_MODE = 7,
    FROSTBITE = 8,
    BOUNCY = 9,
    ALL_SHROOMS = 10,
};

public static class BiomeConv
{
    public static OurBiome FromSegmentBiome(Segment s, Biome.BiomeType b)
    {
        switch (s)
        {
            case Segment.Caldera:
                return OurBiome.Caldera;
            case Segment.TheKiln:
                return OurBiome.Kiln;
            case Segment.Peak:
                return OurBiome.Peak;
            case Segment.Tropics:
                if (b == Biome.BiomeType.Roots) return OurBiome.Roots;
                else return OurBiome.Tropics;
            case Segment.Alpine:
                if (b == Biome.BiomeType.Alpine) return OurBiome.Alpine;
                else return OurBiome.Mesa;
            case Segment.Beach:
            default:
                return OurBiome.Shore;
        }
    }
}
public interface IEvent
{
    public JObject to_json();
    public void Enable(EventInterface eintf);
    public void Disable(EventInterface eintf);
    public HashSet<OurBiome> ZoneLimit()
    {
        return [];
    }
    public HashSet<AllEvents> ExcludeEvents()
    {
        return [];
    }
    public static Dictionary<AllEvents, IEventFactory> all_events = new()
    {
        { AllEvents.GREAT_MAGICIAN, GreatMagicianEvent.factory() },
        { AllEvents.EMISSIONS, HeatEmissionEvent.factory() },
        { AllEvents.CURSE_DAMAGE, CurseDamageEvent.factory() },
        { AllEvents.FREE_BALLOON, FreeBalloonEvent.factory() },
        { AllEvents.NO_SCUTTLING, NoScuttlingEvent.factory() },
        { AllEvents.SUPER_RESCUE_HOOKS, SuperRescueHookEvent.factory() },
        { AllEvents.DOOM_MODE, DoomModeEvent.factory() },
        { AllEvents.FROSTBITE, FrostbiteEvent.factory() },
        { AllEvents.BOUNCY, BouncyEvent.factory() },
        { AllEvents.ALL_SHROOMS, AllShroomsEvent.factory() },
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

    public String? PickNewEvents(bool is_first, OurBiome biome)
    {
        if (!Messages.IsMaster())
        {
            return null;
        }
        var all_e = Enum.GetValues(typeof(AllEvents)).Cast<AllEvents>().ToList();

        // Select all candidates first.
        var all = all_e.Select(e => (id: e, e: IEvent.all_events[e].New(biome)))
                        .Where(e => e.e.ZoneLimit().Count == 0 || e.e.ZoneLimit().Contains(biome)).ToList();
        all.Shuffle();

        // Now filter conflicting candidates, in order.
        HashSet<AllEvents> excludes = [];
        HashSet<AllEvents> already_selected = [];
        var cands = all.Where(e =>
                        {
                            // Previous ones on the list conflict with it.
                            if (excludes.Contains(e.id)) return false;

                            var new_excludes = e.e.ExcludeEvents();
                            // This one has no conflicts.
                            if (!new_excludes.Any())
                            {
                                already_selected.Add(e.id);
                                return true;
                            }

                            // This one conflicts with previous ones on the list.
                            if (new_excludes!.Intersect(already_selected).Count() > 0) return false;

                            // No conflicts, expand excludes and select.
                            excludes.UnionWith(new_excludes);
                            already_selected.Add(e.id);
                            return true;
                        }).ToList();

        Dictionary<AllEvents, JObject> e = cands.Take(Math.Min(EVENT_COUNT, all.Count()))
                                              .Select(e => (id: e.id, json: e.e.to_json()))
                                              .ToDictionary(e => e.id, e => e.json);
        EnableMessage em = new()
        {
            events = e,
            is_first = is_first
        };
        return JsonConvert.SerializeObject(em);
    }
    public void UnloadEvents(EventInterface eintf)
    {
        foreach (var ev in events)
        {
            ev.Disable(eintf);
        }
        events = [];
    }
    public void LoadNewEvents(String s, EventInterface eintf)
    {
        // FIXME handle errors
        EnableMessage em = JsonConvert.DeserializeObject<EnableMessage>(s)!;
        is_first = em.is_first;
        eintf.is_first = is_first;
        events = em.events.Select(kv => IEvent.all_events[kv.Key].FromJson(kv.Value)).ToList();
        foreach (var ev in events)
        {
            ev.Enable(eintf);
        }
    }
}

