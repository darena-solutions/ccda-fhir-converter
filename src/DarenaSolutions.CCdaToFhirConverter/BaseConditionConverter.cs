using System;
using System.Xml;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Base converter that contains the common mapping between all condition type resources
    /// </summary>
    public abstract class BaseConditionConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseConditionConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        protected BaseConditionConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            var id = Guid.NewGuid().ToString();
            var condition = new Condition
            {
                Id = id,
                Meta = new Meta(),
                Subject = new ResourceReference($"urn:uuid:{PatientId}"),
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
                var identifier = identifierElement.ToIdentifier();
                if (cacheManager.TryGetResource(ResourceType.Condition, identifier.System, identifier.Value, out var resource))
                    return resource;

                condition.Identifier.Add(identifier);
                cacheManager.Add(condition, identifier.System, identifier.Value);
            }

            condition.Onset = element
                .Element(Defaults.DefaultNs + "effectiveTime")?
                .ToDateTimeElement();

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = condition
            });

            return condition;
        }
    }
}
