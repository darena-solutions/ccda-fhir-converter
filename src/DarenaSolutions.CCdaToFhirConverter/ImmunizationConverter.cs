﻿using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <inheritdoc />
    public class ImmunizationConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmunizationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public ImmunizationConverter(string patientId)
        {
            _patientId = patientId;
        }

        /// <inheritdoc />
        public void AddToBundle(
            Bundle bundle,
            IEnumerable<XElement> elements,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            foreach (var element in elements)
            {
                var id = Guid.NewGuid().ToString();
                var immunization = new Immunization
                {
                    Id = id,
                    Meta = new Meta(),
                    Patient = new ResourceReference($"urn:uuid:{_patientId}"),
                    PrimarySource = false
                };

                immunization.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-immunization"));

                var identifierElements = element.Elements(Defaults.DefaultNs + "id");
                foreach (var identifierElement in identifierElements)
                {
                    immunization.Identifier.Add(identifierElement.ToIdentifier());
                }

                var statusReasonXPath = "n1:entryRelationship/n1:observation/n1:templateId[@root='2.16.840.1.113883.10.20.22.4.53']/../n1:code";
                immunization.StatusReason = element.XPathSelectElement(statusReasonXPath, namespaceManager)?.ToCodeableConcept();

                var effectiveTimeElement = element.Element(Defaults.DefaultNs + "effectiveTime");
                var effectiveTime = effectiveTimeElement?.ToFhirDateTime();

                if (effectiveTime == null)
                    throw new InvalidOperationException($"An immunization occurrence date time could not be found in: {element}");

                immunization.Occurrence = effectiveTime;

                var manufacturedMaterialXPath = "n1:consumable/n1:manufacturedProduct/n1:templateId[@root='2.16.840.1.113883.10.20.22.4.54']/../n1:manufacturedMaterial";
                var manufacturedMaterialElement = element.XPathSelectElement(manufacturedMaterialXPath, namespaceManager);

                if (manufacturedMaterialElement == null)
                    throw new InvalidOperationException($"An immunization manufactured material was not found in: {element}");

                var manufacturedMaterialCodeElement = manufacturedMaterialElement.Element(Defaults.DefaultNs + "code");
                if (manufacturedMaterialCodeElement == null)
                    throw new InvalidOperationException($"An immunization vaccine code was not found in: {element}");

                immunization.VaccineCode = manufacturedMaterialCodeElement.ToCodeableConcept();

                var statusCodeValue = element
                    .Element(Defaults.DefaultNs + "statusCode")?
                    .Attribute("code")?
                    .Value;

                if (string.IsNullOrWhiteSpace(statusCodeValue))
                    throw new InvalidOperationException($"An immunization status could not be found in: {element}");

                var statusCode = EnumUtility.ParseLiteral<Immunization.ImmunizationStatusCodes>(statusCodeValue, true);
                if (statusCode == null)
                    throw new InvalidOperationException($"Could not determine immunization status code from '{statusCodeValue}'");

                immunization.Status = statusCode;
                immunization.Route = element.Element(Defaults.DefaultNs + "routeCode")?.ToCodeableConcept();
                immunization.Site = element.Element(Defaults.DefaultNs + "approachSiteCode")?.ToCodeableConcept();

                var representedOrganizationXPath = "n1:informant/n1:assignedEntity/n1:id[@root='FACILITY']/../n1:representedOrganization";
                var representedOrganizationElement = element.XPathSelectElement(representedOrganizationXPath, namespaceManager);

                if (representedOrganizationElement != null)
                {
                    var representedOrganizationConverter = new RepresentedOrganizationConverter();
                    representedOrganizationConverter.AddToBundle(
                        bundle,
                        new List<XElement> { representedOrganizationElement },
                        namespaceManager,
                        cacheManager);

                    immunization.Performer.Add(new Immunization.PerformerComponent
                    {
                        Actor = new ResourceReference($"urn:uuid:{representedOrganizationConverter.OrganizationId}")
                    });
                }

                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = immunization
                });
            }
        }
    }
}
