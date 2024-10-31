using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Loudenvier.Utils;

/// <summary>
/// Generates short, human-readable, base-62 encoded ids and even shorter session ids 
/// (useful for tagging logs and to group operations together). It uses a very precise timing 
/// mechanism with 100-nano seconds units, and can be called with a very high frequency 
/// without the risk of generating collisions.
/// </summary>
/// <remarks>
/// Ids are guaranteed to be unique and sequential only within the same machine. Additionaly,
/// session ids are guaranteed to be unique and sequential only within the same app session and machine.
/// If ids are captured from different machines/devices, additional context (a machine id for example) will
/// be needed to prevent collisions. 
/// <para>
/// The original use case for <see cref="IdGenerator"/> was to group together log messages by operations while providing
/// an Id more instantly recognizable and readable than the much longer <see cref="Guid"/> based approach.
/// <code>
/// // Length comparison:
/// 
/// var sessionId = IdGenerator.ShortSessionId(); // typically 3 to 4 characters -> "1yu"
/// var shortId = IdGenerator.ShortId(); // typically 10 characters -> "9rJK4cGpMN"
/// var uuid = Guid.NewGuid.ToString(); // 36 characters -> "4b73e4d2-4cdd-4662-92d6-92142476cfc1"
/// 
/// </code>
/// Session ids are very useful in scenarios where the id acts more as a clue than a 
/// strong guarantee of uniqueness. Non-session ids together with per machine/device id
/// can be used to generate unique ids across diverse, distributed sources. 
/// </para>
/// </remarks>
public static class IdGenerator
{
    /// <summary>The time the session for <see cref="ShortSessionId"/> generation was started or <see cref="Restart"/>ed. </summary>
    public static DateTime SessionStart { get; private set; } = DateTime.Now;

    /// <summary>Restarts the session for <see cref="ShortSessionId"/> generation by resetting <see cref="SessionStart"/></summary>
    public static void Restart() => SessionStart = DateTime.Now;

    /// <summary>
    /// Generates a short session Id based on the time since the app started or the last time <see cref="Restart"/> was called.
    /// </summary>
    /// <returns>A very short, instantly recognizable, human-readable, base-62 encoded session id</returns>
    public static string ShortSessionId() => ShortId(DateTimeOffset.FromFileTime(PreciseSystemTime).Subtract(SessionStart).Ticks);

    /// <summary>
    /// Generates a short Id guaranteed to be unique and sequential within a single machine/device even if 
    /// called with very high frequencies.
    /// </summary>
    /// <returns>A short, instantly recognizable, human-readable, base-62 encoded id</returns>
    public static string ShortId() => ShortId(PreciseSystemTime);

    /// <summary>
    /// Generates a short Id based on an arbitrary <see cref="long"/> <paramref name="value"/>. 
    /// The caller is responsible to ensure uniqueness and/or sequencing of <paramref name="value"/> if its required.
    /// </summary>
    /// <param name="value">The value to derive the Id from</param>
    /// <returns>A short, instantly recognizable, human-readable, base-62 encoded id</returns>
    public static string ShortId(long value) => value.ToBase62String();

    [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
    static extern void GetSystemTimePreciseAsFileTime(out long filetime);

    static long PreciseSystemTime {
        get {
            // 100 nano seconds units
            GetSystemTimePreciseAsFileTime(out long fileTime);
            return fileTime;
        }
    }
}