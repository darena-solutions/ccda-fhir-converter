using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <inheritdoc />
    public class DeviceConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public DeviceConverter(string patientId)
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
                var device = new Device
                {
                    Id = id,
                    Meta = new Meta(),
                    Patient = new ResourceReference($"urn:uuid:{_patientId}")
                };

                device.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-implantable-device"));

                device.Type = element
                    .Element(Defaults.DefaultNs + "playingDevice")?
                    .Element(Defaults.DefaultNs + "code")?
                    .ToCodeableConcept();

                if (device.Type == null)
                    throw new InvalidOperationException($"The device type could not be found in: {element}");

                Device.UdiCarrierComponent udiCarrierComponent = null;
                var barcodeEl = element.Element(Defaults.DefaultNs + "id");
                if (barcodeEl != null)
                {
                    var barcodeValue = barcodeEl.Attribute("extension")?.Value;
                    if (string.IsNullOrWhiteSpace(barcodeValue))
                        continue;

                    if (cacheManager.Contains(ResourceType.Device, null, barcodeValue))
                        continue;

                    udiCarrierComponent = new Device.UdiCarrierComponent
                    {
                        CarrierHRF = barcodeValue
                    };

                    cacheManager.Add(device, null, barcodeValue);
                }

                var typeCoding = device.Type.Coding.First();
                if (!string.IsNullOrWhiteSpace(typeCoding.Code))
                {
                    udiCarrierComponent ??= new Device.UdiCarrierComponent();
                    udiCarrierComponent.DeviceIdentifier = typeCoding.Code;
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
                        throw new InvalidOperationException($"The device identifier could not be found in: {element}");

                    if (string.IsNullOrWhiteSpace(udiCarrierComponent.CarrierHRF))
                        throw new InvalidOperationException($"A device barcode value could not be found in: {element}");

                    device.UdiCarrier.Add(udiCarrierComponent);
                }

                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = device
                });
            }
        }
    }
}
