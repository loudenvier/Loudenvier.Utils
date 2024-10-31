using System;
using System.Drawing;

namespace Loudenvier.Utils.Drawing;

/// <summary>
/// Adds a few neat drawing extensions
/// </summary>
public static class DrawingExtensions
{
    /// <summary>
    /// Expands the <paramref name="rect"/> by the specified fixed amount, 
    /// ensuring it's contained inside an optional <paramref name="boundingBox"/>. When the bounding box is omitted
    /// it's assumed to be (0, 0, <see cref="int.MaxValue"/>, <see cref="int.MaxValue"/>) which prevents
    /// negative <see cref="Rectangle.X"/>/<see cref="Rectangle.Y"/> resulting values.
    /// </summary>
    /// <param name="rect">The rectangle to expand/inflate</param>
    /// <param name="widthOrTotal">The amount to expand this rectangle horizontally or totally if <paramref name="height"/> is omitted</param>
    /// <param name="height">The amount to expand this rectangle vertically. If omitted it uses the same value of <paramref name="widthOrTotal"/></param>
    /// <param name="boundingBox">The bounding box constraining the maximum dimensions of the expanded rectangle</param>
    public static void Expand(this ref Rectangle rect, int widthOrTotal, int? height = null, Rectangle? boundingBox = null) {
        height ??= widthOrTotal;
        boundingBox ??= new(0, 0, int.MaxValue, int.MaxValue);
        rect.Inflate(widthOrTotal, height.Value);
        rect.Intersect(boundingBox.Value);
    }

    /// <summary>
    /// Expands the <paramref name="rect"/> by the specified percentage,  ensuring it's contained inside 
    /// an optional <paramref name="boundingBox"/>. When the bounding box is omitted
    /// it's assumed to be (0, 0, <see cref="int.MaxValue"/>, <see cref="int.MaxValue"/>) which prevents
    /// negative <see cref="Rectangle.X"/>/<see cref="Rectangle.Y"/> resulting values.
    /// </summary>
    /// <param name="rect">The rectangle to expand/inflate</param>
    /// <param name="percentWidthOrTotal">The percentage to expand this rectangle horizontally or totally if <paramref name="percentHeight"/> is omitted</param>
    /// <param name="percentHeight">The percentage to expand this rectangle vertically. If omitted it uses the same value of <paramref name="widthOrTotal"/></param>
    /// <param name="boundingBox">The bounding box constraining the maximum dimensions of the expanded rectangle</param>
    public static void Expand(this ref Rectangle rect, double percentWidthOrTotal, double? percentHeight = null, Rectangle? boundingBox = null) {
        percentHeight ??= percentWidthOrTotal;
        var width = rect.Width * percentWidthOrTotal;
        var height = rect.Height * percentHeight;
        Expand(ref rect, (int)width, (int)height, boundingBox);
    }


}