// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="ImmunizationConverter.cs" company="Darena Solutions LLC">
// Copyright (c) Darena Solutions LLC. All rights reserved.
// Licensed under the Apache License Version 2.0. See LICENSE file in the project root for full license information.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Exceptions;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts various elements in the CCDA into immunization FHIR resources
    /// </summary>
    public class ImmunizationConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmunizationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public ImmunizationConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='11369-6']/../n1:entry/n1:substanceAdministration";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var id = Guid.NewGuid().ToString();
            var immunization = new Immunization
            {
                Id = id,
                Meta = new Meta(),
                Patient = new ResourceReference($"urn:uuid:{PatientId}"),
                PrimarySource = true
            };

            immunization.Meta.ProfileElement.Add(new FhirUri("http://hl7.org/fhir/us/core/StructureDefinition/us-core-immunization"));
            var cachedResource = element.SetIdentifiers(context, immunization);
            if (cachedResource != null)
                return cachedResource;

            var status = element
                .Element(Namespaces.DefaultNs + "statusCode")?
                .Attribute("code")?
                .Value;

            immunization.Status = status == "completed"
                ? Immunization.ImmunizationStatusCodes.Completed
                : Immunization.ImmunizationStatusCodes.NotDone;

            try
            {
                immunization.Occurrence = element
                    .Element(Namespaces.DefaultNs + "effectiveTime")?
                    .ToFhirDateTime();

                if (immunization.Occurrence == null)
                {
                    throw new RequiredValueNotFoundException(
                        element,
                        "effectiveTime",
                        "Immunization.occurrence");
                }
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            var manufacturedMaterialXPath = "n1:consumable/n1:manufacturedProduct/n1:manufacturedMaterial";
            var manufacturedMaterialEl = element.XPathSelectElement(manufacturedMaterialXPath, context.NamespaceManager);
            if (manufacturedMaterialEl != null)
            {
                try
                {
                    immunization.VaccineCode = manufacturedMaterialEl
                        .Element(Namespaces.DefaultNs + "code")?
                        .ToCodeableConcept("Immunization.vaccineCode");
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }

                immunization.LotNumber = manufacturedMaterialEl
                    .Element(Namespaces.DefaultNs + "lotNumberText")?
                    .GetFirstTextNode();
            }

            if (immunization.VaccineCode == null)
            {
                try
                {
                    throw new RequiredValueNotFoundException(
                        element,
                        "consumable/manufacturedProduct/manufacturedMaterial/code",
                        "Immunization.vaccineCode");
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            try
            {
                var statusReasonCodeXPath = "n1:entryRelationship/n1:observation/n1:code";
                immunization.StatusReason = element
                    .XPathSelectElement(statusReasonCodeXPath, context.NamespaceManager)?
                    .ToCodeableConcept("Immunization.statusReason");
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            context.Bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = immunization
            });

            return immunization;
        }
    }
}
