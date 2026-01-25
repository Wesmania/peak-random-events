using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RandomEvents;

[HarmonyPatch(typeof(CharacterClimbing), "StartClimbRpc")]
public class CharacterClimbingPatch
{
    public static bool allow_scuttling = true;
    private static bool Prefix(CharacterClimbing __instance, Vector3 climbPos, Vector3 climbNormal)
    {
        // Copied from original implementation
        var i = __instance;

        float num = 0f;
        if (i.character.data.hasClimbedSinceGrounded)
        {
            Vector3 vector = i.GetVisualClimberPos(climbPos, climbNormal) - (i.character.Center + Vector3.up * 0.5f);
            vector = Vector3.ProjectOnPlane(vector * 1.5f, climbNormal);
            float a = vector.magnitude;

            // Our anti-scuttling changes.
            float cmp = allow_scuttling ? 0f : -a / 1.3f;
            if (Vector3.Dot(vector, Vector3.up) < cmp)
            {
                a = 0f;
            }

            a = Mathf.Max(a, 0.1f);
            i.character.UseStamina(0.15f * a);
            if (i.character.OutOfStamina())
            {
                num += (0f - a) * i.outOfStamAttachSlide;
            }
        }

        if (i.character.data.avarageVelocity.y < 0f)
        {
            num += i.character.data.avarageVelocity.y * 1.5f;
        }

        i.character.OutOfStamina();
        i.playerSlide = new Vector2(i.playerSlide.x, num);
        i.character.data.climbPos = climbPos;
        i.character.data.climbNormal = climbNormal;
        i.character.data.hasClimbedSinceGrounded = true;
        i.character.data.isClimbing = true;
        i.character.data.isGrounded = false;
        i.character.data.sinceStartClimb = 0f;
        i.character.OnStartClimb();

        return false;
    }
}

public class NoScuttlingEvent : IEvent
{
    public void Disable(EventInterface eintf)
    {
        CharacterClimbingPatch.allow_scuttling = true;
    }

    public void Enable(EventInterface eintf)
    {
        eintf.AddEnableLine("No scuttling!");
        CharacterClimbingPatch.allow_scuttling = false;
    }

    public JObject to_json()
    {
        return [];
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = _ => new NoScuttlingEvent(),
            FromJson = _ => new NoScuttlingEvent(),
        };
    }
}