using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// A base converter that contains the common mapping between all observation type resources
    /// </summary>
    public class BaseObservationConverter : IResourceConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseObservationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        protected BaseObservationConverter(string patientId)
        {
            PatientId = patientId;
        }

        /// <summary>
        /// Gets the id of the patient referenced in the CCDA
        /// </summary>
        protected string PatientId { get; }

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
                    Id = id,
                    Status = ObservationStatus.Final,
                    Subject = new ResourceReference($"urn:uuid:{PatientId}")
                };

                // Identifiers
                var identifierElements = element.Elements(Defaults.DefaultNs + "id");
                foreach (var identifierElement in identifierElements)
                {
                    observation.Identifier.Add(identifierElement.ToIdentifier());
                }

                // Category
                // The category code should be included by all derived instances of this base converter
                var codeConcept = new CodeableConcept
                {
                    Coding = new List<Coding>
                    {
                        new Coding
                        {
                            System = "http://terminology.hl7.org/CodeSystem/observation-category"
                        }
                    }
                };

                observation.Category.Add(codeConcept);

                // Code
                observation.Code = element
                    .FindCodeElementWithTranslation()?
                    .ToCodeableConcept();

                if (observation.Code == null)
                    throw new InvalidOperationException($"Could not find code in: {element}");

                observation.Effective = element
                    .Element(Defaults.DefaultNs + "effectiveTime")?
                    .ToDateTimeElement();

                observation.Value = element
                    .Element(Defaults.DefaultNs + "value")?
                    .ToFhirElementBasedOnType();

                // Commit Resource
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = observation
                });

                CustomizeMapping(element, observation);
            }
        }

        /// <summary>
        /// This method will be called for each observation resource that was read from the CCDA. When called, the base
        /// mapping will have been already applied to <paramref name="observation"/>. Each derived converter can customize
        /// this mapping if necessary. The base method does nothing
        /// </summary>
        /// <param name="element">The observation element read from the CCDA</param>
        /// <param name="observation">The observation FHIR resource generated from the CCDA element</param>
        protected virtual void CustomizeMapping(XElement element, Observation observation)
        {
            return;
        }
    }
}
