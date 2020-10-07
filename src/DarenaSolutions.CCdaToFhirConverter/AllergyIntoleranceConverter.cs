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
        protected override Resource PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            var id = Guid.NewGuid().ToString();
            var allergyIntolerance = new AllergyIntolerance
            {
                Id = id,
                Meta = new Meta(),
                Patient = new ResourceReference($"urn:uuid:{PatientId}")
            };

            allergyIntolerance.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-allergyintolerance"));

            var identifierElements = element.Elements(Defaults.DefaultNs + "id");
            foreach (var identifierElement in identifierElements)
            {
                allergyIntolerance.Identifier.Add(identifierElement.ToIdentifier());
            }

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
                var effectiveTimeElement = element.Element(Defaults.DefaultNs + "effectiveTime");
                allergyIntolerance.Onset = effectiveTimeElement?.ToDateTimeElement();

                CodeableConcept clinicalStatus = null;
                var statusCodeElement = element.Element(Defaults.DefaultNs + "statusCode");
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

                var substanceCodeableConcept = element
                    .FindCodeElementWithTranslation("n1:participant/n1:participantRole/n1:playingEntity", namespaceManager)?
                    .ToCodeableConcept();

                if (substanceCodeableConcept == null)
                    throw new InvalidOperationException($"No substance element was found in: {element}");

                allergyIntolerance.Code = substanceCodeableConcept;

                var reaction = new AllergyIntolerance.ReactionComponent();
                var obsEntryRelationships = element.Elements(Defaults.DefaultNs + "entryRelationship");
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

                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = allergyIntolerance
                });

                // Provenance
                var authorElement = element.Elements(Defaults.DefaultNs + "author").FirstOrDefault();
                if (authorElement == null)
                    return allergyIntolerance;

                var provenanceConverter = new ProvenanceConverter(PatientId);
                var provenanceResources = provenanceConverter.AddToBundle(
                    bundle,
                    new List<XElement> { authorElement },
                    namespaceManager,
                    cacheManager);

                var provenance = provenanceResources.GetFirstResourceAsType<Provenance>();
                provenance.Target.Add(new ResourceReference($"{ResourceType.AllergyIntolerance}/{id}"));
            }

            return allergyIntolerance;
        }
    }
}
