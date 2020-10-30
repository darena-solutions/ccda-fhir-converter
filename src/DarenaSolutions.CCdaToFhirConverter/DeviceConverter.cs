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
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var id = Guid.NewGuid().ToString();
            var device = new Device
            {
                Id = id,
                Meta = new Meta(),
                Patient = new ResourceReference($"urn:uuid:{PatientId}")
            };

            device.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-implantable-device"));
            var cachedResource = element.SetIdentifiers(context, device);
            if (cachedResource != null)
                return cachedResource;

            try
            {
                device.Type = element
                    .Element(Namespaces.DefaultNs + "playingDevice")?
                    .Element(Namespaces.DefaultNs + "code")?
                    .ToCodeableConcept("Device.type");
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            Device.UdiCarrierComponent udiCarrierComponent = null;
            var typeCoding = device.Type?.Coding.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(typeCoding?.Code))
            {
                udiCarrierComponent = new Device.UdiCarrierComponent
                {
                    DeviceIdentifier = typeCoding.Code
                };
            }

            var barcodeEl = element.Element(Namespaces.DefaultNs + "id");
            if (barcodeEl != null)
            {
                try
                {
                    var barcodeValue = barcodeEl.Attribute("extension")?.Value;
                    if (udiCarrierComponent != null && string.IsNullOrWhiteSpace(barcodeValue))
                    {
                        throw new RequiredValueNotFoundException(
                            barcodeEl,
                            "[@extension]",
                            "Device.udiCarrier.carrierHRF");
                    }

                    if (!string.IsNullOrWhiteSpace(barcodeValue))
                    {
                        udiCarrierComponent ??= new Device.UdiCarrierComponent();
                        var cacheKey = $"{ResourceType.Device}|{barcodeValue}";
                        if (context.Cache.TryGetValue(cacheKey, out var resource))
                            return resource;

                        udiCarrierComponent.CarrierHRF = barcodeValue;
                        context.Cache.Add(cacheKey, device);
                    }
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            var scopingEntityValue = element
                .Element(Namespaces.DefaultNs + "scopingEntity")?
                .Element(Namespaces.DefaultNs + "id")?
                .Attribute("root")?
                .Value;

            if (!string.IsNullOrWhiteSpace(scopingEntityValue))
            {
                udiCarrierComponent ??= new Device.UdiCarrierComponent();
                udiCarrierComponent.Issuer = scopingEntityValue;
            }

            try
            {
                if (udiCarrierComponent != null)
                {
                    if (string.IsNullOrWhiteSpace(udiCarrierComponent.DeviceIdentifier))
                    {
                        throw new RequiredValueNotFoundException(
                            element,
                            "playingDevice/code[@code]",
                            "Device.udiCarrier.deviceIdentifier");
                    }

                    device.UdiCarrier.Add(udiCarrierComponent);
                }
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            var deviceComponentsXPath = "../../n1:entryRelationship/n1:organizer/n1:component/n1:observation";
            var deviceComponents = element.XPathSelectElements(deviceComponentsXPath, context.NamespaceManager);

            foreach (var component in deviceComponents)
            {
                var code = component
                    .Element(Namespaces.DefaultNs + "code")?
                    .Attribute("code")?
                    .Value;

                var valueEl = component.Element(Namespaces.DefaultNs + "value");
                if (valueEl == null)
                    continue;

                try
                {
                    switch (code)
                    {
                        case "C101669":
                            if (valueEl.ToFhirElementBasedOnType(new[] { "ts" }, "Device.manufactureDate") is FhirDateTime manufactureDateTime)
                                device.ManufactureDateElement = manufactureDateTime;

                            break;
                        case "C101670":
                            if (valueEl.ToFhirElementBasedOnType(new[] { "ts" }, "Device.expirationDate") is FhirDateTime expirationDateTime)
                                device.ExpirationDateElement = expirationDateTime;

                            break;
                        case "C101671":
                            if (valueEl.ToFhirElementBasedOnType(new[] { "st" }, "Device.serialNumber") is FhirString serialNumber)
                                device.SerialNumber = serialNumber.Value;

                            break;
                        case "C101672":
                            if (valueEl.ToFhirElementBasedOnType(new[] { "st" }, "Device.lotNumber") is FhirString lotNumber)
                                device.LotNumber = lotNumber.Value;

                            break;
                        case "C113843":
                            if (valueEl.ToFhirElementBasedOnType(new[] { "st" }, "Device.distinctIdentifier") is FhirString distinctIdentifier)
                                device.DistinctIdentifier = distinctIdentifier.Value;

                            break;
                        default:
                            continue;
                    }
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
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
                    "entryRelationship/organizer/component[*]/observation/code[@code='C101669' or @code='C101670' or " +
                    "@code='C101671' or @code='C101672' or @code='C113843']";

                var fhirPropertyPaths =
                    "Device.manufactureDate, " +
                    "Device.expirationDate, " +
                    "Device.serialNumber, " +
                    "Device.lotNumber, " +
                    "Device.distinctIdentifier";

                try
                {
                    throw new RequiredValueNotFoundException(element.Parent.Parent, xPathToRequired, fhirPropertyPaths);
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            context.Bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = device
            });

            return device;
        }
    }
}
