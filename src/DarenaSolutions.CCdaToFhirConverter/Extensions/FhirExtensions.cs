// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="FhirExtensions.cs" company="Darena Solutions LLC">
// Copyright (c) Darena Solutions LLC. All rights reserved.
// Licensed under the Apache License Version 2.0. See LICENSE file in the project root for full license information.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Retrieves the first resource in the list cast to a specific type
        /// </summary>
        /// <typeparam name="T">The type to cast to</typeparam>
        /// <param name="self">The source list of resources</param>
        /// <returns>The first resource in the list cast to the specified type</returns>
        public static T GetFirstResourceAsType<T>(this List<Resource> self)
            where T : Resource
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            return (T)self.First();
        }
    }
}
