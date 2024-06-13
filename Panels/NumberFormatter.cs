using System.Collections.Generic;
using System.Linq;

namespace Ranger2
{
    public static class NumberFormatter
    {
        private class Magnitude
        {
            public readonly string Suffix;
            public readonly string PluralSuffix;
            public readonly string FormatString;

            public Magnitude(string suffix, string pluralSuffix, string formatString)
            {
                Suffix = suffix;
                PluralSuffix = pluralSuffix;
                FormatString = formatString;
            }
        }

        private static readonly List<Magnitude> s_magnitudes = 
        [
            new ("byte", "bytes", "N0"),
            new ("KiB",  "KiB",   "N0"),
            new ("MiB",  "MiB",   "N1"),
            new ("GiB",  "GiB",   "N2"),
            new ("TiB",  "TiB",   "N2")
        ];

        public static string FormatBytes(ulong bytes)
        {
            float bytesAsFloat = bytes;
            Magnitude bestMagnitude = null;

            foreach (var magnitude in s_magnitudes)
            {
                if (bytesAsFloat > 1024)
                {
                    bytesAsFloat /= 1024;
                }
                else
                {
                    bestMagnitude = magnitude;
                    break;
                }
            }

            bestMagnitude = bestMagnitude ?? s_magnitudes.Last();

            return $"{bytesAsFloat.ToString(bestMagnitude.FormatString)} {(bytesAsFloat == 1 ? bestMagnitude.Suffix : bestMagnitude.PluralSuffix)}";
        }
    }
}
