using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
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

        /// <summary>
        /// Gets the id of the FHIR organization resource that was generated
        /// </summary>
        public string ResultId { get; private set; }

        /// <inheritdoc />
        public void AddToBundle(
            Bundle bundle,
            IEnumerable<XElement> elements,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            var resultElement = elements.FirstOrDefault();
            if (resultElement == null)
                return;

            var id = Guid.NewGuid().ToString();
            var result = new Observation()
            {
                Id = id
            };

            // Identifiers
            var identifierElements = resultElement.Elements(Defaults.DefaultNs + "id");
            foreach (var identifierElement in identifierElements)
            {
                result.Identifier.Add(identifierElement.ToIdentifier());
            }

            // Category
            var codeConcept = new CodeableConcept
            {
                Coding = new List<Coding>()
                {
                    new Coding()
                    {
                        System = "http://terminology.hl7.org/CodeSystem/observation-category",
                        Code = "laboratory"
                    }
                }
            };

            result.Category = new List<CodeableConcept>()
            {
                codeConcept
            };

            // Status
            var statusCodeXPath = "n1:statusCode";
            var statusCode = resultElement
                .XPathSelectElement(statusCodeXPath, namespaceManager)?
                .Attribute("code")?
                .Value;

            if (!string.IsNullOrWhiteSpace(statusCode) && statusCode.ToLowerInvariant().Equals("completed"))
            {
                result.Status = ObservationStatus.Final;
            }
            else
            {
                result.Status = ObservationStatus.Unknown;
            }

            // Code
            var codeXPath = "n1:code/n1:translation";
            var codeElement = resultElement.XPathSelectElement(codeXPath, namespaceManager);
            if (codeElement == null)
            {
                codeXPath = "n1:code";
                codeElement = resultElement.XPathSelectElement(codeXPath, namespaceManager);

                if (codeElement == null)
                    throw new InvalidOperationException($"Could not find code in: {resultElement}");
            }

            result.Code = codeElement.ToCodeableConcept();

            // Effective Date
            var effectiveDateXPath = "n1:effectiveTime";
            var elementEffectiveDate = resultElement.XPathSelectElement(effectiveDateXPath, namespaceManager);

            result.Effective = elementEffectiveDate?.ToDateTimeElement();
            if (result.Effective == null)
                throw new InvalidOperationException($"Could not determine the result's effective date in: {resultElement}");

            // Value
            var valueXPath = "n1:value";
            var valueElement = resultElement.XPathSelectElement(valueXPath, namespaceManager);

            if (valueElement == null)
                throw new InvalidOperationException($"Could not determine the result's result in: {resultElement}");

            var valueType = valueElement.Attribute(Defaults.XsiNs + "type")?.Value;
            if (string.IsNullOrWhiteSpace(valueType))
                throw new InvalidOperationException($"Could not determine the result's type in: {valueElement}");

            if (valueType.ToLowerInvariant().Equals("pq"))
            {
                var value = valueElement.Attribute("value")?.Value;
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException($"No result value found in: {valueElement}");

                if (decimal.TryParse(value, out var quantityValue))
                {
                    var unit = valueElement.Attribute("unit")?.Value;

                    var resultValue = new Quantity();
                    resultValue.Value = quantityValue;

                    if (!string.IsNullOrWhiteSpace(unit))
                    {
                        resultValue.Unit = unit;
                    }

                    result.Value = resultValue;
                }

                Observation.ReferenceRangeComponent referenceRangeComponent = resultElement.ToObservationReferenceRange(namespaceManager);
                if (referenceRangeComponent != null)
                    result.ReferenceRange.Add(referenceRangeComponent);
            }
            else if (valueType.ToLowerInvariant().Equals("st"))
            {
                if (string.IsNullOrWhiteSpace(valueElement.Value))
                    throw new InvalidOperationException($"No result value found in: {valueElement}");

                result.Value = new CodeableConcept { Text = valueElement.Value };
            }

            result.Subject = new ResourceReference($"urn:uuid:{_patientId}");

            // Commit Resource
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = result
            });

            ResultId = id;
        }
    }
}
