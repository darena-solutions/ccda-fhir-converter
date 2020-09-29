using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;

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
                    Meta = new Meta()
                };

                device.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-implantable-device"));

                var identifierElements = element.Elements(Defaults.DefaultNs + "id");
                foreach (var identifierElement in identifierElements)
                {
                    device.Identifier.Add(identifierElement.ToIdentifier());
                }

                var playingDeviceCodeableConcept = element
                    .FindCodeElementWithTranslation("n1:participant/n1:participantRole/n1:playingDevice", namespaceManager)?
                    .ToCodeableConcept();

                if (playingDeviceCodeableConcept == null)
                    throw new InvalidOperationException($"No playing device code element was found in: {element}");

                var playingDeviceCoding = playingDeviceCodeableConcept.Coding.First();
                var udiCarrierHumanReadableStringXPath = "n1:participant/n1:participantRole/n1:id";
                var udiCarrierHumanReadableStringElement = element.XPathSelectElement(udiCarrierHumanReadableStringXPath, namespaceManager);

                if (udiCarrierHumanReadableStringElement != null)
                {
                    if (string.IsNullOrWhiteSpace(playingDeviceCoding.Code))
                        throw new InvalidOperationException($"If a udi carrier is found, then a device identifier must exist: {element}");

                    var udiCarrierHumanReadableString = udiCarrierHumanReadableStringElement
                        .Attribute("extension")?
                        .Value;

                    Device.UdiCarrierComponent udiCarrierCcomponent = new Device.UdiCarrierComponent
                    {
                        DeviceIdentifier = playingDeviceCoding.Code,
                        CarrierHRF = !string.IsNullOrWhiteSpace(udiCarrierHumanReadableString) ? udiCarrierHumanReadableString : null
                    };
                    device.UdiCarrier.Add(udiCarrierCcomponent);
                }

                var statusCodeValue = element
                    .Element(Defaults.DefaultNs + "statusCode")?
                    .Attribute("code")?
                    .Value;

                if (!string.IsNullOrWhiteSpace(statusCodeValue) && statusCodeValue.ToLowerInvariant().Equals("completed"))
                {
                    device.Status = Device.FHIRDeviceStatus.Active;
                }
                else
                {
                    device.Status = Device.FHIRDeviceStatus.Unknown;
                }

                if (!string.IsNullOrWhiteSpace(playingDeviceCoding.Display))
                {
                    device.DeviceName.Add(new Device.DeviceNameComponent { Name = playingDeviceCoding.Display, Type = DeviceNameType.UserFriendlyName });
                }

                var codeableConcept = element
                    .FindCodeElementWithTranslation()?
                    .ToCodeableConcept();

                if (codeableConcept == null)
                    throw new InvalidOperationException($"No code element was found in: {element}");

                device.Type = codeableConcept;
                device.Type.Text = codeableConcept.Coding.First().Display;
                device.Patient = new ResourceReference($"urn:uuid:{_patientId}");
                device.DistinctIdentifier = playingDeviceCoding.Code;

                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = device
                });
            }
        }
    }
}
