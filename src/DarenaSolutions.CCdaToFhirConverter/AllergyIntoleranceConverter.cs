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
        protected override Resource PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            Dictionary<string, Resource> cache)
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
                            throw new UnrecognizedValueException(
                                statusCodeElement,
                                clinicalStatus.Coding[0].Code,
                                elementAttributeName: "code");
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
                {
                    throw new RequiredValueNotFoundException(
                        element,
                        "participant/participantRole/playingEntity/code",
                        "AllergyIntolerance.code");
                }

                allergyIntolerance.Code = substanceCodeableConcept;

                AllergyIntolerance.ReactionComponent reaction = null;
                var obsEntryRelationships = element
                    .Elements(Defaults.DefaultNs + "entryRelationship")
                    .ToList();

                foreach (var obsEntryRelationship in obsEntryRelationships)
                {
                    reaction ??= new AllergyIntolerance.ReactionComponent();

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

                            if (manifestationCodeableConcept != null)
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
                                continue;

                            var severityDisplay = severityCodeableConcept.Coding.First().Display;
                            reaction.Severity = EnumUtility.ParseLiteral<AllergyIntolerance.AllergyIntoleranceSeverity>(severityDisplay, true);
                            break;
                        default:
                            continue;
                    }
                }

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
                    cache);

                var provenance = provenanceResources.GetFirstResourceAsType<Provenance>();
                provenance.Target.Add(new ResourceReference($"urn:uuid:{id}"));
            }

            return allergyIntolerance;
        }
    }
}
