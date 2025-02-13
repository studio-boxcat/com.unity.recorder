namespace UnityEditor.Recorder.Encoder
{
    /// <summary>
    /// The convention of the coordinate system for an encoder, to ensure that the images supplied to the encoder are flipped if needed.
    /// </summary>
    public enum EncoderCoordinateConvention
    {
        /// <summary>
        /// The origin is in the top left corner of each frame.
        /// </summary>
        OriginIsTopLeft,
        /// <summary>
        /// The origin is in the bottom left corner of each frame.
        /// </summary>
        OriginIsBottomLeft,
    }
}
