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
                var codeXPath = "n1:code/n1:translation";
                var codeElement = element.XPathSelectElement(codeXPath, namespaceManager);
                if (codeElement == null)
                {
                    codeXPath = "n1:code";
                    codeElement = element.XPathSelectElement(codeXPath, namespaceManager);

                    if (codeElement == null)
                        throw new InvalidOperationException($"Could not find code in: {element}");
                }

                labOrder.Code = codeElement.ToCodeableConcept();
                labOrder.Subject = new ResourceReference($"urn:uuid:{_patientId}");

                // Observation FHIR (Results)
                var resultsXPath = "n1:component/n1:observation/n1:templateId[@root='2.16.840.1.113883.10.20.22.4.2']/..";
                var resultsElements = element.XPathSelectElements(resultsXPath, namespaceManager);

                foreach (var resultElement in resultsElements)
                {
                    string resultReference = GenerateResultObservationResourceReference(bundle, resultElement, namespaceManager);
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

        /// <summary>
        /// This will return the resource reference for Observation.
        /// </summary>
        /// <param name="bundle">The bundle to add the converted resources to as entries</param>
        /// <param name="resultElement">The result element</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the list of elements</param>
        /// <returns>The FHIR <see cref="Observation"/> representation of the source element</returns>
        private string GenerateResultObservationResourceReference(
            Bundle bundle,
            XElement resultElement,
            XmlNamespaceManager namespaceManager)
        {
            var id = Guid.NewGuid().ToString();
            var result = new Observation()
            {
                Id = id,
                Meta = new Meta()
            };

            // Meta PENDING
            // result.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/StructureDefinition/"));

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
                        Code = "results"
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

                Observation.ReferenceRangeComponent referenceRangeComponent = GetReferenceRangeComponent(resultElement, namespaceManager);
                if (referenceRangeComponent != null)
                    result.ReferenceRange.Add(referenceRangeComponent);
            }
            else if (valueType.ToLowerInvariant().Equals("st"))
            {
                if (string.IsNullOrWhiteSpace(valueElement.Value))
                    throw new InvalidOperationException($"No result value found in: {valueElement}");

                CodeableConcept valueCodeableConcept = new CodeableConcept { Text = valueElement.Value };
                result.Value = valueCodeableConcept;
            }

            result.Subject = new ResourceReference($"urn:uuid:{_patientId}");

            // Commit Resource
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = result
            });

            return id;
        }

        /// <summary>
        /// This will return the Reference Range for a result.
        /// </summary>
        /// <param name="resultElement">The result element</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the list of elements</param>
        /// <returns>The FHIR <see cref="Observation.ReferenceRangeComponent"/> representation of the source element</returns>
        private Observation.ReferenceRangeComponent GetReferenceRangeComponent(
            XElement resultElement,
            XmlNamespaceManager namespaceManager)
        {
            var referenceRangeLowXPath = "n1:referenceRange/n1:observationRange/n1:value/n1:low";
            var referenceRangeLowElement = resultElement.XPathSelectElement(referenceRangeLowXPath, namespaceManager);
            Observation.ReferenceRangeComponent referenceRangeComponent = null;

            if (referenceRangeLowElement != null)
            {
                referenceRangeComponent = new Observation.ReferenceRangeComponent();
                referenceRangeComponent.Low = ToSimpleQuantity(referenceRangeLowElement);
            }

            var referenceRangeHighXPath = "n1:referenceRange/n1:observationRange/n1:value/n1:high";
            var referenceRangeHighElement = resultElement.XPathSelectElement(referenceRangeHighXPath, namespaceManager);

            if (referenceRangeHighElement != null)
            {
                if (referenceRangeComponent == null)
                    referenceRangeComponent = new Observation.ReferenceRangeComponent();

                referenceRangeComponent.High = ToSimpleQuantity(referenceRangeHighElement);
            }

            return referenceRangeComponent;
        }

        /// <summary>
        /// This will return the Reference Range Element for a result.
        /// </summary>
        /// <param name="referenceRangeElement">The reference range element</param>
        /// <returns>The FHIR <see cref="SimpleQuantity"/> representation of the source element</returns>
        private SimpleQuantity ToSimpleQuantity(XElement referenceRangeElement)
        {
            var refRangeValue = referenceRangeElement.Attribute("value")?.Value;

            if (!decimal.TryParse(refRangeValue, out var quantityValue))
                throw new InvalidOperationException($"The reference range value is not numeric in: {referenceRangeElement}");

            return new SimpleQuantity { Value = quantityValue, Unit = referenceRangeElement.Attribute("unit")?.Value };
        }
    }
}
