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
    /// Converter that converts various elements in the CCDA into location FHIR resources
    /// </summary>
    public class LocationConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public LocationConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            throw new InvalidOperationException(
                "This converter is not intended to be used as a standalone converter. Location elements must " +
                "be determined before using this converter. This converter itself cannot determine location resources");
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            Dictionary<string, Resource> cache)
        {
            var id = Guid.NewGuid().ToString();
            var location = new Location()
            {
                Id = id,
                Meta = new Meta()
            };

            location.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-location"));

            var identifierElements = element.Elements(Defaults.DefaultNs + "id");
            foreach (var identifierElement in identifierElements)
            {
                var identifier = identifierElement.ToIdentifier(true, "Location.identifier");
                var cacheKey = $"{ResourceType.Location}|{identifier.System}|{identifier.Value}";
                if (cache.TryGetValue(cacheKey, out var resource))
                    return resource;

                location.Identifier.Add(identifier);
                cache.Add(cacheKey, location);
            }

            // Type - Code
            var locationCode = element.ToCodeableConcept("Location.type");
            location.Type.Add(locationCode);

            // Name
            var nameElement = element
                .Element(Defaults.DefaultNs + "location")?
                .Elements(Defaults.DefaultNs + "name")
                .FirstOrDefault();

            if (nameElement == null)
                throw new RequiredValueNotFoundException(element, "location/name", "Location.name");

            location.Name = nameElement.Value;

            // Address
            var addrXPath = "n1:location/n1:addr";
            var addressElement = element.XPathSelectElements(addrXPath, namespaceManager).FirstOrDefault();
            if (addressElement != null)
                location.Address = addressElement.ToAddress("Location.address");

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = location
            });

            return location;
        }
    }
}
