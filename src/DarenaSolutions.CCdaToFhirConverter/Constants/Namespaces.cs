﻿// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="Namespaces.cs" company="Darena Solutions LLC">
// Copyright (c) Darena Solutions LLC. All rights reserved.
// Licensed under the Apache License Version 2.0. See LICENSE file in the project root for full license information.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;

namespace DarenaSolutions.CCdaToFhirConverter.Constants
{
    /// <summary>
    /// A class that contains various namespaces that are used throughout this library
    /// </summary>
    public static class Namespaces
    {
        /// <summary>
        /// The default namespace for a CCDA document
        /// </summary>
        public static readonly XNamespace DefaultNs = "urn:hl7-org:v3";

        /// <summary>
        /// The STDC namespace for a CCDA document
        /// </summary>
        public static readonly XNamespace SdtcNs = "urn:hl7-org:sdtc";

        /// <summary>
        /// The XsiNs namespace for a CCDA document
        /// </summary>
        public static readonly XNamespace XsiNs = "http://www.w3.org/2001/XMLSchema-instance";
    }
}
