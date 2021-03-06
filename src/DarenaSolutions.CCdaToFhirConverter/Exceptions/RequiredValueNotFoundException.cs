﻿// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="RequiredValueNotFoundException.cs" company="Darena Solutions LLC">
// Copyright (c) Darena Solutions LLC. All rights reserved.
// Licensed under the Apache License Version 2.0. See LICENSE file in the project root for full license information.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

using System;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Extensions;

namespace DarenaSolutions.CCdaToFhirConverter.Exceptions
{
    /// <summary>
    /// An exception that is thrown when a value is required, but the element in the CCDA that contains the value is not
    /// found
    /// </summary>
    public class RequiredValueNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequiredValueNotFoundException"/> class
        /// </summary>
        /// <param name="parent">The parent element that should have contained the desired element</param>
        /// <param name="xPathToRequired">the XPath to the required element that was not found from the context of <paramref name="parent"/></param>
        /// <param name="fhirPropertyPath">Optionally specify the path to the FHIR property that requires the value</param>
        /// <param name="message">Optionally specify a custom error message if the default error message is not desired</param>
        public RequiredValueNotFoundException(
            XElement parent,
            string xPathToRequired,
            string fhirPropertyPath = null,
            string message = null)
            : base(CreateDefaultMessage(parent, xPathToRequired, fhirPropertyPath, message))
        {
            Parent = parent;
            XPathToRequired = xPathToRequired;
            FhirPropertyPath = fhirPropertyPath;
        }

        /// <summary>
        /// Gets the parent element that should have contained the desired element
        /// </summary>
        public XElement Parent { get; }

        /// <summary>
        /// Gets the XPath to the required element that was not found from the context of <see cref="Parent"/>
        /// </summary>
        public string XPathToRequired { get; }

        /// <summary>
        /// Gets the path to the FHIR property that required the value
        /// </summary>
        public string FhirPropertyPath { get; }

        private static string CreateDefaultMessage(XElement parent, string xPathToRequired, string fhirPropertyPath, string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                return message;

            var fullPath = parent.GetAbsolutePath();
            if (!string.IsNullOrWhiteSpace(xPathToRequired))
            {
                fullPath += xPathToRequired.StartsWith("/") || xPathToRequired.StartsWith("[")
                    ? xPathToRequired
                    : $"/{xPathToRequired}";
            }

            var errorMessage = $"Required value at path '{fullPath}' could not be found";
            if (!string.IsNullOrWhiteSpace(fhirPropertyPath))
                errorMessage += $". Value required for FHIR property(ies): {fhirPropertyPath}";

            return errorMessage;
        }
    }
}
