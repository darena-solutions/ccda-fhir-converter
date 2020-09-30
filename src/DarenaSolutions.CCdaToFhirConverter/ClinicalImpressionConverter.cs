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
    public class ClinicalImpressionConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClinicalImpressionConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public ClinicalImpressionConverter(string patientId)
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
                var clinicalImpression = new ClinicalImpression
                {
                    Id = id,
                    Status = ClinicalImpression.ClinicalImpressionStatus.Completed,
                    Subject = new ResourceReference($"urn:uuid:{_patientId}")
                };

                var textEl = element.Element(Defaults.DefaultNs + "text")?.GetFirstTextNode();
                if (string.IsNullOrWhiteSpace(textEl))
                    throw new InvalidOperationException($"Could not find any text for the clinical impression in: {element}");

                clinicalImpression.Note.Add(new Annotation
                {
                    Text = new Markdown(textEl)
                });

                clinicalImpression.DateElement = element.Element(Defaults.DefaultNs + "effectiveTime")?.ToFhirDateTime();
                clinicalImpression.Code = element
                    .FindCodeElementWithTranslation()?
                    .ToCodeableConcept();

                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = clinicalImpression
                });
            }
        }
    }
}
