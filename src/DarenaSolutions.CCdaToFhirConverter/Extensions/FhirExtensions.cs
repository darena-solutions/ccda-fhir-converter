using System;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter.Extensions
{
    /// <summary>
    /// A class that contains extensions to FHIR related models
    /// </summary>
    public static class FhirExtensions
    {
        /// <summary>
        /// Converts a FHIR date time to a date, which removes the time portion
        /// </summary>
        /// <param name="self">The source FHIR date time value</param>
        /// <returns>The date representation of the source FHIR date time value</returns>
        public static Date ToDate(this FhirDateTime self)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            var offset = self.ToDateTimeOffset(TimeSpan.Zero);
            return new Date(offset.Year, offset.Month, offset.Day);
        }
    }
}
