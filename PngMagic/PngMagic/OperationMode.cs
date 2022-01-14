namespace PngMagic.Core;

/// <summary>
/// Defines how the core should handle the PNG
/// </summary>
public enum OperationMode
{
    /// <summary>
    /// No operation is specified, fail.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Append an image as custom chunks.
    /// </summary>
    Pack,

    /// <summary>
    /// Extracts an image from the custom chunk data.
    /// </summary>
    Extract,

    /// <summary>
    /// Removes an image from the custom chunk data.
    /// </summary>
    Remove,

    /// <summary>
    /// projects the hidden image onto the original image.
    /// </summary>
    Project
}
