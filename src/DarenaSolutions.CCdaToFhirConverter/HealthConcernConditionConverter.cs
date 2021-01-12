// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="HealthConcernConditionConverter.cs" company="Darena Solutions LLC">
// Copyright (c) Darena Solutions LLC. All rights reserved.
// Licensed under the Apache License Version 2.0. See LICENSE file in the project root for full license information.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Exceptions;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts various elements in the CCDA to condition FHIR resources. This converter is specifically
    /// for health concern sections of the CCDA
    /// </summary>
    public class HealthConcernConditionConverter : BaseConditionConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HealthConcernConditionConverter"/> class.
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public HealthConcernConditionConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='75310-3']/../n1:entry/n1:act";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var condition = (Condition)base.PerformElementConversion(element, context);

            try
            {
                // Get the value from the observation element
                var xPath = "../../n1:entry/n1:observation/n1:value";
                var valueEl = element
                    .XPathSelectElement(xPath, context.NamespaceManager)?
                    .ToFhirDataTypeBasedOnType(new[] { "co", "cd" }, "Condition.code");

                if (valueEl == null)
                {
                    throw new RequiredValueNotFoundException(
                        element,
                        "../../entry/observation/value",
                        "Condition.code");
                }

                condition.Code = (CodeableConcept)valueEl;
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            condition.Category.Add(new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/condition-category",
                "health-concern",
                "Health Concern",
                null));

            return condition;
        }
    }
}
