using System;
using System.Reflection;

namespace Loudenvier.Utils.Events;

/// <summary>
/// A few utility methods/extensions that deal with .NET event handling and dispatching mechanisms.
/// </summary>
public static class EventsExtensions
{

    /// <summary>
    /// Removes all event handlers from the <see cref="event"/> named <paramref name="EventName"/>.
    /// </summary>
    /// <param name="obj">The object from which the event handlers will be removed.</param>
    /// <param name="EventName">The name of the event from which to remove all event handlers.</param>
    public static void RemoveEventHandlers(this object obj, string EventName) {
        const BindingFlags bindings = BindingFlags.IgnoreCase | BindingFlags.Public |
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        if (obj is null)
            return;

        EventInfo? ei = obj.GetType().GetEvent(EventName, bindings);

        if (ei?.DeclaringType is not null) { 
            FieldInfo? fi = ei.DeclaringType.GetField(ei.Name, bindings);
            if (fi?.GetValue(obj) is Delegate mdel) {
                foreach (Delegate del in mdel.GetInvocationList())
                    ei.RemoveEventHandler(obj, del);
            }
        }
    }

}
