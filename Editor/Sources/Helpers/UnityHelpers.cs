using System.ComponentModel;
using System.IO;
using System.Linq;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// An ad-hoc collection of helpers for the Recorders.
    /// </summary>
    public static class UnityHelpers
    {
        /// <summary>
        /// Allows destroying Unity.Objects.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="allowDestroyingAssets"></param>
        public static void Destroy(UnityObject obj, bool allowDestroyingAssets = false)
        {
            if (obj == null)
                return;

            if (EditorApplication.isPlaying)
                UnityObject.Destroy(obj);
            else
                UnityObject.DestroyImmediate(obj, allowDestroyingAssets);
        }

        internal static GameObject CreateRecorderGameObject(string name)
        {
            var gameObject = new GameObject(name) { tag = "EditorOnly" };
            SetGameObjectVisibility(gameObject, RecorderOptions.ShowRecorderGameObject);
            return gameObject;
        }

        internal static void SetGameObjectsVisibility(bool value)
        {
            var rcs = FindObjectsHelper.FindObjectsByTypeWrapper<RecorderComponent>();
            foreach (var rc in rcs)
            {
                SetGameObjectVisibility(rc.gameObject, value);
            }
        }

        static void SetGameObjectVisibility(GameObject obj, bool visible)
        {
            if (obj != null)
            {
                obj.hideFlags = visible ? HideFlags.None : HideFlags.HideInHierarchy;

                if (!Application.isPlaying)
                {
                    try
                    {
                        EditorSceneManager.MarkSceneDirty(obj.scene);
                        EditorApplication.RepaintHierarchyWindow();
                        EditorApplication.DirtyHierarchyWindowSorting();
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        internal static bool AreAllSceneDataLoaded()
        {
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.isLoaded == false)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Load an asset from the current package's Editor/Assets folder.
        /// </summary>
        /// <param name="relativeFilePathWithExtension">The relative filename inside the Editor/Assets folder, without
        /// leading slash.</param>
        /// <param name="logError">Set this flag to true if you need to log errors when the Recorder cannot find the asset.</param>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <returns></returns>
        internal static T LoadLocalPackageAsset<T>(string relativeFilePathWithExtension, bool logError) where T : Object
        {
            T result = default(T);
            var fullPathInProject = $"Packages/com.unity.recorder/Editor/Assets/{relativeFilePathWithExtension}";

            if (File.Exists(fullPathInProject))
                result = AssetDatabase.LoadAssetAtPath(fullPathInProject, typeof(T)) as T;
            else if (logError)
                Debug.LogError($"Local asset file {fullPathInProject} not found.");
            return result;
        }

        /// <summary>
        /// Returns True if a manual vertical flip is required, False otherwise.
        /// The decision is based on the user's intention as well as the characteristics of the current graphics API
        /// (OpenGL is flipped vertically compared to Metal & DirectX) and the type of capture source.
        /// </summary>
        /// <param name="wantFlippedTexture">True if the user expects a vertically flipped texture, False otherwise.</param>
        /// <param name="captureSource">The input source for the encoder.</param>
        /// <param name="flipForEncoder">True if the encoder requires a flipped image, False otherwise.</param>
        /// <returns></returns>
        internal static bool NeedToActuallyFlip(bool wantFlippedTexture, BaseRenderTextureInput captureSource,
            bool flipForEncoder)
        {
            // We need to take several things into account: what the user expects, whether or not the rendering is made
            // on a GameView source, and whether or not the hardware is OpenGL.
            bool isGameView = captureSource is GameViewInput; // game view is already flipped
            bool isCameraInputLegacyRP = captureSource is CameraInput; // legacy RP has vflipped camera input

            bool isFlippedBecauseOfOpenGL = !SystemInfo.graphicsUVStartsAtTop;

            // The image will already be flipped if:
            // * the input comes from the GameView, OR
            // * the input comes from a TargetCamera in a LRP project, OR
            // * the OpenGL context flips it
            bool willBeFlipped = isGameView ^ flipForEncoder ^ isCameraInputLegacyRP ^ isFlippedBecauseOfOpenGL;

            // We flip if the user's intention is different from the result, and take into account the Y axis convention of the encoder
            return willBeFlipped != wantFlippedTexture;
        }

        /// <summary>
        /// Whether the current number of audio channels is supported by the recorder.
        /// </summary>
        /// <returns>bool</returns>
        internal static bool IsNumAudioChannelsSupported()
        {
            return AudioSettings.speakerMode is AudioSpeakerMode.Mono or AudioSpeakerMode.Stereo;
        }

        /// <summary>
        /// Returns the number of audio channels of the project.
        /// </summary>
        /// <returns>The number of audio channels of the project.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown if the speaker mode is not supported.</exception>
        internal static uint GetNumAudioChannels()
        {
            return GetNumAudioChannels(AudioSettings.speakerMode);
        }

        /// <summary>
        /// Returns the number of audio channels for a given speaker mode.
        /// </summary>
        /// <returns>The number of audio channels of the project.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown if the speaker mode is not supported</exception>
        internal static uint GetNumAudioChannels(AudioSpeakerMode mode)
        {
            switch (mode)
            {
                case AudioSpeakerMode.Mono:
                    return 1;
                case AudioSpeakerMode.Prologic: // not supported, but recognized.
                case AudioSpeakerMode.Stereo:
                    return 2;
                case AudioSpeakerMode.Quad:
                    return 4;
                case AudioSpeakerMode.Surround:
                    return 5;
                case AudioSpeakerMode.Mode5point1:
                    return 6;
                case AudioSpeakerMode.Mode7point1:
                    return 8;
                default:
                    throw new InvalidEnumArgumentException($"Unsupported speaker mode '{AudioSettings.speakerMode}'");
            }
        }

        /// <summary>
        /// Returns the name of a given speaker mode. If no speaker mode is provided, the project's speaker mode
        /// is probed.
        /// </summary>
        /// <returns>The number of audio channels of the project.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown if the speaker mode is not supported</exception>
        internal static string GetSpeakerModeName(AudioSpeakerMode mode)
        {
            switch (mode)
            {
                case AudioSpeakerMode.Mono:
                    return "Mono";
                case AudioSpeakerMode.Prologic:
                    return "Prologic DTS";
                case AudioSpeakerMode.Stereo:
                    return "Stereo";
                case AudioSpeakerMode.Quad:
                    return "Quad";
                case AudioSpeakerMode.Surround:
                    return "Surround";
                case AudioSpeakerMode.Mode5point1:
                    return "Surround 5.1";
                case AudioSpeakerMode.Mode7point1:
                    return "Surround 7.1";
                default:
                    throw new InvalidEnumArgumentException($"Unsupported speaker mode '{AudioSettings.speakerMode}'");
            }
        }

        /// <summary>
        /// Returns error message that is raised when the current default speaker mode is not supported depending on
        /// current encoder and current speaker mode.
        /// </summary>
        ///<param name="encoderName">Current encoder.</param>
        /// ///<param name="supportedSpeakerModes">Speaker modes supported by the encoder.</param>
        /// <returns>Error message.</returns>
        internal static string GetUnsupportedSpeakerModeErrorMessage(string encoderName, AudioSpeakerMode[] supportedSpeakerModes)
        {
            var defaultSpeakerModeName = GetSpeakerModeName(AudioSettings.speakerMode);
            var speakerModesMsg = AudioSpeakerModesToString(supportedSpeakerModes);
            return
                $"The {encoderName} only supports {speakerModesMsg} audio recording. The Default Speaker Mode is {defaultSpeakerModeName}.";
        }

        /// <summary>
        /// Returns an array of AudioSpeakerModes in a human readable string (ex: "speakerMode1, speakerMode2 and speakerMode3")
        /// </summary>
        /// ///<param name="speakerModes">An array of speaker modes</param>
        /// <returns>SpeakerModes separated by commas and 'and' for the last one.</returns>
        internal static string AudioSpeakerModesToString(AudioSpeakerMode[] speakerModes)
        {
            return string.Join(" ", speakerModes.Select((v, i) =>
            {
                if (i < speakerModes.Length - 2)
                    return $"{GetSpeakerModeName(v)},";
                if (i < speakerModes.Length - 1)
                    return $"{GetSpeakerModeName(v)} and";
                return GetSpeakerModeName(v);
            }));
        }
    }
}
