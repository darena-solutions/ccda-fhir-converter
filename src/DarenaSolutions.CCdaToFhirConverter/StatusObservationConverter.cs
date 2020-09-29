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
    public class StatusObservationConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusObservationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public StatusObservationConverter(string patientId)
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
                var observation = new Observation
                {
                    Id = $"urn:uuid:{id}",
                    Status = ObservationStatus.Final,
                    Subject = new ResourceReference($"urn:uuid:{_patientId}")
                };

                var identifierElements = element.Elements(Defaults.DefaultNs + "id");
                foreach (var identifierElement in identifierElements)
                {
                    observation.Identifier.Add(identifierElement.ToIdentifier());
                }

                observation.Category.Add(new CodeableConcept(
                    "http://terminology.hl7.org/CodeSystem/observation-category",
                    "exam",
                    "Exam",
                    null));

                observation.Code = element
                    .FindCodeElementWithTranslation()?
                    .ToCodeableConcept();

                if (observation.Code == null)
                    throw new InvalidOperationException($"A code could not be found for the status observation: {element}");

                observation.Effective = element.Element(Defaults.DefaultNs + "effectiveTime")?.ToDateTimeElement();
                observation.Value = element.Element(Defaults.DefaultNs + "value")?.ToFhirElementBasedOnType();

                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = observation
                });
            }
        }
    }
}
