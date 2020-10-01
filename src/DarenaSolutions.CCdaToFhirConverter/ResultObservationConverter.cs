using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <inheritdoc />
    public class ResultObservationConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultObservationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public ResultObservationConverter(string patientId)
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
                var result = new Observation
                {
                    Id = id,
                    Meta = new Meta(),
                    Status = ObservationStatus.Final,
                    Subject = new ResourceReference($"urn:uuid:{_patientId}")
                };

                result.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-observation-lab"));

                // Identifiers
                var identifierElements = element.Elements(Defaults.DefaultNs + "id");
                foreach (var identifierElement in identifierElements)
                {
                    result.Identifier.Add(identifierElement.ToIdentifier());
                }

                // Category
                var codeConcept = new CodeableConcept
                {
                    Coding = new List<Coding>
                {
                    new Coding
                    {
                        System = "http://terminology.hl7.org/CodeSystem/observation-category",
                        Code = "laboratory"
                    }
                }
                };

                result.Category.Add(codeConcept);

                // Code
                result.Code = element
                    .FindCodeElementWithTranslation()?
                    .ToCodeableConcept();

                if (result.Code == null)
                    throw new InvalidOperationException($"Could not find code in: {element}");

                result.Effective = element
                    .Element(Defaults.DefaultNs + "effectiveTime")?
                    .ToDateTimeElement();

                result.Value = element
                    .Element(Defaults.DefaultNs + "value")?
                    .ToFhirElementBasedOnType();

                // Commit Resource
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = result
                });
            }
        }
    }
}
