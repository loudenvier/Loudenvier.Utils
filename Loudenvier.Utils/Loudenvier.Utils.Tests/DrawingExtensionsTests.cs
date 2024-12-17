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

    [Fact]
    public void ParseRect_ReturnsEmpty_OnNullText() {
        Assert.Equal(Rectangle.Empty, ((string?)null).ParseRect());
    }
    [Fact]
    public void ParseRect_ReturnsEmpty_OnEmptyText() {
        Assert.Equal(Rectangle.Empty, "".ParseRect());
    }
    [Fact]
    public void ParseRect_ReturnsEmpty_OnWhiteSpaceText() {
        Assert.Equal(Rectangle.Empty, "    ".ParseRect());
    }
    [Fact]
    public void ParseRect_WorksWithSingleLength_ResultingIn() {
        Assert.Equal(new Rectangle(0, 0, 100, 100), "100".ParseRect());
    }
    [Fact]
    public void ParseRect_WorksWithWidthAndHeightOnly() {
        Assert.Equal(new Rectangle(0, 0, 100, 200), "100,200".ParseRect());
    }
    [Fact]
    public void ParseRect_WorksWithAllFourX_Y_Width_Height() {
        Assert.Equal(new Rectangle(10, 20, 100, 200), "10,20,100,200".ParseRect());
    }
    [Fact]
    public void ParseRect_SpacesAreAllowed() {
        Assert.Equal(new Rectangle(10, 20, 100, 200), " 10 ,  20 ,    100,   200".ParseRect());
    }
}