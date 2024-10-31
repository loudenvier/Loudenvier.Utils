using Loudenvier.Utils.Drawing;
using System.Drawing;

namespace Loudenvier.Utils.Tests;

public class DrawingExtensionsTests {
    [Fact]
    public void RectangleExpand_PreventsGoingNegative() {
        var rect = new Rectangle(10, 10, 10, 10); // 10 x 10 rectangle (10,10)-(20,20)
        rect.Expand(20, 20);
        Assert.Equal(new Rectangle(0, 0, 40, 40), rect);
    }
    [Fact]
    public void RectangleExpandWithBoundingBox_ContainsInsideBox() {
        var rect = new Rectangle(10, 10, 10, 10); // 10 x 10 rectangle (10,10)-(20,20)
        rect.Expand(20, 20, new Rectangle(0, 0, 30, 30));
        Assert.Equal(new Rectangle(0, 0, 30, 30), rect);
    }
    [Fact]
    public void RectangleExpandWithBoundingBox_BoundingBoxCanBeNegative() {
        var rect = new Rectangle(10, 10, 10, 10); // 10 x 10 rectangle (10,10)-(20,20)
        rect.Expand(20, 20, new Rectangle(-10, -10, 60, 60));
        Assert.Equal(new Rectangle(-10, -10, 50, 50), rect);
    }

    [Fact]
    public void RectangleExpandPercent_PreventsGoingNegative() {
        var rect = new Rectangle(10, 10, 10, 10); // 10 x 10 rectangle (10,10)-(20,20)
        // 2.0 = 200% = 10w * 200% = 20 pixels to expand (the same as the fixed amount tests)
        rect.Expand(2.0, 2.0);
        Assert.Equal(new Rectangle(0, 0, 40, 40), rect);
    }
    [Fact]
    public void RectangleExpandPercent_CanUseSinglePercentage() {
        var rect = new Rectangle(10, 10, 10, 10); // 10 x 10 rectangle (10,10)-(20,20)
        rect.Expand(2.0);
        Assert.Equal(new Rectangle(0, 0, 40, 40), rect);
    }
    [Fact]
    public void RectangleExpandPercentWithBoundingBox_ContainsInsideBox() {
        var rect = new Rectangle(10, 10, 10, 10); // 10 x 10 rectangle (10,10)-(20,20)
        rect.Expand(2.0, boundingBox: new Rectangle(0, 0, 30, 30));
        Assert.Equal(new Rectangle(0, 0, 30, 30), rect);
    }

}