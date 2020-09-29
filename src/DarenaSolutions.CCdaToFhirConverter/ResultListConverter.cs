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
    public class ResultListConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultListConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public ResultListConverter(string patientId)
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
                var labOrder = new List()
                {
                    Id = id,
                };

                // Identifiers
                var identifierElements = element.Elements(Defaults.DefaultNs + "id");
                foreach (var identifierElement in identifierElements)
                {
                    labOrder.Identifier.Add(identifierElement.ToIdentifier());
                }

                // Status
                var statusCodeXPath = "n1:statusCode";
                var statusCodeValue = element
                    .XPathSelectElement(statusCodeXPath, namespaceManager)?
                    .Attribute("code")?
                    .Value;

                if (!string.IsNullOrWhiteSpace(statusCodeValue) && statusCodeValue.ToLowerInvariant().Equals("completed"))
                {
                    labOrder.Status = List.ListStatus.Current;
                }
                else
                {
                    labOrder.Status = List.ListStatus.Retired;
                }

                labOrder.Mode = ListMode.Working;

                // Code
                var codeableConcept = element
                    .FindCodeElementWithTranslation()?
                    .ToCodeableConcept();

                if (codeableConcept == null)
                    throw new InvalidOperationException($"Could not find code in: {element}");

                labOrder.Code = codeableConcept;
                labOrder.Subject = new ResourceReference($"urn:uuid:{_patientId}");

                // Observation FHIR (Results)
                var resultsXPath = "n1:component/n1:observation/n1:templateId[@root='2.16.840.1.113883.10.20.22.4.2']/..";
                var resultsElements = element.XPathSelectElements(resultsXPath, namespaceManager);

                foreach (var resultElement in resultsElements)
                {
                    var resultObservationConverter = new ResultObservationConverter(_patientId);
                    resultObservationConverter.AddToBundle(
                        bundle,
                        new List<XElement> { resultElement },
                        namespaceManager,
                        cacheManager);

                    string resultReference = resultObservationConverter.ResultId;
                    if (string.IsNullOrWhiteSpace(resultReference))
                        throw new InvalidOperationException($"Could not find resultReference for ObservationResult");

                    List.EntryComponent resultEntryComponent = new List.EntryComponent { Item = new ResourceReference($"urn:uuid:{resultReference}") };
                    labOrder.Entry.Add(resultEntryComponent);
                }

                // Commit Resource
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = labOrder
                });
            }
        }
    }
}
