// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="ProfileRelatedException.cs" company="Darena Solutions LLC">
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
    /// An exception that is thrown when a profile related issue occurs on a particular element such as the cardinality
    /// or some invariant related constraint
    /// </summary>
    public class ProfileRelatedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileRelatedException"/> class
        /// </summary>
        /// <param name="element">The source element the issue occurred on</param>
        /// <param name="message">The error message</param>
        /// <param name="additionalXPath">Optionally specify if an additional xpath was applied to <paramref name="element"/>
        /// to get to the source element</param>
        /// <param name="fhirPropertyPath">Optionally specify the path to the FHIR property that can provide additional
        /// context to this exception</param>
        public ProfileRelatedException(XElement element, string message, string additionalXPath = null, string fhirPropertyPath = null)
            : base(CreateDefaultMessage(element, message, additionalXPath, fhirPropertyPath))
        {
            Element = element;
            AdditionalXPath = additionalXPath;
            FhirPropertyPath = fhirPropertyPath;
        }

        /// <summary>
        /// Gets the source element the issue occurred on
        /// </summary>
        public XElement Element { get; }

        /// <summary>
        /// Gets an additional xpath that was applied to <see cref="Element"/> to get to the source element
        /// </summary>
        public string AdditionalXPath { get; }

        /// <summary>
        /// Gets the path to the FHIR property that can provide additional context to this exception
        /// </summary>
        public string FhirPropertyPath { get; }

        private static string CreateDefaultMessage(
            XElement element,
            string message,
            string additionalXPath,
            string fhirPropertyPath)
        {
            var fullPath = element.GetAbsolutePath();
            if (!string.IsNullOrWhiteSpace(additionalXPath))
                fullPath += additionalXPath.StartsWith("/") ? additionalXPath : $"/{additionalXPath}";

            var errorMessage = $"{message} | Source: {fullPath}";
            if (!string.IsNullOrWhiteSpace(fhirPropertyPath))
                errorMessage += $" | FHIR property context: {fhirPropertyPath}";

            return errorMessage;
        }
    }
}
