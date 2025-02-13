using System;
using System.ComponentModel;
using System.Linq;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace UnityEditor.Recorder
{
    [CustomEditor(typeof(MovieRecorderSettings))]
    class MovieRecorderEditor : RecorderEditor
    {
        SerializedProperty m_EncoderSettings;

        private Rect? lastRect;

        static class Styles
        {
            internal static readonly GUIContent SourceLabel = new GUIContent("Source", "The input type to use for the recording.");
            internal static readonly GUIContent AlphaLabel = new GUIContent("Include alpha", "Whether or not to include the alpha channel.");
            internal static readonly GUIContent AudioLabel = new GUIContent("Include audio", "Whether or not to include the audio signal.");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (target == null)
                return;

            m_EncoderSettings = serializedObject.FindProperty("encoderSettings");
        }

        protected override void OnEncodingGui()
        {
        }

        protected override void FileTypeAndFormatGUI()
        {
            var mrs = target as MovieRecorderSettings;

            if (mrs.EncoderSettings == null)
            {
                return;
            }

            // Display selected encoder's fields, greyed out if not supported
            using (new EditorGUI.DisabledScope(!mrs.EncoderSettings.SupportsCurrentPlatform()))
                EditorGUILayout.PropertyField(m_EncoderSettings, true);

            // Expose CaptureAudio and CaptureAlpha from the MovieRecorderSettings but look at input and encoder capabilities
            if (mrs.EncoderSettings.CanCaptureAudio)
                mrs.CaptureAudio = EditorGUILayout.Toggle(Styles.AudioLabel, mrs.CaptureAudio);

            if (mrs.ImageInputSettings.SupportsTransparent && mrs.EncoderSettings.CanCaptureAlpha)
                mrs.CaptureAlpha = EditorGUILayout.Toggle(Styles.AlphaLabel, mrs.CaptureAlpha);
        }

        protected override void ImageRenderOptionsGUI()
        {
            var recorder = (RecorderSettings)target;

            foreach (var inputsSetting in recorder.InputsSettings)
            {
                var audioSettings = inputsSetting as AudioInputSettings;
                if (audioSettings == null) // don't draw the audio input, let the choice be handled by ExtraOptionsGUI()
                {
                    var p = GetInputSerializedProperty(serializedObject, inputsSetting);
                    EditorGUILayout.PropertyField(p, Styles.SourceLabel);
                }
            }
        }
    }
}
