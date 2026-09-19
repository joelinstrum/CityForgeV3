using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Player;
public static class ReleaseLotCheck
{
 public static void Run()
 {
  var output = Path.GetFullPath("ReleaseScripts"); Directory.CreateDirectory(output);
  var result = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings {
   target = BuildTarget.StandaloneOSX, group = BuildTargetGroup.Standalone,
   options = ScriptCompilationOptions.None }, output);
  if (result.assemblies == null || result.assemblies.Count == 0) throw new Exception("Player compilation failed");
  var file = Directory.GetFiles(output, "CityForgeV3.Runtime.dll", SearchOption.AllDirectories).Single();
  var bytes = File.ReadAllBytes(file);
  var assembly = Assembly.Load(bytes);
  var type = assembly.GetType("CityForgeV3.UI.CityForgeApp", true);
  if ((bool)type.GetProperty("TestLotToolsAvailable", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null))
   throw new Exception("Testing bypass enabled in release");
  var needle = System.Text.Encoding.Unicode.GetBytes("district-mode-lots");
  for (int i = 0; i <= bytes.Length - needle.Length; i++)
   if (bytes.Skip(i).Take(needle.Length).SequenceEqual(needle)) throw new Exception("Testing Lots button remains in release");
  File.WriteAllText("release-lots-check.txt", "PASS: release player scripts compiled; testing bypass returns false; testing Lots button is absent from compiled assembly.\n");
 }
}
