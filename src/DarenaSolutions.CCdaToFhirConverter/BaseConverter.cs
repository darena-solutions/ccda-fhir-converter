// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="BaseConverter.cs" company="Darena Solutions LLC">
// Copyright (c) Darena Solutions LLC. All rights reserved.
// Licensed under the Apache License Version 2.0. See LICENSE file in the project root for full license information.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// The base converter that all other converters must derive from
    /// </summary>
    public abstract class BaseConverter : IResourceConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        protected BaseConverter(string patientId)
        {
            PatientId = patientId;
        }

        /// <summary>
        /// Gets the id of the patient referenced in the CCDA
        /// </summary>
        protected string PatientId { get; }

        /// <inheritdoc />
        /// <remarks>
        /// This method is used in situations where the elements to be converted are not known, and the converter must find
        /// these elements. The method, <see cref="GetPrimaryElements"/>, is called in this overload which will then lead
        /// to <see cref="PerformElementConversion"/> being called for each element
        /// </remarks>
        public virtual List<Resource> AddToBundle(XDocument cCda, ConversionContext context)
        {
            var resources = new List<Resource>();

            var elements = GetPrimaryElements(cCda, context.NamespaceManager);
            foreach (var element in elements)
            {
                if (element == null)
                    continue;

                resources.Add(PerformElementConversion(element, context));
            }

            return resources;
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method is used in situations where the elements have already been retrieved and the converter does not
        /// need to perform finding elements. In this overload, <see cref="GetPrimaryElements"/> IS NOT called and is skipped.
        /// For each element, <see cref="PerformElementConversion"/> will be called
        /// </remarks>
        public virtual List<Resource> AddToBundle(IEnumerable<XElement> elements, ConversionContext context)
        {
            var resources = new List<Resource>();

            foreach (var element in elements)
            {
                if (element == null)
                    continue;

                resources.Add(PerformElementConversion(element, context));
            }

            return resources;
        }

        /// <summary>
        /// Retrieves the primary elements that will need to be converted by the converter. This method will be called by
        /// the <c>AddToBundle</c> overload that accepts a single XDocument. This method is used in situations where the
        /// elements to convert are not known beforehand and the converter must find those elements
        /// </summary>
        /// <param name="cCda">The root CCDA document</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the root CCDA document</param>
        /// <returns>The primary elements that will need to be converted by the converter</returns>
        protected abstract IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager);

        /// <summary>
        /// This method is called for each iteration of an element. This is the method that should perform actual conversion
        /// from a CCDA element to a FHIR resource
        /// </summary>
        /// <param name="element">The element to convert</param>
        /// <param name="context">The conversion context that contains necessary data to perform conversion</param>
        /// <returns>The resource that was converted and added to the bundle</returns>
        protected abstract Resource PerformElementConversion(XElement element, ConversionContext context);
    }
}
