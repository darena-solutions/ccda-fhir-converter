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
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
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
                try
                {
                    encounter.Identifier.Add(identifierElement.ToIdentifier(true, "Encounter.identifier"));
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            try
            {
                // Class - Override when found
                var translationCode = element
                    .FindCodeElementWithTranslation(translationOnly: true)?
                    .ToCodeableConcept("Encounter.class");
                if (translationCode?.Coding?.Count > 0)
                    encounter.Class = new Coding(translationCode.Coding[0].System, translationCode.Coding[0].Code);
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            try
            {
                if (element.Attribute("code") == null)
                {
                    throw new RequiredValueNotFoundException(
                        element,
                        "[@code]",
                        "Encounter.type");
                }

                // Type - CPT Code
                var encounterCode = element.ToCodeableConcept("Encounter.type");
                encounter.Type.Add(encounterCode);
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            // Period
            var effectiveTimeElement =
                element.Element(Defaults.DefaultNs + "effectiveTime");
            encounter.Period = effectiveTimeElement?.ToPeriod();

            try
            {
                // Diagnoses
                var encounterDiagnosisElements =
                    element.XPathSelectElements("n1:entryRelationship/n1:act/n1:entryRelationship/n1:observation", context.NamespaceManager);
                var encounterDiagnosesConverter = new EncounterDiagnosesConditionConverter(PatientId);
                var encounterDiagnoses = encounterDiagnosesConverter.AddToBundle(encounterDiagnosisElements, context);

                if (encounterDiagnoses.Count > 0)
                {
                    foreach (var condition in encounterDiagnoses)
                    {
                        encounter.Diagnosis.Add(new Encounter.DiagnosisComponent
                        {
                            Condition = new ResourceReference($"urn:uuid:{condition.Id}")
                        });
                    }
                }
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            try
            {
                // Location
                var locationElements =
                    element.XPathSelectElements(
                        "/n1:ClinicalDocument/n1:documentationOf/n1:serviceEvent/n1:performer/n1:assignedEntity/n1:representedOrganization",
                        context.NamespaceManager);

                var locationConverter = new LocationConverter(PatientId);
                var locations = locationConverter.AddToBundle(locationElements, context);

                if (locations.Count > 0)
                {
                    encounter.Location.Add(new Encounter.LocationComponent()
                    {
                        Location = new ResourceReference($"urn:uuid:{locations[0].Id}")
                    });
                }
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            context.Bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{encounter.Id}",
                Resource = encounter
            });

            return encounter;
        }
    }
}
