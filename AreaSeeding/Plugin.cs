using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using Wish;
using UnityEngine;
using System.ComponentModel;
using static AreaSeeding.Plugin;

namespace AreaSeeding
{
    [BepInPlugin(PluginGuid, PluginName, PluginVer)]
    public class Plugin : BaseUnityPlugin
    {
        public static readonly int MaxLength = 150;
        public static ManualLogSource logger;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<KeyboardShortcut> activeKey;
        public static ConfigEntry<int> width;
        public static ConfigEntry<int> height;
        public static ConfigEntry<KeyboardShortcut> rotateKey;
        public static ConfigEntry<KeyboardShortcut> increaseWidthKey;
        public static ConfigEntry<KeyboardShortcut> increaseHeightKey;
        public static ConfigEntry<KeyboardShortcut> decreaseWidthKey;
        public static ConfigEntry<KeyboardShortcut> decreaseHeightKey;
        public static ConfigEntry<GrowthStageOptions> growthStageOption;
        public static ConfigEntry<bool> destroyOtherCrops;

        public static String prevAction;
        public static Vector2Int prevPos;
        public static int prevId;
        public static bool playAudio;

        private const string PluginGuid = "niratokage125.sunhaven.AreaSeeding";
        private const string PluginName = "AreaSeeding";
        private const string PluginVer = "1.0.3";
        private void Awake()
        {
            logger = Logger;
            modEnabled = Config.Bind<bool>("Common", "Mod Enabled", true, "Set to false to disable this mod.");
            width = Config.Bind<int>("Common", "Area Width", 5, new ConfigDescription("Description", new AcceptableValueRange<int>(1, MaxLength)));
            height = Config.Bind<int>("Common", "Area Height", 5, new ConfigDescription("Description", new AcceptableValueRange<int>(1, MaxLength)));
            activeKey = Config.Bind<KeyboardShortcut>("Key Config", "Active Key", new KeyboardShortcut(KeyCode.LeftControl), "Seed: ActiveKey + LeftClick with Seeds. Destroy: ActiveKey + RightClick with Seeds.");
            rotateKey = Config.Bind<KeyboardShortcut>("Key Config", "Rotate Key", new KeyboardShortcut(KeyCode.Z, new[] {KeyCode.LeftControl}), "Swap With and Height");
            increaseWidthKey = Config.Bind<KeyboardShortcut>("Key Config", "Increase Width Key", new KeyboardShortcut(KeyCode.RightArrow, new[] { KeyCode.LeftControl }), "Increase Area Width");
            increaseHeightKey = Config.Bind<KeyboardShortcut>("Key Config", "Increase Height Key", new KeyboardShortcut(KeyCode.UpArrow, new[] { KeyCode.LeftControl }), "Increase Area Height");
            decreaseWidthKey = Config.Bind<KeyboardShortcut>("Key Config", "Decrease Width Key", new KeyboardShortcut(KeyCode.LeftArrow, new[] { KeyCode.LeftControl }), "Decrease Area Width");
            decreaseHeightKey = Config.Bind<KeyboardShortcut>("Key Config", "Decrease Height Key", new KeyboardShortcut(KeyCode.DownArrow, new[] { KeyCode.LeftControl }), "Decrease Area Height");
            growthStageOption = Config.Bind<GrowthStageOptions>("Destroy Settings","Growth Stage Option", GrowthStageOptions.Day1Only);
            destroyOtherCrops = Config.Bind<bool>("Destroy Settings", "Destroy Other Crops", false, "If enabled, destroy crops even if they are different from the seed");

            var harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo($"Plugin {PluginGuid} v{PluginVer} is loaded");
        }

        public enum GrowthStageOptions
        {
            [Description("Disable Destroy")]
            None,
            [Description("Destroy Crops Planted Today")]
            Day1Only,
            [Description("Destroy Crops Not Fully Grown")]
            NotFullyGrown,
            [Description("ALL [Even Fully Grown Crops Drop SEEDS!!!]")]
            All
        }
    }
}
