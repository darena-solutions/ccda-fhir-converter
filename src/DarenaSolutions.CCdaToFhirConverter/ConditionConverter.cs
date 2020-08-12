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
    public class ConditionConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConditionConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public ConditionConverter(string patientId)
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
                var condition = new Condition
                {
                    Id = id,
                    Meta = new Meta(),
                    Subject = new ResourceReference($"urn:uuid:{_patientId}")
                };

                condition.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-condition"));

                var identifierElements = element.Elements(Defaults.DefaultNs + "id");
                foreach (var identifierElement in identifierElements)
                {
                    condition.Identifier.Add(identifierElement.ToIdentifier());
                }

                var codeElement = element
                    .Element(Defaults.DefaultNs + "value")?
                    .Element(Defaults.DefaultNs + "translation");

                if (codeElement == null)
                {
                    codeElement = element.Element(Defaults.DefaultNs + "value");
                    if (codeElement == null)
                        throw new InvalidOperationException($"A condition code was not found in: {element}");
                }

                condition.Code = codeElement.ToCodeableConcept();

                var categoryElement = element
                    .Element(Defaults.DefaultNs + "code")?
                    .Element(Defaults.DefaultNs + "translation");

                if (categoryElement == null)
                {
                    categoryElement = element.Element(Defaults.DefaultNs + "code");
                    if (categoryElement == null)
                        throw new InvalidOperationException($"A condition category was not found in: {element}");
                }

                condition.Category.Add(categoryElement.ToCodeableConcept());

                var effectiveTimeElement = element.Element(Defaults.DefaultNs + "effectiveTime");
                condition.Onset = effectiveTimeElement?.ToDateTimeElement();

                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = condition
                });
            }
        }
    }
}
