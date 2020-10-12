using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts various elements in the CCDA to an encounter FHIR resource.
    /// </summary>
    public class EncounterConverter : BaseConditionConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncounterConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public EncounterConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='46240-8']/.." + "/n1:entry/n1:encounter";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            Dictionary<string, Resource> cache)
        {
            // Encounter
            var id = Guid.NewGuid().ToString();
            var encounter = new Encounter
            {
                Id = id,
                Meta = new Meta(),
                Subject = new ResourceReference($"urn:uuid:{PatientId}"),
                Status = Encounter.EncounterStatus.Finished,
                Class = new Coding("2.16.840.1.113883.5.4", "AMB")
            };

            encounter.Meta.ProfileElement.Add(
                new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-encounter"));

            var identifierElements =
                element.Elements(Defaults.DefaultNs + "id");
            foreach (var identifierElement in identifierElements)
            {
                encounter.Identifier.Add(identifierElement.ToIdentifier());
            }

            // Class - Override when found
            var translationCode = element
                .FindCodeElementWithTranslation(translationOnly: true)?
                .ToCodeableConcept();
            if (translationCode?.Coding?.Count > 0)
                encounter.Class = new Coding(translationCode.Coding[0].System, translationCode.Coding[0].Code);

            // Type - CPT Code
            var encounterCode = element.ToCodeableConcept();
            encounter.Type.Add(encounterCode);

            // Period
            var effectiveTimeElement =
                element.Element(Defaults.DefaultNs + "effectiveTime");
            encounter.Period = effectiveTimeElement?.ToPeriod();

            // Diagnoses
            var encounterDiagnosisElements =
                element.Elements("n1:entryRelationship/n1:act/n1:entryRelationship/n1:observation");
            var encounterDiagnosesConverter = new EncounterDiagnosesConditionConverter(PatientId);
            var encounterDiagnoses = encounterDiagnosesConverter.AddToBundle(
                bundle,
                encounterDiagnosisElements,
                namespaceManager,
                cache);

            if (encounterDiagnoses?.Count > 0)
            {
                foreach (var condition in encounterDiagnoses)
                {
                    encounter.Diagnosis.Add(new Encounter.DiagnosisComponent
                    {
                        Condition = new ResourceReference($"urn:uuid:{condition.Id}")
                    });
                }
            }

            // Practitioner - Author
            var practitionerElement =
                element.XPathSelectElements("/n1:ClinicalDocument/n1:author/n1:assignedAuthor")
                .FirstOrDefault();
            var identifier =
                practitionerElement?.Elements(Defaults.DefaultNs + "id").FirstOrDefault().ToIdentifier(true);
            if (identifier != null)
            {
                // Check Practitioner List
                var cacheKey = $"{ResourceType.Practitioner}|{identifier.System}|{identifier.Value}";
                cache.TryGetValue(cacheKey, out var resource);
                if (resource != null)
                {
                    encounter.Participant.Add(new Encounter.ParticipantComponent
                    {
                        Individual = new ResourceReference($"urn:uuid:{resource.Id}")
                    });
                }
            }

            // Location - Healthcare Facility
            var locationElement =
                element.XPathSelectElements("/n1:ClinicalDocument/n1:componentOf/n1:encompassingEncounter/n1:location/n1:healthCareFacility")
                .FirstOrDefault();
            identifier =
                locationElement?.Elements(Defaults.DefaultNs + "id").FirstOrDefault().ToIdentifier(true);
            if (identifier != null)
            {
                // Check Location List
                var cacheKey = $"{ResourceType.Location}|{identifier.System}|{identifier.Value}";
                cache.TryGetValue(cacheKey, out var resource);
                if (resource != null)
                {
                    encounter.Location.Add(new Encounter.LocationComponent()
                    {
                        Location = new ResourceReference($"urn:uuid:{resource.Id}")
                    });
                }
            }

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{encounter.Id}",
                Resource = encounter
            });

            return encounter;
        }
    }
}
