using System;
using System.Collections;
using System.Threading.Tasks;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Photon.Voice.Unity.Demos;
using Photon.Voice.Unity.Demos.DemoVoiceUI;
using UnityEngine;
using UnityEngine.Networking;

namespace RandomEvents;


[HarmonyPatch(typeof(CharacterMovement), "GetMovementForce")]
public class MovementSpeedPatch
{
    public static bool doom_mode = false;
    private static void Postfix(CharacterMovement __instance, ref float __result)
    {
        if (doom_mode)
        {
            __result *= 2.5f;
        }
    }
}
[HarmonyPatch(typeof(CharacterMovement), "TryToJump")]
public class JumpPatch
{
    public static bool doom_mode = false;
    private static bool Prefix(CharacterMovement __instance)
    {
        bool? webbed = __instance.character.refs?.afflictions.isWebbed;
        if (webbed.HasValue && webbed.Value) return true;

        return !doom_mode;
    }
}

public class DoomMusic : MonoBehaviour
{
    private AudioClip? e1m1;
    private GameObject? musicObj;
    public void Start()
    {
        StartCoroutine(GetMusic());
    }

    IEnumerator GetMusic()
    {
        string url = "file://" + Application.dataPath + "/../BepInEx/plugins/RandomEvents_e1m1.mp3";
        using UnityWebRequest e1m1r = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
        yield return e1m1r.SendWebRequest();
        if (e1m1r.result == UnityWebRequest.Result.Success)
        {
            e1m1 = DownloadHandlerAudioClip.GetContent(e1m1r);
        }
        else
        {
            Plugin.Log.LogError("No doom music!");
        }
    }
    public void TriggerMusic()
    {
        StartCoroutine(DoTriggerMusic());
    }
    IEnumerator DoTriggerMusic()
    {
        yield return new WaitForSeconds(15);
        if (e1m1 != null)
        {
            if (musicObj != null)
            {
                Destroy(musicObj);
            }
            musicObj = new GameObject("RandomEventsDoomModeMusic");
            var audioSource = musicObj.AddComponent<AudioSource>();
            audioSource.clip = e1m1;
            audioSource.volume = 0.5f;
            audioSource.Play();
        }
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
        GlobalBehaviours.doom_music?.TriggerMusic();
    }

    public JObject to_json()
    {
        return [];
    }
    public static IEventFactory factory()
    {
        return new IEventFactory
        {
            New = _ => new DoomModeEvent(),
            FromJson = _ => new DoomModeEvent(),
        };
    }
}