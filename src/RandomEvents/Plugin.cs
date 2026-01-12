using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace RandomEvents;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    public static ManualLogSource Log = null!;

    private void Awake()
    {

        Harmony harmony = new("com.github.Wesmania.RandomEvents");
        try
        {
            harmony.PatchAll();
        }
        catch (System.Exception ex)
        {
            Log.LogError($"Failed to load mod: {ex}");
        }

        Log = Logger;
        Log.LogInfo($"Plugin {Name} is loaded!");
    }
}
