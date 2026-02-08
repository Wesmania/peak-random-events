using HarmonyLib;
using UnityEngine;

using System.Collections;
using Newtonsoft.Json.Linq;

namespace RandomEvents;

// Pieces borrowed from CollisionModifier.

[HarmonyPatch(typeof(CharacterMovement), "Land")]
public class CharacterLandPatch
{
    public static bool enabled = false;
    private static bool nested = false;
    private static void Postfix(CharacterMovement __instance, CharacterMovement.PlayerGroundSample bestSample)
    {
        if (!enabled) return;
        if (nested) return;

        var c = __instance.character;
        if (c.data.sinceGrounded <= 0.5f) return;

        nested = true;

        float num = 5000f;
        if ((bool)c.data.currentItem && c.data.currentItem.TryGetComponent<Parasol>(out var component) && component.isOpen)
        {
            num = 1000f;
        }

        if (c.data.fallSeconds > 0f || c.refs.afflictions.shouldPassOut)
        {
            num = 0f;
        }

        c.AddStamina(0.2f);

        var planeNormal = bestSample.normal;
        SmoothBounce b = __instance.GetComponent<SmoothBounce>() ?? __instance.gameObject.AddComponent<SmoothBounce>();
        b.StartCoroutine(b.BounceRoutine(num, c, planeNormal));
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
            if (character.data.GetTargetRagdollControll() != 0.0f)
            {
                float num3 = knockback;
                character.AddForce(kb * num3 * (1f - t) * Time.fixedDeltaTime);
            }
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