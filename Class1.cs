using System;
using System.IO;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using HarmonyLib;

[assembly: MelonInfo(typeof(MDReplacer.Main), "MD-Replacer", "1.0", "rei")]
[assembly: MelonGame("PeroPeroGames", "MuseDash")]

namespace MDReplacer
{
    public class Main : MelonMod
    {
        public static Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        public static string ImagesPath = Path.Combine(Environment.CurrentDirectory, "MDRImages");

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg(System.ConsoleColor.Cyan, "[INFO] MD-Replacer is initializing...");

            if (!Directory.Exists(ImagesPath))
            {
                Directory.CreateDirectory(ImagesPath);

                MelonLogger.Msg(System.ConsoleColor.Yellow, "--------------------------------------------------");
                MelonLogger.Msg(System.ConsoleColor.Red, "[!] ALERT: 'MDRImages' folder not found!");
                MelonLogger.Msg(System.ConsoleColor.Green, "[+] SUCCESS: 'MDRImages' folder has been created.");
                MelonLogger.Msg(System.ConsoleColor.White, ">>> Please put your .png images into the 'MDRImages' folder and RESTART the game.");
                MelonLogger.Msg(System.ConsoleColor.Yellow, "--------------------------------------------------");
                return;
            }

            string[] files = Directory.GetFiles(ImagesPath, "*.png");

            if (files.Length == 0)
            {
                MelonLogger.Msg(System.ConsoleColor.Yellow, "[WARNING] No .png files found in 'MDRImages' folder. The mod is active but doing nothing.");
                return;
            }

            MelonLogger.Msg(System.ConsoleColor.Magenta, $"[INFO] {files.Length} images found. Loading to memory...");

            foreach (string filePath in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                byte[] fileData = File.ReadAllBytes(filePath);

                Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                if (UnityEngine.ImageConversion.LoadImage(tex, fileData))
                {
                    tex.name = fileName;
                    tex.hideFlags = HideFlags.HideAndDontSave;
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.Apply(true);
                    TextureCache[fileName] = tex;
                    MelonLogger.Msg(System.ConsoleColor.Gray, "[LOADED]: " + fileName);
                }
            }
        }

        [HarmonyPatch(typeof(Material), nameof(Material.mainTexture), MethodType.Getter)]
        public class TexturePatch
        {
            private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
            private static readonly int ColorId = Shader.PropertyToID("_Color");
            private static HashSet<int> processedMaterials = new HashSet<int>();

            public static void Postfix(Material __instance, ref Texture __result)
            {
                if (__result == null || __instance == null) return;

                int instanceId = __instance.GetInstanceID();
                if (processedMaterials.Contains(instanceId)) return;

                string texName = __result.name;

                if (Main.TextureCache.TryGetValue(texName, out Texture2D customTex))
                {
                    __result = customTex;
                    __instance.mainTexture = customTex;
                    __instance.mainTextureScale = Vector2.one;
                    __instance.mainTextureOffset = Vector2.zero;

                    if (__instance.HasProperty(ColorId))
                        __instance.SetColor(ColorId, Color.white);

                    if (__instance.HasProperty(MainTexId))
                        __instance.SetTexture(MainTexId, customTex);

                    processedMaterials.Add(instanceId);

                    MelonLogger.Msg(System.ConsoleColor.Green, $"[REPLACED]: {texName}");
                }
            }
        }
    }

}
