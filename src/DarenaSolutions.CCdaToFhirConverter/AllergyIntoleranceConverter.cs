// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="AllergyIntoleranceConverter.cs" company="Darena Solutions LLC">
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
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts various elements in the CCDA to allergy intolerance FHIR resources
    /// </summary>
    public class AllergyIntoleranceConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AllergyIntoleranceConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public AllergyIntoleranceConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='48765-2']/../n1:entry/n1:act/n1:entryRelationship/n1:observation";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var id = Guid.NewGuid().ToString();
            var allergyIntolerance = new AllergyIntolerance
            {
                Id = id,
                Meta = new Meta(),
                Patient = new ResourceReference($"urn:uuid:{PatientId}")
            };

            allergyIntolerance.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-allergyintolerance"));
            var cachedResource = element.SetIdentifiers(context, allergyIntolerance);
            if (cachedResource != null)
                return cachedResource;

            var hasNoKnownDocumentedAllergies = false;
            element.TryGetAttribute("negationInd", out var attributeValue);
            if (string.Compare(attributeValue, "true", StringComparison.InvariantCultureIgnoreCase) == 0)
                hasNoKnownDocumentedAllergies = true;

            if (hasNoKnownDocumentedAllergies)
            {
                allergyIntolerance.Code = new CodeableConcept("http://snomed.info/sct", "716186003");
                allergyIntolerance.VerificationStatus = new CodeableConcept("http://terminology.hl7.org/CodeSystem/allergyintolerance-verification", "unconfirmed");
            }
            else
            {
                var effectiveTimeElement = element.Element(Namespaces.DefaultNs + "effectiveTime");
                allergyIntolerance.Onset = effectiveTimeElement?.ToDateTimeElement();

                CodeableConcept clinicalStatus = null;
                var statusCodeElement = element.Element(Namespaces.DefaultNs + "statusCode");
                if (statusCodeElement != null)
                {
                    try
                    {
                        clinicalStatus = statusCodeElement.ToCodeableConcept("AllergyIntolerance.clinicalStatus");
                        clinicalStatus.Coding[0].System =
                            "http://terminology.hl7.org/CodeSystem/allergyintolerance-clinical";

                        switch (clinicalStatus.Coding[0].Code)
                        {
                            case "aborted":
                            case "completed":
                                clinicalStatus.Coding[0].Code = "resolved";
                                break;
                            case "suspended":
                                clinicalStatus.Coding[0].Code = "inactive";
                                break;
                            case "active":
                                break;
                            default:
                                throw new UnrecognizedValueException(
                                    statusCodeElement,
                                    clinicalStatus.Coding[0].Code,
                                    elementAttributeName: "code",
                                    fhirPropertyPath: "AllergyIntolerance.clinicalStatus.coding.code");
                        }
                    }
                    catch (Exception exception)
                    {
                        context.Exceptions.Add(exception);
                    }
                }

                if (clinicalStatus != null)
                {
                    allergyIntolerance.ClinicalStatus = clinicalStatus;
                    allergyIntolerance.VerificationStatus = new CodeableConcept(
                        "http://terminology.hl7.org/CodeSystem/allergyintolerance-verification", "confirmed");
                }

                try
                {
                    var substanceCodeableConcept = element
                        .FindCodeElementWithTranslation("n1:participant/n1:participantRole/n1:playingEntity", context.NamespaceManager)?
                        .ToCodeableConcept("AllergyIntolerance.code");

                    if (substanceCodeableConcept == null)
                    {
                        throw new RequiredValueNotFoundException(
                            element,
                            "participant/participantRole/playingEntity/code",
                            "AllergyIntolerance.code");
                    }

                    allergyIntolerance.Code = substanceCodeableConcept;
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }

                AllergyIntolerance.ReactionComponent reaction = null;
                var obsEntryRelationships = element
                    .Elements(Namespaces.DefaultNs + "entryRelationship")
                    .ToList();

                foreach (var obsEntryRelationship in obsEntryRelationships)
                {
                    reaction ??= new AllergyIntolerance.ReactionComponent();

                    var templateIdValue = obsEntryRelationship
                        .Element(Namespaces.DefaultNs + "observation")?
                        .Element(Namespaces.DefaultNs + "templateId")?
                        .Attribute("root")?
                        .Value;

                    try
                    {
                        switch (templateIdValue)
                        {
                            case "2.16.840.1.113883.10.20.22.4.9":
                                var manifestationCodeableConcept = obsEntryRelationship
                                    .FindCodeElementWithTranslation(
                                        "n1:observation",
                                        context.NamespaceManager,
                                        "value")?
                                    .ToCodeableConcept("AllergyIntolerance.reaction.manifestation");

                                if (manifestationCodeableConcept != null)
                                    reaction.Manifestation.Add(manifestationCodeableConcept);
                                break;
                            case "2.16.840.1.113883.10.20.22.4.8":
                                var severityCodeableConcept = obsEntryRelationship
                                    .FindCodeElementWithTranslation(
                                        "n1:observation",
                                        context.NamespaceManager,
                                        "value")?
                                    .ToCodeableConcept("AllergyIntolerance.reaction.severity");

                                if (severityCodeableConcept == null)
                                    continue;

                                var severityDisplay = severityCodeableConcept.Coding.First().Display;
                                reaction.Severity = EnumUtility.ParseLiteral<AllergyIntolerance.AllergyIntoleranceSeverity>(severityDisplay, true);
                                break;
                            default:
                                continue;
                        }
                    }
                    catch (Exception exception)
                    {
                        context.Exceptions.Add(exception);
                    }
                }

                try
                {
                    if (reaction != null)
                    {
                        if (!reaction.Manifestation.Any())
                        {
                            throw new RequiredValueNotFoundException(
                                obsEntryRelationships[0].Parent,
                                $"{obsEntryRelationships[0].Name.LocalName}[*]/observation/value",
                                "AllergyIntolerance.reaction");
                        }

                        allergyIntolerance.Reaction.Add(reaction);
                    }
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }

                try
                {
                    // Provenance
                    var authorElement = element.Elements(Namespaces.DefaultNs + "author").FirstOrDefault();
                    if (authorElement == null)
                        return allergyIntolerance;

                    var provenanceConverter = new ProvenanceConverter(PatientId);
                    var provenanceResources = provenanceConverter.AddToBundle(new List<XElement> { authorElement }, context);

                    var provenance = provenanceResources.GetFirstResourceAsType<Provenance>();
                    provenance.Target.Add(new ResourceReference($"urn:uuid:{id}"));
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            context.Bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = allergyIntolerance
            });

            return allergyIntolerance;
        }
    }
}
