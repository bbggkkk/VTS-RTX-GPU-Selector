// AUTO-GENERATED STUBS for CI builds.
// These provide type signatures that Plugin.cs compiles against.
// The actual implementations are in VTube Studio's Assembly-CSharp.dll.
// This file is NOT used at runtime — BepInEx loads the real assemblies.

using System;
using System.Collections.Generic;
using UnityEngine;

public static class PlatformHelper
{
    public static bool SupportsRTX;
    public static bool IsWindows;
    public static bool IsDesktop;
}

public class Logger : MonoBehaviour
{
    public void LogDeviceType() { }
}

public class MXStarter : MonoBehaviour
{
    public void StartTrackerWithArguments(int a, int b, int c, int d, int e, bool f) { }
    public void TriggerFirewall() { }
}

public static class ConfigManager
{
    public static readonly string C_WEBCAM_QUALITY = "Config_Webcam_Quality";
    public static string GetString(string key) { return ""; }
}

public class WebcamTrackingConfigItem : MonoBehaviour
{
    public virtual void OnItemSelectionEventOver(string eventID, bool success, int selectedID, string selectedString) { }
}

public class ItemSelectionWindow : MonoBehaviour
{
    public enum ItemSelectionWindowType { TYPE_SELECTION, TYPE_YES_NO }

    private static ItemSelectionWindow _instance;
    public static ItemSelectionWindow Instance() { return _instance; }

    internal static string EventID(string s) { return "ItemSelectionEvent_ID_" + s; }

    internal void ShowForResult(string id, string text, List<ItemSelectionWindowEntry> items,
        ItemSelectionWindowType type, bool allowCancel, bool allowFilter,
        string helpString = null, string helpLink = null, bool showButtons = false,
        string yesId = null, string noId = null, object toggleInfo = null, object height = null) { }
}

public class ItemSelectionWindowEntry
{
    public string title;
    public string subTitle;
    public bool selectable;
    public bool preSelected;
}
