using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Win32;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace VTSRTXGPUSelector
{
    public class GpuInfo
    {
        public int CudaIndex;
        public string Name;
        public string RegistryId;

        public override string ToString()
        {
            return $"[CUDA:{CudaIndex}] {Name}";
        }
    }

    public static class GpuState
    {
        public static List<GpuInfo> RtxGpus = new List<GpuInfo>();
        public static int SelectedIndex = 0;
        public static ConfigEntry<int> SavedGpuIndex;
        public static ConfigEntry<string> SavedGpuName;

        /// <summary>
        /// EventID for GPU selection in VTS's ItemSelectionWindow.
        /// Must use "ItemSelectionEvent_ID_" prefix (checked by ShowForResult).
        /// </summary>
        public static readonly string GpuSelectionEventID =
            "ItemSelectionEvent_ID_RTXUnlocker_GPU_" + Guid.NewGuid().ToString();

        public static void EnumerateGpus()
        {
            RtxGpus.Clear();

            try
            {
                string basePath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";
                using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(basePath))
                {
                    if (baseKey == null) return;

                    int cudaIdx = 0;
                    foreach (string subKeyName in baseKey.GetSubKeyNames())
                    {
                        if (!int.TryParse(subKeyName, out _)) continue;

                        using (RegistryKey adapterKey = baseKey.OpenSubKey(subKeyName))
                        {
                            if (adapterKey == null) continue;

                            string driverDesc = adapterKey.GetValue("DriverDesc")?.ToString() ?? "";
                            if (string.IsNullOrEmpty(driverDesc)) continue;

                            string providerName = adapterKey.GetValue("ProviderName")?.ToString() ?? "";
                            bool isNvidia = driverDesc.ToLowerInvariant().Contains("nvidia") ||
                                            providerName.ToLowerInvariant().Contains("nvidia");

                            if (!isNvidia) continue;

                            var gpu = new GpuInfo
                            {
                                CudaIndex = cudaIdx,
                                Name = driverDesc,
                                RegistryId = subKeyName
                            };
                            cudaIdx++;

                            Debug.Log($"[RTXUnlocker] Found NVIDIA GPU: {gpu}");

                            if (driverDesc.ToLowerInvariant().Contains("rtx"))
                            {
                                RtxGpus.Add(gpu);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[RTXUnlocker] Failed to enumerate GPUs: " + ex.Message);
            }

            // Restore saved selection
            if (SavedGpuName != null && !string.IsNullOrEmpty(SavedGpuName.Value))
            {
                for (int i = 0; i < RtxGpus.Count; i++)
                {
                    if (RtxGpus[i].Name == SavedGpuName.Value)
                    {
                        SelectedIndex = i;
                        Debug.Log($"[RTXUnlocker] Restored saved GPU: {RtxGpus[i]}");
                        return;
                    }
                }
            }

            if (RtxGpus.Count > 0)
                Debug.Log($"[RTXUnlocker] Default GPU: {RtxGpus[SelectedIndex]}");
        }

        public static GpuInfo GetSelectedGpu()
        {
            if (RtxGpus.Count == 0 || SelectedIndex < 0 || SelectedIndex >= RtxGpus.Count)
                return null;
            return RtxGpus[SelectedIndex];
        }

        public static void SaveSelection()
        {
            var gpu = GetSelectedGpu();
            if (gpu != null && SavedGpuIndex != null && SavedGpuName != null)
            {
                SavedGpuIndex.Value = SelectedIndex;
                SavedGpuName.Value = gpu.Name;
            }
        }

        /// <summary>
        /// Show GPU selection using VTS's native ItemSelectionWindow.
        /// </summary>
        public static void ShowGpuSelectionWindow()
        {
            if (RtxGpus.Count == 0) return;

            var window = ItemSelectionWindow.Instance();
            if (window == null)
            {
                Debug.LogWarning("[RTXUnlocker] ItemSelectionWindow not found.");
                return;
            }

            var entries = new List<ItemSelectionWindowEntry>();
            for (int i = 0; i < RtxGpus.Count; i++)
            {
                entries.Add(new ItemSelectionWindowEntry
                {
                    title = RtxGpus[i].Name,
                    subTitle = $"CUDA Device {RtxGpus[i].CudaIndex}",
                    selectable = true,
                    preSelected = (i == SelectedIndex)
                });
            }

            // ShowForResult is internal, so use reflection
            var showMethod = AccessTools.Method(typeof(ItemSelectionWindow), "ShowForResult");
            if (showMethod == null)
            {
                Debug.LogError("[RTXUnlocker] Could not find ShowForResult method.");
                return;
            }

            showMethod.Invoke(window, new object[] {
                GpuSelectionEventID,
                "RTX GPU 선택 (ExpressionApp용)",
                entries,
                ItemSelectionWindow.ItemSelectionWindowType.TYPE_SELECTION,
                true,  // allowCancel
                false, // allowFilter
                null,  // helpString
                null,  // helpLink
                false, // showItemTypeFilterButtons
                null,  // yesOverrideStringID
                null,  // noOverrideStringID
                null,  // genericToggleInfo
                null   // heightForNormalListWindow
            });

            Debug.Log("[RTXUnlocker] GPU selection window opened.");
        }
    }

    // =========================================================================
    // BepInEx Plugin Entry Point
    // =========================================================================

    [BepInPlugin("com.bbggkkk.vtsrtxgpuselector", "VTS RTX GPU Selector", "2.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        private const string MyPluginInfo_PLUGIN_NAME = "VTS RTX GPU Selector";
        private void Awake()
        {
            GpuState.SavedGpuIndex = Config.Bind("GPU", "SelectedIndex", 0,
                "Index of the selected RTX GPU");
            GpuState.SavedGpuName = Config.Bind("GPU", "SelectedGpuName", "",
                "Name of the selected RTX GPU");

            GpuState.EnumerateGpus();

            var harmony = new Harmony("com.vts.rtxgpuselector");
            harmony.PatchAll();

            Logger.LogInfo($"VTS RTX GPU Selector v2.2 loaded. {GpuState.RtxGpus.Count} RTX GPU(s) found.");
        }
    }

    // =========================================================================
    // Harmony Patches
    // =========================================================================

    /// <summary>
    /// Override RTX detection: check ALL system GPUs, not just rendering GPU.
    /// </summary>
    [HarmonyPatch(typeof(global::Logger), "LogDeviceType")]
    public static class LogDeviceTypePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (PlatformHelper.SupportsRTX)
            {
                Debug.Log("[RTXUnlocker] RTX already supported via rendering GPU.");
                return;
            }

            if (!PlatformHelper.IsWindows || !PlatformHelper.IsDesktop)
                return;

            if (GpuState.RtxGpus.Count > 0)
            {
                PlatformHelper.SupportsRTX = true;
                Debug.Log($"[RTXUnlocker] Enabled RTX. GPU: {GpuState.GetSelectedGpu()}");
            }
            else
            {
                Debug.Log("[RTXUnlocker] No RTX GPU found.");
            }
        }
    }

    /// <summary>
    /// Inject CUDA_VISIBLE_DEVICES when ExpressionApp launches.
    /// </summary>
    [HarmonyPatch(typeof(MXStarter), "StartTrackerWithArguments")]
    public static class StartTrackerPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            var gpu = GpuState.GetSelectedGpu();
            if (gpu != null)
            {
                Environment.SetEnvironmentVariable("CUDA_VISIBLE_DEVICES", gpu.CudaIndex.ToString());
                Debug.Log($"[RTXUnlocker] CUDA_VISIBLE_DEVICES={gpu.CudaIndex} ({gpu.Name})");
            }
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            Environment.SetEnvironmentVariable("CUDA_VISIBLE_DEVICES", null);
        }
    }

    [HarmonyPatch(typeof(MXStarter), "TriggerFirewall")]
    public static class TriggerFirewallPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            var gpu = GpuState.GetSelectedGpu();
            if (gpu != null)
                Environment.SetEnvironmentVariable("CUDA_VISIBLE_DEVICES", gpu.CudaIndex.ToString());
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            Environment.SetEnvironmentVariable("CUDA_VISIBLE_DEVICES", null);
        }
    }

    /// <summary>
    /// Hook quality/type selection events:
    /// - When NVIDIA quality (Level_6/Level_8) is selected → show GPU selection popup
    /// - When our GPU selection event fires → save the chosen GPU
    /// </summary>
    [HarmonyPatch(typeof(WebcamTrackingConfigItem), "OnItemSelectionEventOver")]
    public static class OnItemSelectionPatch
    {
        [HarmonyPostfix]
        public static void Postfix(string eventID, bool success, int selectedID, string selectedString)
        {
            // Handle our GPU selection result
            if (eventID == GpuState.GpuSelectionEventID)
            {
                if (success && selectedID >= 0 && selectedID < GpuState.RtxGpus.Count)
                {
                    GpuState.SelectedIndex = selectedID;
                    GpuState.SaveSelection();
                    Debug.Log($"[RTXUnlocker] GPU set: {GpuState.RtxGpus[selectedID]}");
                }
                return;
            }

            // When NVIDIA quality is selected, chain-show GPU selection
            if (!success || GpuState.RtxGpus.Count == 0) return;

            string quality = ConfigManager.GetString(ConfigManager.C_WEBCAM_QUALITY);
            if (quality == "Level_6" || quality == "Level_8")
            {
                Debug.Log("[RTXUnlocker] NVIDIA tracking selected. Showing GPU picker.");
                GpuState.ShowGpuSelectionWindow();
            }
        }
    }
}
