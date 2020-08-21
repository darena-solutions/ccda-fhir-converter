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

                var playingDeviceCodeXPath = "n1:participant/n1:participantRole/n1:playingDevice/n1:code";
                var playingDeviceCodeElement = element.XPathSelectElement(playingDeviceCodeXPath, namespaceManager);

                if (playingDeviceCodeElement != null)
                {
                    var translationElement = playingDeviceCodeElement.Element(Defaults.DefaultNs + "translation");
                    if (translationElement != null)
                    {
                        playingDeviceCodeElement = translationElement;
                    }
                }
                else
                {
                    throw new InvalidOperationException($"No playing device code element was found in: {element}");
                }

                var playingDeviceCode = playingDeviceCodeElement
                    .Attribute("code")?
                    .Value;

                var playingDeviceName = playingDeviceCodeElement
                    .Attribute("displayName")?
                    .Value;

                var udiCarrierHumanReadableStringXPath = "n1:participant/n1:participantRole/n1:id";
                var udiCarrierHumanReadableStringElement = element.XPathSelectElements(udiCarrierHumanReadableStringXPath, namespaceManager).FirstOrDefault();

                if (udiCarrierHumanReadableStringElement == null)
                {
                    throw new InvalidOperationException($"No participantRole id element was found in: {element}");
                }
                else
                {
                    var udiCarrierHumanReadableString = udiCarrierHumanReadableStringElement
                        .Attribute("extension")?
                        .Value;

                    Device.UdiCarrierComponent udiCarrierCcomponent = new Device.UdiCarrierComponent
                    {
                        DeviceIdentifier = playingDeviceCode,
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

                if (!string.IsNullOrWhiteSpace(playingDeviceName))
                {
                    device.DeviceName.Add(new Device.DeviceNameComponent { Name = playingDeviceName, Type = DeviceNameType.UserFriendlyName });
                }

                var codeElement = element.Element(Defaults.DefaultNs + "code");
                if (codeElement != null)
                {
                    var translationElement = codeElement.Element(Defaults.DefaultNs + "translation");
                    if (translationElement != null)
                    {
                        codeElement = translationElement;
                    }
                }
                else
                {
                    throw new InvalidOperationException($"No code element was found in: {element}");
                }

                device.Type = codeElement.ToCodeableConcept();
                device.Type.Text = codeElement.Attribute("displayName")?.Value;
                device.Patient = new ResourceReference($"urn:uuid:{_patientId}");
                device.DistinctIdentifier = playingDeviceCode;

                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = device
                });
            }
        }
    }
}
