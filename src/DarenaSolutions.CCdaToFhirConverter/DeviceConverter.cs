using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Converter that converts various elements in the CCDA into device FHIR resources
    /// </summary>
    public class DeviceConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public DeviceConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='46264-8']/../n1:entry/n1:procedure/n1:participant/n1:participantRole";
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
            var device = new Device
            {
                Id = id,
                Meta = new Meta(),
                Patient = new ResourceReference($"urn:uuid:{PatientId}")
            };

            device.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-implantable-device"));

            device.Type = element
                .Element(Defaults.DefaultNs + "playingDevice")?
                .Element(Defaults.DefaultNs + "code")?
                .ToCodeableConcept();

            Device.UdiCarrierComponent udiCarrierComponent = null;
            var typeCoding = device.Type?.Coding.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(typeCoding?.Code))
            {
                udiCarrierComponent = new Device.UdiCarrierComponent
                {
                    DeviceIdentifier = typeCoding.Code
                };
            }

            var barcodeEl = element.Element(Defaults.DefaultNs + "id");
            if (barcodeEl != null)
            {
                var barcodeValue = barcodeEl.Attribute("extension")?.Value;
                if (udiCarrierComponent != null && string.IsNullOrWhiteSpace(barcodeValue))
                    throw new RequiredValueNotFoundException(barcodeEl, "[@extension]");

                if (!string.IsNullOrWhiteSpace(barcodeValue))
                {
                    udiCarrierComponent ??= new Device.UdiCarrierComponent();
                    var cacheKey = $"{ResourceType.Device}|{barcodeValue}";
                    if (cache.TryGetValue(cacheKey, out var resource))
                        return resource;

                    udiCarrierComponent.CarrierHRF = barcodeValue;
                    cache.Add(cacheKey, device);
                }
            }

            var scopingEntityValue = element
                .Element(Defaults.DefaultNs + "scopingEntity")?
                .Element(Defaults.DefaultNs + "id")?
                .Attribute("root")?
                .Value;

            if (!string.IsNullOrWhiteSpace(scopingEntityValue))
            {
                udiCarrierComponent ??= new Device.UdiCarrierComponent();
                udiCarrierComponent.Issuer = scopingEntityValue;
            }

            if (udiCarrierComponent != null)
            {
                if (string.IsNullOrWhiteSpace(udiCarrierComponent.DeviceIdentifier))
                    throw new RequiredValueNotFoundException(element, "playingDevice/code[@code]");

                device.UdiCarrier.Add(udiCarrierComponent);
            }

            var deviceComponentsXPath = "../../n1:entryRelationship/n1:organizer/n1:component/n1:observation";
            var deviceComponents = element.XPathSelectElements(deviceComponentsXPath, namespaceManager);

            foreach (var component in deviceComponents)
            {
                var code = component
                    .Element(Defaults.DefaultNs + "code")?
                    .Attribute("code")?
                    .Value;

                var valueEl = component.Element(Defaults.DefaultNs + "value");
                if (valueEl == null)
                    continue;

                switch (code)
                {
                    case "C101669":
                        if (valueEl.ToFhirElementBasedOnType("ts") is FhirDateTime manufactureDateTime)
                            device.ManufactureDateElement = manufactureDateTime;

                        break;
                    case "C101670":
                        if (valueEl.ToFhirElementBasedOnType("ts") is FhirDateTime expirationDateTime)
                            device.ExpirationDateElement = expirationDateTime;

                        break;
                    case "C101671":
                        if (valueEl.ToFhirElementBasedOnType("st") is FhirString serialNumber)
                            device.SerialNumber = serialNumber.Value;

                        break;
                    case "C101672":
                        if (valueEl.ToFhirElementBasedOnType("st") is FhirString lotNumber)
                            device.LotNumber = lotNumber.Value;

                        break;
                    case "C113843":
                        if (valueEl.ToFhirElementBasedOnType("st") is FhirString distinctIdentifier)
                            device.DistinctIdentifier = distinctIdentifier.Value;

                        break;
                    default:
                        continue;
                }
            }

            if (udiCarrierComponent != null &&
                device.ManufactureDateElement == null &&
                device.ExpirationDateElement == null &&
                string.IsNullOrWhiteSpace(device.SerialNumber) &&
                string.IsNullOrWhiteSpace(device.LotNumber) &&
                string.IsNullOrWhiteSpace(device.DistinctIdentifier))
            {
                var xPathToRequired =
                    "../../entryRelationship/organizer/component[*]/observation/code[@code='C101669' or @code='C101670' or " +
                    "@code='C101671' or @code='C101672' or @code='C113843']";

                throw new RequiredValueNotFoundException(element, xPathToRequired);
            }

            return device;
        }
    }
}
