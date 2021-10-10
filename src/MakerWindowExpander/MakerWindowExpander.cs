using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChaCustom;
using HarmonyLib;
using KKAPI;
using UnityEngine;
using UnityEngine.UI;

namespace KK_K7A_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class MakerWindowExpander : BaseUnityPlugin
    {
        public const string GUID = "com.k7a.bepinex.makerwindowexpander";
        public const string PluginName = "Maker Window Expander";
        public const string PluginNameInternal = "KK_MakerWindowExpander";
        public const string Version = "1.0.0";

        const int MinColNum = 3;
        const int MaxColNum = 10;
        const float MinScale = 0.5f;
        const float MaxScale = 1.0f;
        const int DefaultColCount = 3;
        const int ItemWidth = 120;

        static ConfigEntry<int> ColumnsCount { get; set; }
        static ConfigEntry<float> WindowScale { get; set; }
        static ConfigEntry<bool> ExpandHeight { get; set; }
        static Dictionary<CustomSelectListCtrl, Vector2> DefaultSizeCaches { get; set; }
        
        internal static new ManualLogSource Logger;

        public MakerWindowExpander()
        {
            Logger = base.Logger;
            
            Harmony.CreateAndPatchAll(typeof(MakerWindowExpander));

            ColumnsCount = Config.Bind("Maker Window Settings", "Columns Count", 6,
                new ConfigDescription("Maker custom select window columns count", new AcceptableValueRange<int>(MinColNum, MaxColNum)));
            WindowScale = Config.Bind("Maker Window Settings", "Window Scale",  1.0f,
                new ConfigDescription("Maker custom select window scale", new AcceptableValueRange<float>(MinScale, MaxScale)));
            ExpandHeight = Config.Bind("Maker Window Settings", "Expand Height", true,
                new ConfigDescription("Expand window height"));
            ColumnsCount.SettingChanged += OnSettingChanged;
            WindowScale.SettingChanged += OnSettingChanged;
            ExpandHeight.SettingChanged += OnSettingChanged;

            DefaultSizeCaches = new Dictionary<CustomSelectListCtrl, Vector2>();
            KKAPI.Maker.MakerAPI.MakerExiting += OnMakerExit;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CustomSelectListCtrl), "Start")]
        static void OnStartHook(CustomSelectListCtrl __instance) => SetWindowSize(__instance);

        static void OnMakerExit(object sender, System.EventArgs e)
        {
            DefaultSizeCaches.Clear();
        }

        static void OnSettingChanged(object sender, System.EventArgs e)
        {
            foreach (var ctrl in DefaultSizeCaches.Keys)
            {
                SetWindowSize(ctrl);
            }
        }

        static void SetWindowSize(CustomSelectListCtrl ctrl)
        {
            var rect = ctrl.csWindow.GetComponent<RectTransform>();
            
            if (!DefaultSizeCaches.ContainsKey(ctrl))
            {
                DefaultSizeCaches[ctrl] = rect.sizeDelta;
            }
            else
                rect.sizeDelta = DefaultSizeCaches[ctrl];

            rect.sizeDelta = new Vector2(rect.sizeDelta.x + ItemWidth * (ColumnsCount.Value - DefaultColCount), ExpandHeight.Value ? rect.sizeDelta.y * (1.0f / WindowScale.Value) : rect.sizeDelta.y);
            rect.localScale = new Vector2(WindowScale.Value, WindowScale.Value);
            
            if (ExpandHeight.Value)
            {
                var listArea = ctrl.transform.Find("WinRect/ListArea");
                var listRect = listArea.GetComponent<RectTransform>();
                var listLayout = listArea.GetComponent<LayoutElement>();
                listLayout.preferredHeight = rect.sizeDelta.y - 90f;
                listRect.sizeDelta = new Vector2(listRect.sizeDelta.x, listLayout.preferredHeight);
            }
        }
    }
}
