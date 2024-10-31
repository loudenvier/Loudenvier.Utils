using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Loudenvier.Utils;

public static class SqlServerErrorHelpers
{
    // This regex tries to account for differences of most languages but it's only tested on English!
    static readonly Lazy<Regex> infoGrabber = new(() =>
        new Regex(@"(?<= \"").*?(?=\"")|(?<= \').*?(?=\')|(?<= \«).*?(?=\»)|(?<= \„).*?(?=\”)|(?<= \().*?(?=\))", 
            RegexOptions.Compiled)
    );
    static Regex InfoGrabber => infoGrabber.Value;

    public static IEnumerable<string> GrabErrorInfo(this string message) {
        if (message is null || string.IsNullOrWhiteSpace(message))
            return Enumerable.Empty<string>();
        var captures = InfoGrabber.Matches(message);
        if (captures is null)
            return Enumerable.Empty<string>();
        return captures.AsStrings();
    }
}
