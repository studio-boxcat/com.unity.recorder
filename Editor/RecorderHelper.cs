#nullable enable
// A capture API for callers outside this package, so none of them name a Recorder type — the package
// is an optional submodule (worktreePoolTag = editor) that build slots check out without.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine;

namespace Universe.Editor
{
    public static class RecorderHelper
    {
        // Broader than the window's own IsRecording, which reads false while scene data is still being
        // gathered — the game has to stay in its recording branch across that gap.
        public static bool IsRecording()
        {
            var win = FindWindow();
            return win is not null && win.EventuallyStartRecording();
        }

        // Start a movie capture and enter play mode, returning the file being written. An empty
        // outputFile or a non-positive size leaves that part of the window's setup alone. Driven
        // through the window, not a headless RecorderController: the window is what IsRecording reads.
        public static string StartMovie(string outputFile, int width, int height)
        {
            var win = EditorWindow.GetWindow<RecorderWindow>(false, "Recorder", true);
            var settings = win.GetRecorderControllerSettings();

            var movie = settings.RecorderSettings.OfType<MovieRecorderSettings>().FirstOrDefault();
            if (movie is null)
            {
                movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
                movie.name = "Movie";
                settings.AddRecorderSettings(movie);
                win.SetRecorderControllerSettings(settings); // rebuilds the recorder list around it
            }
            movie.Enabled = true;

            if (outputFile.NotEmpty()) movie.OutputFile = WithoutExtension(outputFile, movie.Extension);
            if (width > 0 && height > 0)
            {
                movie.ImageInputSettings.OutputWidth = width;
                movie.ImageInputSettings.OutputHeight = height;
            }
            settings.Save();

            win.StartRecording(); // enters play mode itself
            // It no-ops on a compile error or a non-idle recorder, silently, so ask.
            if (!win.IsRecording()) throw new InvalidOperationException("the Recorder did not start");

            return movie.OutputFile;
        }

        // Stop the capture, returning the file that was written.
        public static string StopMovie()
        {
            var win = FindWindow() ?? throw new InvalidOperationException("no Recorder window is open");

            win.StopRecording();

            var movie = win.GetRecorderControllerSettings().RecorderSettings.OfType<MovieRecorderSettings>().FirstOrDefault();
            return movie is null ? "" : NewestFileBeside(movie.OutputFile, movie.Extension);
        }

        // Without opening one, unlike EditorWindow.GetWindow: IsRecording must stay false when the
        // user never opened the Recorder.
        private static RecorderWindow? FindWindow() =>
            Resources.FindObjectsOfTypeAll<RecorderWindow>().FirstOrDefault();

        // FileNameGenerator appends the encoder's extension, so the caller's is taken off again —
        // but only after it agrees, or the file asked for is not the file written.
        private static string WithoutExtension(string outputFile, string extension)
        {
            var ext = PathUtils.Ext(outputFile);
            if (ext.IsEmpty()) return outputFile;
            if (ext != $".{extension}")
                throw new ArgumentException($"The recorder writes .{extension}: {outputFile}");
            return outputFile[..^ext.Length];
        }

        // The Take number is resolved inside the recorder, so the file is found rather than rebuilt.
        // By extension, because screenshots land in the same folder.
        private static string NewestFileBeside(string outputFile, string extension)
        {
            if (PathUtils.Parent(outputFile) is not { } dir || !Directory.Exists(dir)) return outputFile;
            var newest = new DirectoryInfo(dir).GetFiles($"*.{extension}")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();
            return newest is null ? outputFile : $"{dir}/{newest.Name}";
        }
    }
}
