using HarmonyLib;
using Newtonsoft.Json.Linq;

namespace RandomEvents;


[HarmonyPatch(typeof(CharacterMovement), "GetMovementForce")]
public class MovementSpeedPatch
{
    public static bool doom_mode = false;
    private static bool multipler_applied = false;
    private static void Prefix(CharacterMovement __instance)
    {
        if (doom_mode && !multipler_applied)
        {
            __instance.movementModifier *= 2.5f;
            multipler_applied = true;
        }
        if (!doom_mode && multipler_applied)
        {
            __instance.movementModifier /= 2.5f;
            multipler_applied = false;
        }
    }
}

[HarmonyPatch(typeof(CharacterMovement), "TryToJump")]
public class JumpPatch
{
    public static bool doom_mode = false;
    private static bool Prefix(CharacterMovement __instance)
    {
        return !doom_mode;
    }
}

public class DoomModeEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        MovementSpeedPatch.doom_mode = false;
        JumpPatch.doom_mode = false;
    }

    public void Enable(EventInterface eintf)
    {
        eintf.AddEnableLine("DOOM mode!");
        MovementSpeedPatch.doom_mode = true;
        JumpPatch.doom_mode = true;
    }

    public JObject to_json()
    {
        return [];
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = () => new DoomModeEvent(),
            FromJson = _ => new DoomModeEvent(),
        };
    }
}