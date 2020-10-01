using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
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

                var alreadyAdded = false;
                var identifierElements = element.Elements(Defaults.DefaultNs + "id");
                foreach (var identifierElement in identifierElements)
                {
                    var identifier = identifierElement.ToIdentifier();
                    if (cacheManager.Contains(ResourceType.Condition, identifier.System, identifier.Value))
                    {
                        alreadyAdded = true;
                        break;
                    }

                    condition.Identifier.Add(identifier);
                    cacheManager.Add(condition, identifier.System, identifier.Value);
                }

                if (alreadyAdded)
                    continue;

                if (_category == ConditionCategory.HealthConcern)
                {
                    // Get the value from the observation element
                    var xPath = "../../n1:entry/n1:observation";
                    var observationEl = element.XPathSelectElement(xPath, namespaceManager);

                    if (observationEl == null)
                        throw new InvalidOperationException($"A condition code was not found for the health concern: {element}");

                    var valueEl = observationEl
                        .Element(Defaults.DefaultNs + "value")?
                        .ToFhirElementBasedOnType();

                    if (!(valueEl is CodeableConcept codeableConcept))
                    {
                        throw new InvalidOperationException(
                            $"Expected the condition code for the health concern to be a codeable concept, however " +
                            $"the value type is not recognized: {observationEl}");
                    }

                    condition.Code = codeableConcept;
                }
                else
                {
                    condition.Code = element
                        .FindCodeElementWithTranslation()?
                        .ToCodeableConcept();

                    if (condition.Code == null)
                        throw new InvalidOperationException($"A condition code was not found in: {element}");
                }

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
