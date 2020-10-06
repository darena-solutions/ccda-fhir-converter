using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <inheritdoc />
    public class LabOrderServiceRequestConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="LabOrderServiceRequestConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public LabOrderServiceRequestConverter(string patientId)
        {
            _patientId = patientId;
        }

        /// <inheritdoc />
        public Resource Resource { get; private set; }

        /// <inheritdoc />
        public virtual void AddToBundle(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            var id = Guid.NewGuid().ToString();
            var labOrder = new ServiceRequest()
            {
                Id = id,
            };

            // Identifiers
            var identifierElements = element.Elements(Defaults.DefaultNs + "id");
            foreach (var identifierElement in identifierElements)
            {
                var identifier = identifierElement.ToIdentifier();
                if (cacheManager.TryGetResource(ResourceType.ServiceRequest, identifier.System, identifier.Value, out var resource))
                {
                    Resource = resource;
                    return;
                }

                labOrder.Identifier.Add(identifier);
                cacheManager.Add(labOrder, identifier.System, identifier.Value);
            }

            // Status
            var statusCodeXPath = "n1:statusCode";
            var statusCodeValue = element
                .XPathSelectElement(statusCodeXPath, namespaceManager)?
                .Attribute("code")?
                .Value;

            if (!string.IsNullOrWhiteSpace(statusCodeValue) && statusCodeValue.ToLowerInvariant().Equals("active"))
            {
                labOrder.Status = RequestStatus.Active;
            }
            else
            {
                labOrder.Status = RequestStatus.Unknown;
            }

            // Code
            var codeableConcept = element
                .FindCodeElementWithTranslation()?
                .ToCodeableConcept();

            if (codeableConcept == null)
                throw new InvalidOperationException($"Could not find code in: {element}");

            labOrder.Code = codeableConcept;
            labOrder.Intent = RequestIntent.Order;

            // Effective Date
            var effectiveDateXPath = "n1:effectiveTime";
            var elementEffectiveDate = element.XPathSelectElement(effectiveDateXPath, namespaceManager);

            labOrder.Occurrence = elementEffectiveDate?.ToDateTimeElement();

            // Subject
            labOrder.Subject = new ResourceReference($"urn:uuid:{_patientId}");

            // Commit Resource
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = labOrder
            });

            Resource = labOrder;
        }
    }
}
