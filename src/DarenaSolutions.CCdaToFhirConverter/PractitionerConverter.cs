using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <inheritdoc />
    public class PractitionerConverter : IResourceConverter
    {
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
            var practitioner = new Practitioner
            {
                Id = id,
                Meta = new Meta()
            };

            practitioner.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-practitioner"));

            var identifierElements = element.Elements(Defaults.DefaultNs + "id");
            foreach (var identifierElement in identifierElements)
            {
                var identifier = identifierElement.ToIdentifier(true);
                if (cacheManager.TryGetResource(ResourceType.Practitioner, identifier.System, identifier.Value, out var resource))
                {
                    Resource = resource;
                    return;
                }

                practitioner.Identifier.Add(identifier);
                cacheManager.Add(practitioner, identifier.System, identifier.Value);
            }

            if (!practitioner.Identifier.Any())
                throw new InvalidOperationException($"No practitioner identifiers were found in: {element}");

            var nameElements = element
                .Element(Defaults.DefaultNs + "assignedPerson")?
                .Elements(Defaults.DefaultNs + "name")
                .ToList();

            if (nameElements == null || !nameElements.Any())
                throw new InvalidOperationException($"Could not find any practitioner names in: {element}");

            foreach (var nameElement in nameElements)
            {
                var humanName = nameElement.ToHumanName();
                if (string.IsNullOrWhiteSpace(humanName.Family))
                    throw new InvalidOperationException($"No practitioner family name was found in: {nameElement}");

                practitioner.Name.Add(humanName);
            }

            var addressElements = element.Elements(Defaults.DefaultNs + "addr");
            foreach (var addressElement in addressElements)
            {
                practitioner.Address.Add(addressElement.ToAddress());
            }

            var telecomElements = element.Elements(Defaults.DefaultNs + "telecom");
            foreach (var telecomElement in telecomElements)
            {
                practitioner.Telecom.Add(telecomElement.ToContactPoint());
            }

            var qualificationCodes = element.Elements(Defaults.DefaultNs + "code");
            foreach (var qualificationCode in qualificationCodes)
            {
                practitioner.Qualification.Add(new Practitioner.QualificationComponent
                {
                    Code = qualificationCode.ToCodeableConcept()
                });
            }

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = practitioner
            });

            Resource = practitioner;
        }
    }
}
