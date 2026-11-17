using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEditor.Rendering.Universal.ShaderGUI;

namespace GDD110.Editor
{
    public static class GddSubstanceMaterials
    {
        const string Menu = "Assets/GDD 110/Create Materials from Substance Textures";
        const string Preset = "Unity Universal Render Pipeline (Metallic Standard)";
        static readonly Regex MapName = new Regex(
            @"^(.+?)[_ -](AlbedoTransparency|Base[_ -]?Color|Albedo|Metallic[_ -]?Smoothness|Normal(?:[_ -]?OpenGL)?|Ambient[_ -]?Occlusion|Occlusion|AO|Emission|Emissive)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        enum Map { Color, MetallicSmoothness, Normal, Occlusion, Emission }

        [MenuItem(Menu, true)]
        static bool CanCreate() => IsAssetFolder(AssetDatabase.GetAssetPath(Selection.activeObject));

        [MenuItem(Menu, false, 2002)]
        static void CreateSelected() => Run(AssetDatabase.GetAssetPath(Selection.activeObject));

        [MenuItem("Tools/GDD 110/Materials/Create from Substance Folder…")]
        public static void ChooseFolder()
        {
            var project = Path.GetDirectoryName(Application.dataPath);
            var folder = EditorUtility.OpenFolderPanel("Choose a Painter export folder inside Assets", Application.dataPath, "");
            if (!string.IsNullOrEmpty(folder))
                Run(Path.GetRelativePath(project, folder).Replace('\\', '/'));
        }

        static bool IsAssetFolder(string folder) =>
            folder != null && folder.StartsWith("Assets/", StringComparison.Ordinal) && AssetDatabase.IsValidFolder(folder);

        static void Run(string folder)
        {
            if (!IsAssetFolder(folder))
            {
                EditorUtility.DisplayDialog("Substance materials", "Copy the exported textures into a folder inside this project's Assets first.", "OK");
                return;
            }

            try
            {
                var materials = CreateMaterials(folder);
                GddStartHere.MarkDone(GddStartHere.MaterialCreated);
                Selection.objects = materials;
                EditorGUIUtility.PingObject(materials[0]);
                Debug.Log($"[GDD 110] Created {materials.Length} URP material(s) in {folder}. Drag each onto its mesh material slot.", materials[0]);
            }
            catch (InvalidOperationException ex)
            {
                EditorUtility.DisplayDialog("Substance materials", ex.Message, "OK");
            }
        }

        internal static Material[] CreateMaterials(string folder)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("Unity could not find the URP Lit shader.");

            var sets = new SortedDictionary<string, Dictionary<Map, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                         .Select(guid => AssetDatabase.GUIDToAssetPath(guid)))
            {
                if (Path.GetDirectoryName(path).Replace('\\', '/') != folder) continue;
                var match = MapName.Match(Path.GetFileNameWithoutExtension(path));
                if (!match.Success) continue;
                var key = match.Groups[1].Value;
                var suffix = Regex.Replace(match.Groups[2].Value.ToLowerInvariant(), "[_ -]", "");
                var map = suffix switch
                {
                    "albedotransparency" or "basecolor" or "albedo" => Map.Color,
                    "metallicsmoothness" => Map.MetallicSmoothness,
                    "normal" or "normalopengl" => Map.Normal,
                    "emission" or "emissive" => Map.Emission,
                    _ => Map.Occlusion
                };

                if (!sets.TryGetValue(key, out var textures))
                    sets.Add(key, textures = new Dictionary<Map, string>());
                if (!textures.TryAdd(map, path))
                    throw new InvalidOperationException($"'{key}' has two {map} maps. Keep one version of that map in this folder.");
            }

            if (sets.Count == 0)
                throw new InvalidOperationException($"No matching textures in this folder. Export with the {Preset} preset and select the folder directly containing files such as Robot_Body_BaseColor.png.");

            foreach (var (name, textures) in sets)
            {
                if (!textures.ContainsKey(Map.Color))
                    throw new InvalidOperationException($"'{name}' is missing its matching BaseColor or AlbedoTransparency map. Export the full set with {Preset}.");
                if (textures.TryGetValue(Map.MetallicSmoothness, out var packed) &&
                    !((TextureImporter)AssetImporter.GetAtPath(packed)).DoesSourceTextureHaveAlpha())
                    throw new InvalidOperationException($"'{Path.GetFileName(packed)}' needs an alpha channel for smoothness. Re-export with {Preset} as PNG or TIFF.");
            }

            var materials = new List<Material>();
            foreach (var (name, textures) in sets)
            {
                var material = new Material(shader);
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Smoothness", textures.ContainsKey(Map.MetallicSmoothness) ? 1f : 0.5f);
                foreach (var (map, path) in textures)
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    var type = map == Map.Normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    bool srgb = map == Map.Color || map == Map.Emission;
                    bool needsAlpha = map == Map.MetallicSmoothness && importer.alphaSource != TextureImporterAlphaSource.FromInput;
                    bool flipGreen = map == Map.Normal && importer.flipGreenChannel;
                    if (importer.textureType != type || importer.sRGBTexture != srgb || needsAlpha || flipGreen)
                    {
                        importer.textureType = type;
                        importer.sRGBTexture = srgb;
                        if (map == Map.MetallicSmoothness) importer.alphaSource = TextureImporterAlphaSource.FromInput;
                        if (map == Map.Normal) importer.flipGreenChannel = false;
                        importer.SaveAndReimport();
                    }

                    material.SetTexture(map switch
                    {
                        Map.Color => "_BaseMap",
                        Map.MetallicSmoothness => "_MetallicGlossMap",
                        Map.Normal => "_BumpMap",
                        Map.Occlusion => "_OcclusionMap",
                        _ => "_EmissionMap"
                    }, AssetDatabase.LoadAssetAtPath<Texture2D>(path));

                    if (map == Map.Emission)
                    {
                        material.SetColor("_EmissionColor", Color.white);
                        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                    }
                }

                BaseShaderGUI.SetMaterialKeywords(material, LitGUI.SetMaterialKeywords);
                AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.mat"));
                materials.Add(material);
            }

            AssetDatabase.SaveAssets();
            return materials.ToArray();
        }
    }
}
