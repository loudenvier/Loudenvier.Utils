using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Loudenvier.Utils
{
    public class NaturalLanguageTimeParser
    {
        enum PartKind { Day, Hour, Min, Sec, MSec };

        public class Patterns {
            private static readonly RegexOptions opts = RegexOptions.IgnoreCase | RegexOptions.Compiled;
            public Regex PartsGrabber { get; init; } = new(@"(-?\s?\d+)([\sA-Za-z]+)", opts);
            public Regex DayMatcher { get; init; } = new(@"\bdays*\b|\bds*\b", opts);
            public Regex HourMatcher { get; init; } = new(@"\bhours*\b|\bhs*\b", opts);
            public Regex MinuteMatcher { get; init; } = new(@"\bminutes*|\bmins*\b|\bm\b", opts);
            public Regex SecondMatcher { get; init; } = new(@"\bseconds*|\bsecs*\b|\bs\b", opts);
            public Regex MillisecondMatcher { get; init; } = new(@"\bmilliseconds*|\bmsecs*\b|\bmillis*\b|\bms\b", opts);

            public static Patterns Pt = new() {
                DayMatcher = new Regex(@"\bdias*\b|\bds*\b", opts),
                HourMatcher = new Regex(@"\bhoras*\b|\bhs*\b", opts),
                MinuteMatcher = new Regex(@"\bminutos*|\bmins*\b|\bm\b", opts),
                SecondMatcher = new Regex(@"\bsegundos*|\bsegs*\b|\bs\b", opts),
                MillisecondMatcher = new Regex(@"\bmilissegundos*|\bmsegs*\b|\bmilis*\b|\bms\b", opts),
            };

            public static Patterns En = new();

            public static Patterns Default = Pt;

            public static Dictionary<string, Patterns> Language = new() {
                { "pt", Pt },
                { "en", En },
            };
        }

        public NaturalLanguageTimeParser(Patterns patterns) { ParserPatterns = patterns ?? Patterns.Default; }
        public NaturalLanguageTimeParser(string languageCode) : this(Patterns.Language[languageCode]) { }

        public Patterns ParserPatterns { get; private set; }

        public TimeSpan Execute(string timeStr) {
            MatchCollection parts = ParserPatterns.PartsGrabber.Matches(timeStr);
            if (parts.Count < 1) 
                throw new FormatException($"The textual time spandefition is invalid: {timeStr}");
            int? days = null;
            int? hours = null;
            int? mins = null;
            int? secs = null;
            int? msecs = null;

            foreach (Match part in parts) {
                if (part.Groups.Count < 3)
                    throw new FormatException($"The textual timespan definition is invalid: {timeStr} (part: {part.Value})");
                var val = part.Groups[1].Value.To<int>();
                var kind = part.Groups[2].Value.To<string>();
                var partKind = ParseKind(kind);
                switch (partKind) {
                    case PartKind.Day:
                        if (days.HasValue)
                            throw new FormatException("The day component can't happen more than once");
                        days = val;
                        break;
                    case PartKind.Hour:
                        if (hours.HasValue)
                            throw new FormatException("The hour component can't happen more than once");
                        hours = val;
                        break;
                    case PartKind.Min:
                        if (mins.HasValue)
                            throw new FormatException("The minute component can't happen more than once");
                        mins = val;
                        break;
                    case PartKind.Sec:
                        if (secs.HasValue)
                            throw new FormatException("The second component can't happen more than once");
                        secs = val;
                        break;
                    case PartKind.MSec:
                        if (msecs.HasValue)
                            throw new FormatException("The millisecond component can't happen more than once");
                        msecs = val;
                        break;
                }
            }

            // TODO: Some additional consistency checks here are needed as you can't have positive days with negative hours!!
            if (days < 0 || hours < 0 || mins < 0) {
                days = -Math.Abs(days ?? 0);
                hours = -Math.Abs(hours ?? 0);
                mins = -Math.Abs(mins ?? 0);
                secs = -Math.Abs(secs ?? 0);
                msecs = -Math.Abs(msecs ?? 0);
            }

            return new TimeSpan(days ?? 0, hours ?? 0, mins ?? 0, secs ?? 0, msecs ?? 0);
        }

        private PartKind ParseKind(string? kind) {
            if (ParserPatterns.DayMatcher.IsMatch(kind))
                return PartKind.Day;
            if (ParserPatterns.HourMatcher.IsMatch(kind))
                return PartKind.Hour;
            if (ParserPatterns.MinuteMatcher.IsMatch(kind))
                return PartKind.Min;
            if (ParserPatterns.SecondMatcher.IsMatch(kind))
                return PartKind.Sec;
            if (ParserPatterns.MillisecondMatcher.IsMatch(kind))
                return PartKind.MSec;
            throw new FormatException("The time duration definition is invalid: " + kind);
        }
        
    }

    
}
