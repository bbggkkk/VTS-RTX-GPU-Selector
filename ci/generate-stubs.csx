// This file generates stub assemblies for CI builds.
// These stubs contain only the type signatures needed for compilation,
// not the actual implementation (which lives in VTube Studio).
//
// Usage: dotnet script generate-stubs.csx
//   or: dotnet run --project . -- generate-stubs

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

void GenerateStub(string assemblyName, string outputDir, Action<ModuleBuilder> defineTypes)
{
    var asmName = new AssemblyName(assemblyName);
    var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Save, outputDir);
    var modBuilder = asmBuilder.DefineDynamicModule(assemblyName, assemblyName + ".dll");
    defineTypes(modBuilder);
    asmBuilder.Save(assemblyName + ".dll");
    Console.WriteLine($"  Generated {assemblyName}.dll");
}

string outDir = Path.Combine("lib", "VTube Studio_Data", "Managed");
Directory.CreateDirectory(outDir);

Console.WriteLine("Generating stub assemblies...");

// We need stubs for types referenced in Plugin.cs:
// From Assembly-CSharp: Logger, PlatformHelper, MXStarter, ConfigManager,
//   WebcamTrackingConfigItem, ItemSelectionWindow, ItemSelectionWindowEntry

// From UnityEngine: Debug, MonoBehaviour, etc. (covered by NuGet or Unity stubs)

// For simplicity, create a single C# stub file that can be compiled
string stubCode = @"
// AUTO-GENERATED STUBS for CI builds
// These provide type signatures only - actual logic lives in VTube Studio

public static class PlatformHelper
{
    public static bool SupportsRTX;
    public static bool IsWindows = true;
    public static bool IsDesktop = true;
}

public class Logger : UnityEngine.MonoBehaviour
{
    public void LogDeviceType() { }
}

public class MXStarter : UnityEngine.MonoBehaviour
{
    public void StartTrackerWithArguments(int a, int b, int c, int d, int e, bool f) { }
    public void TriggerFirewall() { }
}

public static class ConfigManager
{
    public static readonly string C_WEBCAM_QUALITY = ""Config_Webcam_Quality"";
    public static string GetString(string key) { return """"; }
}

public class WebcamTrackingConfigItem : UnityEngine.MonoBehaviour
{
    public void OnItemSelectionEventOver(string eventID, bool success, int selectedID, string selectedString) { }
}

public class ItemSelectionWindow : UnityEngine.MonoBehaviour
{
    public enum ItemSelectionWindowType { TYPE_SELECTION, TYPE_YES_NO }
    public static ItemSelectionWindow Instance() { return null; }
    internal static string EventID(string s) { return ""ItemSelectionEvent_ID_"" + s; }
    internal void ShowForResult(string id, string text, System.Collections.Generic.List<ItemSelectionWindowEntry> items,
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
";

File.WriteAllText(Path.Combine(outDir, "Stubs.cs"), stubCode);
Console.WriteLine($"  Generated Stubs.cs in {outDir}");
Console.WriteLine("Done. Now compile stubs with:");
Console.WriteLine($"  csc /target:library /reference:UnityEngine.dll /out:{outDir}/Assembly-CSharp.dll {outDir}/Stubs.cs");
