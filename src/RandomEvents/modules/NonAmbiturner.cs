using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Zorro.ControllerSupport;
using Zorro.Settings;

namespace RandomEvents;

[HarmonyPatch(typeof(CharacterMovement), "CameraLook")]
class NonAmbiturner
{
    public static bool enabled = false;
    public static bool right = false;
    private static bool Prefix(CharacterMovement __instance)
    {
        if (!enabled) return true;

        var character = __instance.character;
        float num = ((InputHandler.GetCurrentUsedInputScheme() == InputScheme.KeyboardMouse) ? __instance.mouseSensSetting.Value : __instance.controllerSensSetting.Value);
        float vx = character.input.lookInput.x * num * (float)((__instance.invertXSetting.Value == OffOnMode.OFF) ? 1 : (-1));

        if ((right && vx < 0) || (!right && vx > 0))
        {
            vx *= 0.04f;
        }
        character.data.lookValues.x += vx;
        character.data.lookValues.y += character.input.lookInput.y * num * (float)((__instance.invertYSetting.Value == OffOnMode.OFF) ? 1 : (-1));
        character.data.lookValues.y = Mathf.Clamp(character.data.lookValues.y, -85f, 85f);
        character.RecalculateLookDirections();
        return false;
    }
}


public class NonAmbiturnerEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        NonAmbiturner.enabled = false;
    }

    public void Enable(EventInterface eintf)
    {
        eintf.AddEnableLine("Scouts are no longer ambiturners.");

        NonAmbiturner.enabled = true;
        NonAmbiturner.right = UnityEngine.Random.value >= 0.5f;
    }

    public JObject to_json()
    {
        return [];
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = _ => new NonAmbiturnerEvent(),
            FromJson = _ => new NonAmbiturnerEvent(),
        };
    }
}