using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Politicmon.Editor
{
    /// <summary>
    /// "Politicmon" menu in the Unity editor:
    ///  - Configure Player Settings (portrait, ids, icon, build scene). Runs once automatically.
    ///  - Regenerate the character spritesheet slices and their walk/idle AnimationClips.
    ///  - Build the Android APK (also usable from the command line with -executeMethod).
    /// </summary>
    [InitializeOnLoad]
    public static class PolitikmonMenu
    {
        const string MainScene = "Assets/Scenes/Main.unity";
        const string CharactersFolder = "Assets/Resources/Sprites/Characters";
        const string AnimationsFolder = "Assets/Resources/Animations/Characters";
        static readonly string[] Directions = { "down", "up", "left", "right" };

        static PolitikmonMenu()
        {
            EditorApplication.delayCall += () =>
            {
                if (PlayerSettings.productName != "Politicmon")
                    ConfigureProject();
            };
        }

        [MenuItem("Politicmon/Configurar proyecto (Player Settings)")]
        public static void ConfigureProject()
        {
            PlayerSettings.companyName = "Politicmon";
            PlayerSettings.productName = "Politicmon";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.politicmon.game");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.politicmon.game");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.statusBarHidden = true;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Sprites/UI/icon.png");
            if (icon != null)
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            if (File.Exists(MainScene))
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Politicmon: Player Settings configurados (vertical, Android/iOS, icono, escena principal).");
        }

        [MenuItem("Politicmon/Abrir escena principal")]
        public static void OpenMainScene() => EditorSceneManager.OpenScene(MainScene);

        // ---------------------------------------------------------------- spritesheets & animations

        [MenuItem("Politicmon/Regenerar spritesheets y animaciones de personajes")]
        public static void RegenerateCharacterAnimations()
        {
            var sheets = Directory.GetFiles(CharactersFolder, "*.png").Select(p => p.Replace('\\', '/')).ToList();
            foreach (var path in sheets)
                SliceCharacterSheet(path);
            foreach (var path in sheets)
                CreateCharacterClips(Path.GetFileNameWithoutExtension(path), path);
            AssetDatabase.SaveAssets();
            Debug.Log($"Politicmon: {sheets.Count} spritesheets troceados y {sheets.Count * 8} animaciones creadas.");
        }

        /// <summary>Each sheet is 32x64: columns stand/step, rows down/up/left/right (from the top).</summary>
        static void SliceCharacterSheet(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            string name = Path.GetFileNameWithoutExtension(path);
            var rects = new List<SpriteRect>();
            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 2; col++)
            {
                rects.Add(new SpriteRect
                {
                    name = $"{name}_{Directions[row]}_{col}",
                    rect = new Rect(col * 16, 64 - 16 * (row + 1), 16, 16),
                    alignment = SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                    spriteID = GUID.Generate(),
                });
            }
            provider.SetSpriteRects(rects.ToArray());
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        static void CreateCharacterClips(string character, string sheetPath)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().ToDictionary(s => s.name);
            string folder = $"{AnimationsFolder}/{character}";
            Directory.CreateDirectory(folder);
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            const float frame = 1f / 12f;
            foreach (var dir in Directions)
            {
                var stand = sprites[$"{character}_{dir}_0"];
                var step = sprites[$"{character}_{dir}_1"];
                SaveClip($"{folder}/{character}_walk_{dir}.anim", binding, frame, step, stand, step, stand);
                SaveClip($"{folder}/{character}_idle_{dir}.anim", binding, frame, stand);
            }
        }

        static void SaveClip(string path, EditorCurveBinding binding, float frameTime, params Sprite[] frames)
        {
            var clip = new AnimationClip { frameRate = 12 };
            var keys = frames.Select((s, i) => new ObjectReferenceKeyframe { time = i * frameTime, value = s }).ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
        }

        // ---------------------------------------------------------------- builds

        [MenuItem("Politicmon/Build/Android APK")]
        public static void BuildAndroid()
        {
            ConfigureProject();
            var options = new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = "Builds/Android/politicmon.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log("Politicmon: build Android " + report.summary.result + " -> " + options.locationPathName);
        }

        [MenuItem("Politicmon/Build/iOS (proyecto Xcode)")]
        public static void BuildIOS()
        {
            ConfigureProject();
            var options = new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = "Builds/iOS",
                target = BuildTarget.iOS,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log("Politicmon: build iOS " + report.summary.result + " -> " + options.locationPathName);
        }
    }
}
