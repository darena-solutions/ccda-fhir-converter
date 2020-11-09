// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="ConverterFactoryContext.cs" company="Darena Solutions LLC">
// Copyright (c) Darena Solutions LLC. All rights reserved.
// Licensed under the Apache License Version 2.0. See LICENSE file in the project root for full license information.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// The context that is passed down to converter factories when registering the converters in the <see cref="CCdaToFhirExecutor"/>.
    /// This is to provide additional context for creating a converter in a specific manner
    /// </summary>
    public class ConverterFactoryContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConverterFactoryContext"/> class
        /// </summary>
        /// <param name="patientId">The id of the primary patient in the CCDA</param>
        /// <param name="organizationId">The id of the primary organization in the CCDA</param>
        /// <param name="bundle">The current bundle that all converters are adding on to</param>
        public ConverterFactoryContext(
            string patientId,
            string organizationId,
            Bundle bundle)
        {
            PatientId = patientId;
            OrganizationId = organizationId;
            Bundle = bundle;
        }

        /// <summary>
        /// Gets the id of the primary patient in the CCDA
        /// </summary>
        public string PatientId { get; }

        /// <summary>
        /// Gets the id of the primary organization in the CCDA
        /// </summary>
        public string OrganizationId { get; }

        /// <summary>
        /// Gets the current bundle that all converters are adding on to
        /// </summary>
        public Bundle Bundle { get; }
    }
}
