using System;
using System.Drawing;
using System.Linq;

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

    /// <summary>
    /// Parses a string representation of a rectangle into a <see cref="Rectangle"/> object. Valid formats are 
    /// "length" (defines a square of side = length), "width,height" (defines a rectangle at (0,0) with the specified width and height) 
    /// or "x,y,width,height" (defines a rectangle at (x, y) with the specified width and height), 
    /// where length, x, y, width and height are all integers (spaces are allowed)."),
    /// </summary>
    /// <param name="text">The textual representation of the rectangle</param>
    /// <param name="separator">Allow setting of a custom separator (comma is the default)</param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    public static Rectangle ParseRect(this string? text, char separator = ',') {
        if (text is null || string.IsNullOrWhiteSpace(text))
            return Rectangle.Empty;
        var parts = text.Split(separator).Select(text => Convert.ToInt32(text)).ToArray();
        var rect = parts switch {
            { Length: 1 } => new Rectangle(0, 0, parts[0], parts[0]),
            { Length: 2 } => new Rectangle(0, 0, parts[0], parts[1]),
            { Length: 4 } => new Rectangle(parts[0], parts[1], parts[2], parts[3]),
            _ => throw new FormatException("The rectangle was not in the expected format. " +
            "Valid formats are \"width,height\" or \"x,y,width,height\", " +
            "where x, y, width and height are integers."),
        };
        return rect;
    }


}