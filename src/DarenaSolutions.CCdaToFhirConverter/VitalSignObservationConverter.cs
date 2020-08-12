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
    public class VitalSignObservationConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="VitalSignObservationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public VitalSignObservationConverter(string patientId)
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
                var vitalSign = new Observation()
                {
                    Id = id,
                    Meta = new Meta()
                };

                // Meta
                vitalSign.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/StructureDefinition/vitalsigns"));

                // Identifiers
                var identifierElements = element.Elements(Defaults.DefaultNs + "id");
                foreach (var identifierElement in identifierElements)
                {
                    vitalSign.Identifier.Add(identifierElement.ToIdentifier());
                }

                // Category
                var codeConcept = new CodeableConcept
                {
                    Coding = new List<Coding>()
                        {
                            new Coding()
                            {
                                System = "http://terminology.hl7.org/CodeSystem/observation-category",
                                Code = "vital-signs"
                            }
                        }
                };

                vitalSign.Category = new List<CodeableConcept>()
                    {
                        codeConcept
                    };

                // Status
                var statusCodeXPath = "n1:statusCode";
                var statusCode = element
                    .XPathSelectElement(statusCodeXPath, namespaceManager)?
                    .Attribute("code")?
                    .Value;

                switch (statusCode)
                {
                    case "registered":
                    case "received":
                        vitalSign.Status = ObservationStatus.Registered;
                        break;
                    case "preliminary":
                    case "draft":
                        vitalSign.Status = ObservationStatus.Preliminary;
                        break;
                    case "final":
                    case "completed":
                        vitalSign.Status = ObservationStatus.Final;
                        break;
                    case "amended":
                        vitalSign.Status = ObservationStatus.Amended;
                        break;
                    case "corrected":
                        vitalSign.Status = ObservationStatus.Corrected;
                        break;
                    case "cancelled":
                    case "abandoned":
                        vitalSign.Status = ObservationStatus.Cancelled;
                        break;
                    case "entered-in-error":
                    case "error":
                        vitalSign.Status = ObservationStatus.EnteredInError;
                        break;
                    default:
                        vitalSign.Status = ObservationStatus.Unknown;
                        break;
                }

                // Code
                var codeXPath = "n1:code/n1:translation";
                var codeElement = element.XPathSelectElement(codeXPath, namespaceManager);
                if (codeElement == null)
                {
                    codeXPath = "n1:code";
                    codeElement = element.XPathSelectElement(codeXPath, namespaceManager);

                    if (codeElement == null)
                        throw new InvalidOperationException($"Could not determine which vital sign was recorded in: {element}");
                }

                vitalSign.Code = codeElement.ToCodeableConcept();

                // Effective Date
                var effectiveDateXPath = "n1:effectiveTime";
                var elementEffectiveDate = element.XPathSelectElement(effectiveDateXPath, namespaceManager);

                vitalSign.Effective = elementEffectiveDate?.ToDateTimeElement();
                if (vitalSign.Effective == null)
                    throw new InvalidOperationException($"Could not determine the vital sign's effective date in: {element}");

                // Value Quantity
                var valueQuantityXPath = "n1:value";
                var valueQuantityElement = element.XPathSelectElement(valueQuantityXPath, namespaceManager);

                if (valueQuantityElement == null)
                    throw new InvalidOperationException($"Could not determine the vital sign's result in: {element}");

                var value = valueQuantityElement.Attribute("value")?.Value;
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException($"No vital sign value found in: {valueQuantityElement}");

                if (!decimal.TryParse(value, out var quantityValue))
                    throw new InvalidOperationException($"The vital sign result is not numeric in: {element}");

                var unit = valueQuantityElement.Attribute("unit")?.Value;
                if (string.IsNullOrWhiteSpace(unit))
                    throw new InvalidOperationException($"The vital sign unit of measure is not documented in: {element}");

                var vitalSignQuantity = new Quantity()
                {
                    Value = quantityValue,
                    Unit = unit
                };

                vitalSign.Value = vitalSignQuantity;

                // Subject
                vitalSign.Subject = new ResourceReference($"urn:uuid:{_patientId}");

                // Commit Resource
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = vitalSign
                });
            }
        }
    }
}
