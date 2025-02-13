using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// Abstract base class for all Recorders that output images.
    /// </summary>
    /// <typeparam name="T">The class implementing the Recorder Settings.</typeparam>
    public abstract class BaseTextureRecorder<T> : GenericRecorder<T> where T : RecorderSettings
    {
        /// <summary>
        /// Whether or not to use asynchronous GPU commands in order to get the texture for the recorder.
        /// </summary>
        protected bool UseAsyncGPUReadback;

        /// <summary>
        /// Whether or not accumulation is requested and has been enabled.
        /// </summary>
        internal bool accumulationInitialized;

        private PooledBufferAsyncGPUReadback asyncReadback;

        Texture2D m_ReadbackTexture;
        readonly Queue<float> m_AsyncReadbackTimeStamps = new Queue<float>();


        internal void EnqueueTimeStamp(float time)
        {
            m_AsyncReadbackTimeStamps.Enqueue(time);
        }

        internal float DequeueTimeStamp()
        {
            if (m_AsyncReadbackTimeStamps.Count == 0)
            {
                throw new Exception("Timestamp queue is empty");
            }

            return m_AsyncReadbackTimeStamps.Dequeue();
        }

        /// <summary>
        /// Stores the format of the texture used for the readback.
        /// </summary>
        protected const TextureFormat ReadbackTextureFormat = TextureFormat.RGBA32;

        /// <inheritdoc/>
        protected internal override bool BeginRecording(RecordingSession session)
        {
            if (!base.BeginRecording(session))
                return false;
            UseAsyncGPUReadback = SystemInfo.supportsAsyncGPUReadback;
            m_AsyncReadbackTimeStamps.Clear();
            asyncReadback = new PooledBufferAsyncGPUReadback();
            return true;
        }

        /// <inheritdoc/>
        protected internal override void RecordFrame(RecordingSession session)
        {
            EnqueueTimeStamp(session.recorderTime);

            var input = (BaseRenderTextureInput)m_Inputs[0];

            if (input.ReadbackTexture != null)
            {
                WriteFrame(input.ReadbackTexture);
                return;
            }

            var renderTexture = input.OutputRenderTexture;

            if (renderTexture == null)
            {
                Debug.LogWarning($"Ignoring the current frame because the source has been disposed");
                return;
            }

            if (UseAsyncGPUReadback)
            {
                if (WriteGPUTextureFrame(renderTexture)) // Recorder might want ot
                {
                    return;
                }

                asyncReadback.RequestGPUReadBack(renderTexture, GraphicsFormatUtility.GetGraphicsFormat(ReadbackTextureFormat, false), ReadbackDone);
                return;
            }

            var width = renderTexture.width;
            var height = renderTexture.height;

            if (m_ReadbackTexture == null)
                m_ReadbackTexture = CreateReadbackTexture(width, height);

            var backupActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            m_ReadbackTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            m_ReadbackTexture.Apply();
            RenderTexture.active = backupActive;
            WriteFrame(m_ReadbackTexture);
        }

        internal virtual bool WriteGPUTextureFrame(RenderTexture tex)
        {
            return false;
        }

        void ReadbackDone(AsyncGPUReadbackRequest r)
        {
            Profiler.BeginSample("BaseTextureRecorder.ReadbackDone");
            WriteFrame(r);
            Profiler.EndSample();
        }

        /// <inheritdoc/>
        protected internal override void EndRecording(RecordingSession session)
        {
            if (asyncReadback != null)
            {
                asyncReadback.Dispose();
                asyncReadback = null;
            }

            base.EndRecording(session);


            DisposeEncoder();
        }

        private Texture2D CreateReadbackTexture(int width, int height)
        {
            return new Texture2D(width, height, ReadbackTextureFormat, false);
        }

        /// <summary>
        /// Writes the frame from an asynchronous GPU read request.
        /// </summary>
        /// <param name="r">The asynchronous readback target.</param>
        protected virtual void WriteFrame(AsyncGPUReadbackRequest r)
        {
            if (r.hasError)
            {
                ConsoleLogMessage("The rendered image has errors. Skipping this frame.", LogType.Error);
                return;
            }

            if (m_ReadbackTexture == null)
                m_ReadbackTexture = CreateReadbackTexture(r.width, r.height);
            Profiler.BeginSample("BaseTextureRecorder.LoadRawTextureData");
            m_ReadbackTexture.LoadRawTextureData(r.GetData<byte>());
            Profiler.EndSample();
            WriteFrame(m_ReadbackTexture);
        }

        /// <summary>
        /// Writes the frame from a Texture2D.
        /// </summary>
        /// <param name="t">The readback target.</param>
        protected virtual void WriteFrame(Texture2D t)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Releases the encoder resources.
        /// </summary>
        protected virtual void DisposeEncoder()
        {
            UnityHelpers.Destroy(m_ReadbackTexture);
            Recording = false;
        }
    }
}
