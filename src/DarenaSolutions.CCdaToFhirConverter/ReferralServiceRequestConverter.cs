using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <inheritdoc />
    public class ReferralServiceRequestConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferralServiceRequestConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public ReferralServiceRequestConverter(string patientId)
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
                var referral = new ServiceRequest()
                {
                    Id = id,
                };

                // Identifiers
                var identifierElements = element.Elements(Defaults.DefaultNs + "id");
                foreach (var identifierElement in identifierElements)
                {
                    referral.Identifier.Add(identifierElement.ToIdentifier());
                }

                referral.Status = RequestStatus.Active;
                referral.Intent = RequestIntent.Plan;

                // text
                var textXPath = "n1:text";
                var textElement = element.XPathSelectElement(textXPath, namespaceManager);
                if (textElement == null)
                    throw new InvalidOperationException($"Could not find text in: {element}");

                var textElementText = textElement.Value;
                if (string.IsNullOrWhiteSpace(textElementText))
                    throw new InvalidOperationException($"No value for textElement was found in: {textElement}");

                CodeableConcept textCodeableConcept = new CodeableConcept
                {
                    Text = textElementText
                };
                referral.Code = textCodeableConcept;

                // Subject
                referral.Subject = new ResourceReference($"urn:uuid:{_patientId}");

                // Commit Resource
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = referral
                });
            }
        }
    }
}
