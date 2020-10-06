using System;
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
        public Resource Resource { get; private set; }

        /// <inheritdoc />
        public virtual void AddToBundle(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            var id = Guid.NewGuid().ToString();
            var device = new Device
            {
                Id = id,
                Meta = new Meta(),
                Patient = new ResourceReference($"urn:uuid:{_patientId}")
            };

            device.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-implantable-device"));

            Device.UdiCarrierComponent udiCarrierComponent = null;
            var identifierElement = element.Element(Defaults.DefaultNs + "id");
            if (identifierElement != null)
            {
                var idValue = identifierElement.Attribute("extension")?.Value;
                if (!string.IsNullOrWhiteSpace(idValue))
                {
                    if (cacheManager.Contains(ResourceType.Device, null, idValue))
                        return;

                    udiCarrierComponent = new Device.UdiCarrierComponent
                    {
                        DeviceIdentifier = idValue
                    };

                    cacheManager.Add(device, null, idValue);
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

                if (string.IsNullOrWhiteSpace(udiCarrierComponent.DeviceIdentifier))
                {
                    throw new InvalidOperationException(
                        $"A device scoping entity exists, however a device identifier could not be found in " +
                        $"{element}");
                }

                udiCarrierComponent.Issuer = scopingEntityValue;
            }

            if (udiCarrierComponent != null)
                device.UdiCarrier.Add(udiCarrierComponent);

            device.Type = element
                .Element(Defaults.DefaultNs + "playingDevice")?
                .Element(Defaults.DefaultNs + "code")?
                .ToCodeableConcept();

            if (device.Type == null)
                throw new InvalidOperationException($"The device type could not be found in: {element}");

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = device
            });

            Resource = device;
        }
    }
}
