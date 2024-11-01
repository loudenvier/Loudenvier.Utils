using Loudenvier.Utils.Events;

namespace Loudenvier.Utils.Tests;

public class EventsExtensionsTests {
    [Fact]
    public void EventClassHasHandlerBehavesAsExpected() {
        var e = new EventClass();
        Assert.False(e.HasHandler);
        e.MyEvent += Handler;
        e.MyEvent += Handler2;
        Assert.True(e.HasHandler);
        e.MyEvent -= Handler;
        Assert.True(e.HasHandler);
        e.MyEvent -= Handler2;
        Assert.False(e.HasHandler);
    }

    [Fact]
    public void RemoveEventHandlers_CanRemoveInstanceEventHandler() {
        var e = new EventClass();
        e.MyEvent += Handler;
        e.RemoveEventHandlers(nameof(e.MyEvent));
        Assert.False(e.HasHandler);
    }

    [Fact]
    public void RemoveEventHandlers_RemovesAllInstanceEventHandlers() {
        var e = new EventClass();
        e.MyEvent += Handler;
        e.MyEvent += Handler2;
        e.RemoveEventHandlers(nameof(e.MyEvent));
        Assert.False(e.HasHandler);
    }

    [Fact]
    public void RemoveEventHandlers_CanRemoveStaticEventHandlers() {
        var e = new EventClass();
        EventClass.MyStaticEvent += Handler;
        Assert.True(EventClass.HasStaticHandler);
        e.RemoveEventHandlers(nameof(EventClass.MyStaticEvent));
        Assert.False(EventClass.HasStaticHandler);
    }

    [Fact]
    public void RemoveEventHandlers_CanRemoveInheritedInstanceEventHandler() {
        var e = new EventClassChild();
        e.MyEvent += Handler;
        Assert.True(e.HasHandler);
        e.RemoveEventHandlers(nameof(e.MyEvent));
        Assert.False(e.HasHandler);
    }
    [Fact]
    public void RemoveEventHandlers_CanRemoveInstanceEventHandlerInInheritedClass() {
        var e = new EventClassChild();
        e.InheritedEvent += Handler;
        Assert.True(e.HasInheritedHandler);
        e.RemoveEventHandlers(nameof(e.InheritedEvent));
        Assert.False(e.HasInheritedHandler);
    }


    void Handler(object? sender, EventArgs e) { }
    void Handler2(object? sender, EventArgs e) { }  

    public class EventClass {

        public event EventHandler<EventArgs>? MyEvent;
        public bool HasHandler => MyEvent?.GetInvocationList().Any() == true;

        public static event EventHandler<EventArgs>? MyStaticEvent;
        public static bool HasStaticHandler => MyStaticEvent?.GetInvocationList().Any() == true;
    }

    public class EventClassChild : EventClass {
        public event EventHandler<EventArgs>? InheritedEvent;
        public bool HasInheritedHandler => InheritedEvent?.GetInvocationList().Any() == true;
    }
    
}