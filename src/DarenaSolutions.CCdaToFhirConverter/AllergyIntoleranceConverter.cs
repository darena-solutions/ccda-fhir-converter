using System;
using System.Collections.Generic;
using System.Linq;
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
    public class AllergyIntoleranceConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllergyIntoleranceConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public AllergyIntoleranceConverter(string patientId)
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
                var authorElement = element.Elements(Defaults.DefaultNs + "author").FirstOrDefault();
                var observationsXPath = "n1:entryRelationship/n1:observation";
                var observations = element.XPathSelectElements(observationsXPath, namespaceManager);

                foreach (var observation in observations)
                {
                    var id = Guid.NewGuid();
                    var allergyIntolerance = new AllergyIntolerance
                    {
                        Id = id.ToString(),
                        Meta = new Meta(),
                        Patient = new ResourceReference($"urn:uuid:{_patientId}")
                    };

                    allergyIntolerance.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-allergyintolerance"));

                    var identifierElements = observation.Elements(Defaults.DefaultNs + "id");
                    foreach (var identifierElement in identifierElements)
                    {
                        allergyIntolerance.Identifier.Add(identifierElement.ToIdentifier());
                    }

                    var hasNoKnownDocumentedAllergies = false;
                    observation.TryGetAttribute("negationInd", out var attributeValue);
                    if (string.Compare(attributeValue, "true", StringComparison.InvariantCultureIgnoreCase) == 0)
                        hasNoKnownDocumentedAllergies = true;

                    if (hasNoKnownDocumentedAllergies)
                    {
                        allergyIntolerance.Code = new CodeableConcept("http://snomed.info/sct", "716186003");
                        allergyIntolerance.VerificationStatus = new CodeableConcept("http://terminology.hl7.org/CodeSystem/allergyintolerance-verification", "unconfirmed");
                    }
                    else
                    {
                        var effectiveTimeElement = observation.Element(Defaults.DefaultNs + "effectiveTime");
                        allergyIntolerance.Onset = effectiveTimeElement?.ToDateTimeElement();

                        CodeableConcept clinicalStatus = null;
                        var statusCodeElement = observation.Element(Defaults.DefaultNs + "statusCode");
                        if (statusCodeElement != null)
                        {
                            clinicalStatus = statusCodeElement.ToCodeableConcept();
                            clinicalStatus.Coding[0].System = "http://terminology.hl7.org/CodeSystem/allergyintolerance-clinical";

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
                                    throw new InvalidOperationException($"The clinical status code '{clinicalStatus.Coding[0].Code}' is invalid");
                            }
                        }

                        if (clinicalStatus != null)
                        {
                            allergyIntolerance.ClinicalStatus = clinicalStatus;
                            allergyIntolerance.VerificationStatus = new CodeableConcept(
                                "http://terminology.hl7.org/CodeSystem/allergyintolerance-verification", "confirmed");
                        }

                        var substanceCodeableConcept = observation
                            .FindCodeElementWithTranslation("n1:participant/n1:participantRole/n1:playingEntity", namespaceManager)?
                            .ToCodeableConcept();

                        if (substanceCodeableConcept == null)
                            throw new InvalidOperationException($"No substance element was found in: {observation}");

                        allergyIntolerance.Code = substanceCodeableConcept;

                        var reaction = new AllergyIntolerance.ReactionComponent();
                        var obsEntryRelationships = observation.Elements(Defaults.DefaultNs + "entryRelationship");
                        foreach (var obsEntryRelationship in obsEntryRelationships)
                        {
                            var templateIdValue = obsEntryRelationship
                                .Element(Defaults.DefaultNs + "observation")?
                                .Element(Defaults.DefaultNs + "templateId")?
                                .Attribute("root")?
                                .Value;

                            switch (templateIdValue)
                            {
                                case "2.16.840.1.113883.10.20.22.4.9":
                                    var manifestationCodeableConcept = obsEntryRelationship
                                        .FindCodeElementWithTranslation(
                                            "n1:observation",
                                            namespaceManager,
                                            "value")?
                                        .ToCodeableConcept();

                                    if (manifestationCodeableConcept == null && reaction.Manifestation.Count == 0)
                                        throw new InvalidOperationException($"No manifestation element was found in: {obsEntryRelationship}");

                                    reaction.Manifestation.Add(manifestationCodeableConcept);
                                    break;
                                case "2.16.840.1.113883.10.20.22.4.8":
                                    var severityCodeableConcept = obsEntryRelationship
                                        .FindCodeElementWithTranslation(
                                            "n1:observation",
                                            namespaceManager,
                                            "value")?
                                        .ToCodeableConcept();

                                    if (severityCodeableConcept == null)
                                        throw new InvalidOperationException($"No severity element found in: {obsEntryRelationship}");

                                    var severityDisplay = severityCodeableConcept.Coding.First().Display;
                                    var severity = EnumUtility.ParseLiteral<AllergyIntolerance.AllergyIntoleranceSeverity>(severityDisplay, true);
                                    if (severity == null)
                                        throw new InvalidOperationException($"Could not determine allergy intolerance severity from value '{severityDisplay}'");

                                    reaction.Severity = severity;
                                    break;
                                default:
                                    continue;
                            }
                        }

                        allergyIntolerance.Reaction.Add(reaction);
                    }

                    bundle.Entry.Add(new Bundle.EntryComponent
                    {
                        FullUrl = $"urn:uuid:{id}",
                        Resource = allergyIntolerance
                    });

                    // Provenance
                    if (authorElement == null)
                        continue;

                    var provenance = new ProvenanceConverter(ResourceType.AllergyIntolerance, id.ToString());
                    provenance.AddToBundle(
                        bundle,
                        new List<XElement> { authorElement },
                        namespaceManager,
                        cacheManager);
                }
            }
        }
    }
}
