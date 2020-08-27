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
    public class SmokingStatusObservationConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmokingStatusObservationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public SmokingStatusObservationConverter(string patientId)
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
                var smokingStatus = new Observation()
                {
                    Id = id,
                    Meta = new Meta(),
                    Subject = new ResourceReference($"urn:uuid:{_patientId}")
                };

                // Meta
                smokingStatus.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-smokingstatus"));

                // Identifiers
                var identifierElements = element.Elements(Defaults.DefaultNs + "id");
                foreach (var identifierElement in identifierElements)
                {
                    smokingStatus.Identifier.Add(identifierElement.ToIdentifier());
                }

                // Status
                var statusCodeXPath = "n1:statusCode";
                var statusCode = element
                    .XPathSelectElement(statusCodeXPath, namespaceManager)?
                    .Attribute("code")?
                    .Value;

                if (string.Compare(statusCode, "completed", StringComparison.InvariantCultureIgnoreCase) != 0)
                    throw new InvalidOperationException($"Could not determine the smoking status: {element}");

                smokingStatus.Status = ObservationStatus.Final;

                // Category
                smokingStatus.Category = new List<CodeableConcept>()
                {
                    new CodeableConcept()
                    {
                        Coding = new List<Coding>()
                        {
                            new Coding()
                            {
                                System = "http://terminology.hl7.org/CodeSystem/observation-category",
                                Code = "social-history",
                                Display = "Social History"
                            }
                        }
                    }
                };

                // Code
                var codeXPath = "n1:code/n1:translation";
                var codeElement = element.XPathSelectElement(codeXPath, namespaceManager);
                if (codeElement == null)
                {
                    codeXPath = "n1:code";
                    codeElement = element.XPathSelectElement(codeXPath, namespaceManager);

                    if (codeElement == null)
                        throw new InvalidOperationException($"Could not determine the smoking status code: {element}");
                }

                smokingStatus.Code = codeElement.ToCodeableConcept();

                // Issued
                var issuedDateXPath = "n1:effectiveTime";
                var issuedDate = element.XPathSelectElement(issuedDateXPath, namespaceManager);

                if (issuedDate?.ToFhirDateTime() == null)
                    throw new InvalidOperationException($"Could not determine the smoking status documented date: {element}");

                smokingStatus.Issued = issuedDate.ToFhirDateTime().ToDateTimeOffset(TimeSpan.Zero);

                // Effective Date
                smokingStatus.Effective = issuedDate.ToDateTimeElement();

                // Value
                var valueXPath = "n1:value";
                var valueElement = element.XPathSelectElement(valueXPath, namespaceManager);

                if (valueElement == null)
                    throw new InvalidOperationException($"Could not determine the smoking status documented value: {element}");

                smokingStatus.Value = valueElement.ToCodeableConcept();

                // Commit Resource
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = smokingStatus
                });
            }
        }
    }
}
