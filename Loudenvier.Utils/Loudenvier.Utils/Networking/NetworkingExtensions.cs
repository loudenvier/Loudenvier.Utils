using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace Loudenvier.Utils;

/// <summary>
/// Basic helper methods around networking objects (IPAddress, IpEndPoint, Socket, etc.)
/// </summary>
public static class NetworkingHelpers
{
    /// <summary>
    /// Gets the preferred IP Address to the <paramref name="targetIp"/> by connecting with an UDP socket.
    /// Works well on Windows, Linux and Mac (source: https://stackoverflow.com/a/27376368/285678)
    /// </summary>
    /// <remarks>It won't work if there is no network connectivity. Since it finds the target's "preferred" 
    /// outboung IP Address the result can vary for different targets on machines with multiple network
    /// interfaces. It normally finds out the correct, preferred local IP (which is NOT your NAT IP!).</remarks>
    /// <returns></returns>
    public static IPAddress GetLocalIPv4Address(string? targetIp = null) {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
        socket.Connect(targetIp ?? "8.8.8.8", 65530);
        if (socket.LocalEndPoint is IPEndPoint endpoint)
            return endpoint.Address;
        // this should never, ever happens!
        throw new ArgumentException("Local end point is not an IPEndPoint");
    }

    /// <summary>Converts a string in standard IEEE 802 (EUI-48/64) to a MAC (<see cref="PhysicalAddress"/>) 
    /// in a more lenient way than <see cref="PhysicalAddress.Parse()"/> 
    /// (allows <c>:</c> instead of only <c>-</c> as separators)</summary>
    /// <param name="mac">The EUI-48 or EUI-64 textual representation of the MAC Address</param>
    /// <returns>The parsed MAC address</returns>
    public static PhysicalAddress ToMACAddress(this string mac) {
        if (mac == null)
            throw new ArgumentNullException("MAC");
        mac = mac.Replace(':', '-');
        return PhysicalAddress.Parse(mac.ToUpperInvariant());
    }

    /// <summary>
    /// Converts the <paramref name="mac"/> address to its EUI-48 or EUI-64 representation, optionally writing 
    /// hexadecimals in <paramref name="uppercase"/> and allowing customization of the field <paramref name="separator"/>
    /// </summary>
    /// <param name="mac">The MAC address to convert to string</param>
    /// <param name="uppercase">Option to convert hexadecimal digits to uppercase (defaults to <see cref="true"/>)</param>
    /// <param name="separator">Specifies the separator (defaults to ":")</param>
    /// <returns>The formatted MAC address</returns>
    public static string ToFormattedString(this PhysicalAddress mac, bool uppercase = true, string separator = ":") {
        var bytes = mac.GetAddressBytes().ToList();
        // preencho (se for EUI-48 serão 6 bytes, senão 8)
        int top = bytes.Count > 6 ? 8 : 6;
        while (bytes.Count < top)
            bytes.Insert(0, 0);
        var result = string.Join(separator, bytes.Select(x => x.ToString("x2")));
        return uppercase ? result.ToUpperInvariant() : result;
    }

    /// <summary>Converts the integral representation of a MAC address to its EUI-48/64 representation 
    /// (<see cref="ToFormattedString(PhysicalAddress, bool, string)"/>)</summary>
    /// <remarks>It uses the defaults for <see cref="ToFormattedString(PhysicalAddress, bool, string)"/>. If more control is needed
    /// chain <see cref="ToMACAddress(long)"/> with <see cref="ToFormattedString(PhysicalAddress, bool, string)"/> 
    /// to provide custom values to the parameters</remarks>
    /// <param name="mac">The MAC represented as a <see cref="long"/></param>
    /// <returns>The formatted MAC Address</returns>
    public static string? ToFormattedMACAddress(this long mac)
        => mac == 0 ? null : mac.ToMACAddress().ToFormattedString();

    /// <summary>Converts the <paramref name="mac"/> address to its integral (<see cref="long"/>) representation</summary>
    /// <param name="mac">The MAC address to convert to <see cref="long"/></param>
    /// <returns>A <see cref="long"/> which represents the MAC address</returns>
    public static long ToInteger(this PhysicalAddress mac) {
        string sValue = mac.ToFormattedString(separator: "-").Replace("-", string.Empty);
        return long.Parse(sValue, System.Globalization.NumberStyles.HexNumber);
    }

    /// <summary>Converts a string in EUI-48/64 notation to a <see cref="long"/> 
    /// which represents the given <paramref name="mac"/> address.<summary>
    /// <param name="mac">The MAC address in textual EUI-48/64 form</param>
    /// <param name="def">A default value in case <paramref name="mac"/> is null (defaults to 0)</param>
    /// <returns></returns>
    public static long ToMACAddressInteger(this string mac, long def = 0) {
        if (mac == null)
            return def;
        return mac.ToMACAddress().ToInteger();
    }

    /// <summary>Converts a long to its equivalent <see cref="mac"/> address</summary>
    /// <param name="mac">The <see cref="long"/> to be converted into a MAC address</param>
    /// <returns>The <see cref="PhysicalAddress"/> represented by the <paramref name="mac"/> <see cref="long"/> value</returns>
    public static PhysicalAddress ToMACAddress(this long mac) {
        var sMac = mac.ToString("x");
        // garantir que haja sempre uma quantidade par de bytes hexa (resolve o problema de "08:68:4C:F0:21:D1")
        if (sMac.Length % 2 > 0)
            sMac = "0" + sMac;
        return sMac.ToMACAddress();
    }

    /// <summary>Checks to see if the value <paramref name="mac"/> is a valid MAC address conforming to EUI-48 or EUI-64</summary>
    /// <param name="mac">The textual MAC address to test</param>
    /// <returns><see cref="true"/> if its a valida MAC Address representation, <see cref="false"/> otherwise.</returns>
    public static bool IsMACAddress(this string mac) {
        try {
            mac.ToMACAddress();
            return true;
        } catch (FormatException) {
            return false;
        }
    }

    /// <summary>Converts a string representing a host name ("google.com) or IP Address ("142.250.79.46") to 
    /// an <see cref="IPAddress"/> object, optionally opting to return a IpV6 address (defaults to IpV4). 
    /// You can pass the string "auto" to get the local IPv4 address (<see cref="GetLocalIPv4Address(string)"/>)</summary>
    /// <param name="hostNameOrAddress">Host name or address to convert into an <see cref="IPAddress"/></param>
    /// <param name="favorIpV6">When <c>true</c> will return an IpV6 address whenever available, otherwise 
    /// returns an IpV4 address instead.</param>
    /// <returns>The <see cref="IPAddress"/> represented by <paramref name="hostNameOrAddress"/> in either IpV4 or
    /// IpV6 (when available) format depending on <paramref name="favorIpV6"/>, or <see cref="null"/> if 
    /// <paramref name="hostNameOrAddress"/> is null.</returns>
    public static IPAddress? ToIPAddress(this string hostNameOrAddress, bool favorIpV6 = false) {
        if (string.IsNullOrWhiteSpace(hostNameOrAddress))
            return null;
        if (hostNameOrAddress.ToLowerInvariant() == "auto")
            return GetLocalIPv4Address();
        var favoredFamily = favorIpV6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
        var addrs = Dns.GetHostAddresses(hostNameOrAddress);
        return addrs.FirstOrDefault(addr => addr.AddressFamily == favoredFamily)
               ?? addrs.FirstOrDefault();
    }

    static readonly string[] prefixes = [@"https://", @"http://"];

    /// <summary>Removes the "http://" or "https://" part of a URL (if present).</summary>
    /// <param name="url">The string representing an URL with a possible http(s) part removed from it</param>
    /// <returns>The string with the http(s) prefix removed (or itself if no prefix is found)</returns>
    public static string? RemoveHttpPart(this string url) 
        => url?.TrimStart().TrimStart(prefixes, StringComparison.OrdinalIgnoreCase);

    /// <summary>Converts a string in the form {IP ADDRESS|HOST NAME}[:{PORT}] (192.168.0.2:8080) into an <see cref="IPEndPoint"/>. 
    /// Port is optional and defaults to <paramref name="defaultPort"/> (which defaults to 80). You can also pass
    /// an optional IP|Host/port <paramref name="separator"/> (defaults to ":")</summary>
    /// <param name="ipAddressWithPort">A string in the form IP/HOST:PORT</param>
    /// <param name="defaultPort">The default port to use if none was included in the string (defaults to 80)</param>
    /// <param name="separator">An optional IP/port separator (defaults to 80)</param>
    /// <returns>The parsed <see cref="IPEndPoint"/></returns>
    /// <exception cref="ArgumentNullException">Throws if <paramref name="ipAddressWithPort"/> is null</exception>
    public static IPEndPoint ToIPEndPoint(this string? ipAddressWithPort, int defaultPort = 80, char separator = ':') {
        if (ipAddressWithPort is null ||  string.IsNullOrWhiteSpace(ipAddressWithPort))
            throw new ArgumentNullException("ipAddressWithPort");
        ipAddressWithPort = ipAddressWithPort.RemoveHttpPart()!;
        var parts = ipAddressWithPort.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
        var ip = parts[0];
        int port = parts.Length > 1 ? Convert.ToInt32(parts[1]) : defaultPort;
        var ipAddr = ip.ToIPAddress();
        return new IPEndPoint(ipAddr!, port);
    }

    /// <summary>
    /// Separates a string in the form {IP|Host name}:{Port} in a <see cref="Tuple{T1, T2}"/> containing the IP or 
    /// Host (with the http(s):// part stripped) and port (if omitted will use <paramref name="defaultPort"/>)
    /// </summary>
    /// <param name="ipOrHostNameWithPort">The ip|host:port string to "separate"</param>
    /// <param name="defaultPort">The default port to use if no port was present in the string</param>
    /// <returns></returns>
    public static (string?, int) ToIpOrHostAndPort(this string? ipOrHostNameWithPort, int defaultPort = 80) {
        if (ipOrHostNameWithPort is null || string.IsNullOrWhiteSpace(ipOrHostNameWithPort))
            return (null, 0);
        var addr = ipOrHostNameWithPort.RemoveHttpPart()!;
        var parts = addr.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
        var port = parts.Length > 1 ? Convert.ToInt32(parts[1]) : defaultPort;
        return (parts[0], port);
    }

}
