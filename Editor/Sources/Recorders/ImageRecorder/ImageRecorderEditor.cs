using UnityEngine;

namespace UnityEditor.Recorder
{
    [CustomEditor(typeof(ImageRecorderSettings))]
    class ImageRecorderEditor : RecorderEditor
    {
        SerializedProperty m_OutputFormat;
        SerializedProperty m_CaptureAlpha;
        SerializedProperty m_JpegQuality;

        static class Styles
        {
            internal static readonly GUIContent FormatLabel = new GUIContent("Media File Format", "The file encoding format of the recorded output.");
            internal static readonly GUIContent CaptureAlphaLabel = new GUIContent("Include Alpha", "To include the alpha channel in the recording.\n\nIn the High Definition Render Pipeline (HDRP), you need to set the buffer format to R16G16B16A16.");
            internal static readonly GUIContent JpegQualityLabel = new GUIContent("Quality", "The JPEG encoding quality level.");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (target == null)
                return;

            m_OutputFormat = serializedObject.FindProperty("outputFormat");
            m_CaptureAlpha = serializedObject.FindProperty("captureAlpha");
            m_JpegQuality = serializedObject.FindProperty("m_JpegQuality");
        }

        protected override void FileTypeAndFormatGUI()
        {
            EditorGUILayout.PropertyField(m_OutputFormat, Styles.FormatLabel);
            var imageSettings = (ImageRecorderSettings)target;
            if (!UnityHelpers.UsingURP())
            {
                using (new EditorGUI.DisabledScope(!imageSettings.CanCaptureAlpha()))
                {
                    EditorGUILayout.PropertyField(m_CaptureAlpha, Styles.CaptureAlphaLabel);
                }
            }

            if ((ImageRecorderSettings.ImageRecorderOutputFormat)m_OutputFormat.enumValueIndex ==
                ImageRecorderSettings.ImageRecorderOutputFormat.JPEG)
            {
                EditorGUILayout.IntSlider(m_JpegQuality, 1, 100, Styles.JpegQualityLabel);
            }
        }
    }
}
