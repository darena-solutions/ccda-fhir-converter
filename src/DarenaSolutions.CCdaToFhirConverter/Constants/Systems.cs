﻿// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="Systems.cs" company="Darena Solutions LLC">
// Copyright (c) Darena Solutions LLC. All rights reserved.
// Licensed under the Apache License Version 2.0. See LICENSE file in the project root for full license information.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

namespace DarenaSolutions.CCdaToFhirConverter.Constants
{
    /// <summary>
    /// Class that contains various system values that are used throughout this library
    /// </summary>
    public static class Systems
    {
        /// <summary>
        /// The name of the system for null flavors
        /// </summary>
        public const string NullFlavorSystem = "http://hl7.org/fhir/StructureDefinition/iso21090-nullFlavor";

        /// <summary>
        /// The name of the system that indicates a temporary system value for some identifiers. This system value should
        /// be updated to a more concrete and defined system before uploading and committing to a fhir server
        /// </summary>
        public const string SampleBbpSystem = "https://terminology.bluebuttonpro.com/SampleFhirServerId";
    }
}
