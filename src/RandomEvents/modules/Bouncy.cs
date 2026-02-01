using HarmonyLib;
using UnityEngine;

using System.Collections;
using Newtonsoft.Json.Linq;

namespace RandomEvents;

// Pieces borrowed from CollisionModifier.

[HarmonyPatch(typeof(Character), "OnLand")]
public class CharacterLandPatch
{
    public static bool enabled = false;
    private static bool nested = false;
    private static void Postfix(Character __instance, float sinceGrounded)
    {
        if (!enabled) return;
        if (nested) return;
        nested = true;

        float num = 5000f;
        if ((bool)__instance.data.currentItem && __instance.data.currentItem.TryGetComponent<Parasol>(out var component) && component.isOpen)
        {
            num = 1000f;
        }

        if (__instance.data.fallSeconds > 0f || __instance.refs.afflictions.shouldPassOut)
        {
            num = 0f;
        }

        __instance.AddStamina(0.2f);

        var planeNormal = __instance.data.groundNormal;
        SmoothBounce b = __instance.GetComponent<SmoothBounce>() ?? __instance.gameObject.AddComponent<SmoothBounce>();
        b.StartCoroutine(b.BounceRoutine(num, __instance, planeNormal));
        nested = false;
    }
}
public class SmoothBounce : MonoBehaviour
{
    public IEnumerator BounceRoutine(float knockback, Character character, Vector3 kb)
    {
        float t = 0f;
        while (t < 1f)
        {
            float num3 = knockback;
            character.AddForce(kb * num3 * (1f - t) * Time.fixedDeltaTime);
            character.data.sinceGrounded = Mathf.Clamp(character.data.sinceGrounded, 0f, 0.5f);
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }
}


public class BouncyEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        CharacterLandPatch.enabled = false;
    }

    public void Enable(EventInterface eintf)
    {
        CharacterLandPatch.enabled = true;
        eintf.AddEnableLine("All scouts are bouncy.");
    }

    public JObject to_json()
    {
        return [];
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = _ => new BouncyEvent(),
            FromJson = _ => new BouncyEvent(),
        };
    }
}