using System;
using System.Globalization;

namespace DarenaSolutions.CCdaToFhirConverter.Extensions
{
    /// <summary>
    /// A class that contains extensions to the <c>string</c> primitive type
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Takes a CCDA date value and parses it into a <see cref="DateTimeOffset"/> object
        /// </summary>
        /// <param name="self">The source CCDA date value</param>
        /// <returns>The parsed <see cref="DateTimeOffset"/> object</returns>
        public static DateTimeOffset ParseCCdaDateTimeOffset(this string self)
        {
            return DateTimeOffset.ParseExact(
                self,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        /// <summary>
        /// Takes a CCDA date value and parses it into a <see cref="DateTime"/> object
        /// </summary>
        /// <param name="self">The source CCDA date value</param>
        /// <returns>The parsed <see cref="DateTime"/> object</returns>
        public static DateTime ParseCCdaDateTime(this string self)
        {
            return DateTime.ParseExact(
                self,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        /// <summary>
        /// Indicates if the source string has a valid null flavor value based on the defined values in: https://www.hl7.org/fhir/v3/NullFlavor/vs.html
        /// </summary>
        /// <param name="self">The source string</param>
        /// <returns><c>true</c> if the source string is a valid null flavor value</returns>
        public static bool IsValidNullFlavorValue(this string self)
        {
            switch (self)
            {
                case "NI":
                case "INV":
                case "DER":
                case "OTH":
                case "NINF":
                case "PINF":
                case "UNC":
                case "MSK":
                case "NA":
                case "UNK":
                case "ASKU":
                case "NAV":
                case "NASK":
                case "NAVU":
                case "QS":
                case "TRC":
                case "NP":
                    return true;
                default:
                    return false;
            }
        }
    }
}
