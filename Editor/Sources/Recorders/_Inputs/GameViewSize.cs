using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Unity.Recorder.Editor.Tests")]

namespace UnityEditor.Recorder.Input
{
    static partial class GameViewSize
    {
        public static bool IsMainPlayViewGameView()
        {
            return PlayModeWindow.GetViewType() == PlayModeWindow.PlayModeViewTypes.GameView;
        }

        public static void SwapMainPlayViewToGameView()
        {
            if (IsMainPlayViewGameView())
                return;

            PlayModeWindow.SetViewType(PlayModeWindow.PlayModeViewTypes.GameView);
        }

        public static void DisableMaxOnPlay()
        {
            PlayModeWindow.SetPlayModeFocused(true);
        }

        public static void GetGameRenderSize(out uint width, out uint height)
        {
            PlayModeWindow.GetRenderingResolution(out width, out height);
        }

        /// <summary>
        /// Set the GameView to a custom resolution when the passed parameters are different from the current resolution.
        /// A width or height of 0 will be ignored.
        /// </summary>
        public static void SetCustomSize(int width, int height)
        {
            if (width == 0 || height == 0)
                return;

            GetGameRenderSize(out uint currentWidth, out uint currentHeight);
            if (width == currentWidth && height == currentHeight)
                return;

            PlayModeWindow.SetCustomRenderingResolution((uint)width, (uint)height, "Recording Resolution");
        }
    }
}
