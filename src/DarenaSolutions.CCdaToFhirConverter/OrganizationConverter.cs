using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts an element in the CCDA to an organization FHIR resource
    /// </summary>
    public class OrganizationConverter : BaseSingleResourceConverter
    {
        /// <inheritdoc />
        protected override XElement GetPrimaryElement(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "n1:ClinicalDocument/n1:author/n1:assignedAuthor/n1:representedOrganization";
            return cCda.XPathSelectElement(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override void PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            var id = Guid.NewGuid().ToString();
            var organization = new Organization
            {
                Id = id,
                Meta = new Meta(),
                Name = element.Element(Defaults.DefaultNs + "name")?.Value,
                Active = true
            };

            if (string.IsNullOrWhiteSpace(organization.Name))
                throw new InvalidOperationException($"No organization name was found in: {element}");

            organization.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-organization"));

            var identifierElements = element.Elements(Defaults.DefaultNs + "id");
            foreach (var identifierElement in identifierElements)
            {
                var identifier = identifierElement.ToIdentifier();
                if (cacheManager.TryGetResource(ResourceType.Organization, identifier.System, identifier.Value, out var resource))
                {
                    Resource = resource;
                    return;
                }

                organization.Identifier.Add(identifier);
                cacheManager.Add(organization, identifier.System, identifier.Value);
            }

            var telecoms = element.Elements(Defaults.DefaultNs + "telecom");
            foreach (var telecom in telecoms)
            {
                organization.Telecom.Add(telecom.ToContactPoint());
            }

            var addressElements = element.Elements(Defaults.DefaultNs + "addr");
            foreach (var addressElement in addressElements)
            {
                var address = addressElement.ToAddress();
                if (address.LineElement.Count > 4)
                    throw new InvalidOperationException($"More than 4 address lines were provided in: {addressElement}");

                organization.Address.Add(address);
            }

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = organization
            });

            Resource = organization;
        }
    }
}
