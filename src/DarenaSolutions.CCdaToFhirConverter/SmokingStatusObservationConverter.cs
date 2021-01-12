// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="SmokingStatusObservationConverter.cs" company="Darena Solutions LLC">
// Copyright (c) Darena Solutions LLC. All rights reserved.
// Licensed under the Apache License Version 2.0. See LICENSE file in the project root for full license information.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Exceptions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts smoking status elements into FHIR observation resources
    /// </summary>
    public class SmokingStatusObservationConverter : BaseObservationConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmokingStatusObservationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public SmokingStatusObservationConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='29762-2']/../n1:entry/n1:observation/n1:code[@code='72166-2']/..";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var observation = (Observation)base.PerformElementConversion(element, context);
            observation.Meta = new Meta();
            observation.Meta.ProfileElement.Add(new FhirUri("http://hl7.org/fhir/us/core/StructureDefinition/us-core-smokingstatus"));

            try
            {
                if (observation.Value == null)
                    throw new RequiredValueNotFoundException(element, "value", "Observation.value");

                var valueEl = element.Element(Namespaces.DefaultNs + "value");
                if (!(observation.Value is CodeableConcept))
                {
                    throw new UnexpectedValueTypeException(
                        valueEl,
                        valueEl.Attribute(Namespaces.XsiNs + "type").Value,
                        "Observation.value");
                }
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            observation
                .Category
                .First()
                .Coding
                .First()
                .Code = "social-history";

            if (observation.Effective == null)
            {
                try
                {
                    throw new RequiredValueNotFoundException(element, "effectiveTime", "Observation.effective");
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            if (observation.Effective is FhirDateTime dateTimeElement)
                observation.Issued = dateTimeElement.ToDateTimeOffset(TimeSpan.Zero);
            else if (observation.Effective is Period periodElement)
                observation.Issued = periodElement.StartElement.ToDateTimeOffset(TimeSpan.Zero);

            observation.Effective = null;
            return observation;
        }
    }
}
