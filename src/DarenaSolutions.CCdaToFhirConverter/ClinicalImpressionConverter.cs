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
    /// Converter that converts various elements in the CCDA to clinical impression FHIR resources
    /// </summary>
    public class ClinicalImpressionConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClinicalImpressionConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public ClinicalImpressionConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='11488-4']/../n1:entry/n1:act";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            Dictionary<string, Resource> cache)
        {
            var id = Guid.NewGuid().ToString();
            var clinicalImpression = new ClinicalImpression
            {
                Id = id,
                Status = ClinicalImpression.ClinicalImpressionStatus.Completed,
                Subject = new ResourceReference($"urn:uuid:{PatientId}")
            };

            var textEl = element.Element(Defaults.DefaultNs + "text")?.GetFirstTextNode();
            if (string.IsNullOrWhiteSpace(textEl))
                throw new RequiredValueNotFoundException(element, "text");

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

            return clinicalImpression;
        }
    }
}
