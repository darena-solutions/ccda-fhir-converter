using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Enums;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <inheritdoc />
    public class ConditionConverter : IResourceConverter
    {
        private readonly string _patientId;
        private readonly ConditionCategory _category;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConditionConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        /// <param name="category">The enumeration of the condition category</param>
        public ConditionConverter(string patientId, ConditionCategory category = ConditionCategory.Extensible)
        {
            _patientId = patientId;
            _category = category;
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
                var condition = new Condition
                {
                    Id = id,
                    Meta = new Meta(),
                    Subject = new ResourceReference($"urn:uuid:{_patientId}"),
                    VerificationStatus = new CodeableConcept(
                        "http://terminology.hl7.org/CodeSystem/condition-ver-status",
                        "confirmed",
                        "Confirmed",
                        null),
                    ClinicalStatus = new CodeableConcept(
                        "http://terminology.hl7.org/CodeSystem/condition-clinical",
                        "active",
                        "Active",
                        null)
                };

                condition.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-condition"));

                var identifierElements = element.Elements(Defaults.DefaultNs + "id");
                foreach (var identifierElement in identifierElements)
                {
                    condition.Identifier.Add(identifierElement.ToIdentifier());
                }

                var codeableConcept = element
                    .FindCodeElementWithTranslation()?
                    .ToCodeableConcept();

                if (codeableConcept == null)
                    throw new InvalidOperationException($"A condition code was not found in: {element}");

                condition.Code = codeableConcept;

                if (_category == ConditionCategory.Extensible)
                {
                    var categoryCodeableConcept = element
                        .FindCodeElementWithTranslation()?
                        .ToCodeableConcept();

                    if (categoryCodeableConcept == null)
                        throw new InvalidOperationException($"A condition category was not found in: {element}");

                    condition.Category.Add(categoryCodeableConcept);
                }
                else
                {
                    var categoryCoding = new Coding
                    {
                        System = "http://terminology.hl7.org/CodeSystem/condition-category"
                    };

                    switch (_category)
                    {
                        case ConditionCategory.ProblemList:
                            categoryCoding.Code = "problem-list-item";
                            categoryCoding.Display = "Problem List Item";
                            break;
                        case ConditionCategory.EncounterDiagnosis:
                            categoryCoding.Code = "encounter-diagnosis";
                            categoryCoding.Display = "Encounter Diagnosis";
                            break;
                        case ConditionCategory.HealthConcern:
                            categoryCoding.Code = "health-concern";
                            categoryCoding.Display = "Health Concern";
                            break;
                    }

                    condition.Category.Add(new CodeableConcept
                    {
                        Coding = new List<Coding>
                        {
                            categoryCoding
                        }
                    });
                }

                condition.Onset = element
                    .Element(Defaults.DefaultNs + "effectiveTime")?
                    .ToDateTimeElement();

                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = condition
                });
            }
        }
    }
}
