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
    /// <summary>
    /// A model used with CCDA elements that require provenance to verify who and when the clinical data was recorded.
    /// </summary>
    public class ProvenanceConverter : IResourceConverter
    {
        private readonly ResourceType _targetResourceType;
        private readonly string _targetId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProvenanceConverter"/> class
        /// </summary>
        /// <param name="targetResourceType">The target resource, in which the provenance is associated with</param>
        /// <param name="targetId">The target id, in which the provenance is associated with</param>
        public ProvenanceConverter(ResourceType targetResourceType, string targetId)
        {
            _targetResourceType = targetResourceType;
            _targetId = targetId;
        }

        /// <inheritdoc />
        public void AddToBundle(
            Bundle bundle,
            IEnumerable<XElement> elements,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            var authorElement = elements.Elements(Defaults.DefaultNs + "author").FirstOrDefault();
            if (authorElement == null)
                return;

            var id = Guid.NewGuid();
            var provenance = new Provenance
            {
                Id = id.ToString(),
                Meta = new Meta(),
                Agent = new List<Provenance.AgentComponent>()
            };

            // Meta
            provenance.Meta.ProfileElement.Add(
                new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-provenance"));

            // Target
            provenance.Target.Add(new ResourceReference(_targetResourceType + "/" + _targetId));

            // Date Recorded
            var dateRecordedValue = authorElement
                .Element(Defaults.DefaultNs + "time")?
                .Attribute("value")?
                .Value;

            if (string.IsNullOrWhiteSpace(dateRecordedValue))
                throw new InvalidOperationException($"Could not find an authored time in: {authorElement}");

            provenance.Recorded = dateRecordedValue.ParseCCdaDateTimeOffset();

            // Agent
            var assignedAuthorElement = authorElement.Element(Defaults.DefaultNs + "assignedAuthor");
            if (assignedAuthorElement == null)
                throw new InvalidOperationException($"Could not find an assigned author in: {authorElement}");

            var practitionerConverter = new PractitionerConverter();
            practitionerConverter.AddToBundle(
                bundle,
                new List<XElement> { assignedAuthorElement },
                namespaceManager,
                cacheManager);

            var representedOrganizationElement =
                assignedAuthorElement.Element(Defaults.DefaultNs + "representedOrganization");
            var representedOrganizationConverter = new RepresentedOrganizationConverter();
            representedOrganizationConverter.AddToBundle(
                bundle,
                new List<XElement> { representedOrganizationElement },
                namespaceManager,
                cacheManager);

            var agent = new Provenance.AgentComponent
            {
                Type = new CodeableConcept()
                {
                    Coding = new List<Coding>()
                    {
                        new Coding()
                        {
                            System =
                                "http://terminology.hl7.org/CodeSystem/provenance-participant-type",
                            Code = "author"
                        }
                    }
                },
                Who = new ResourceReference($"urn:uuid:{practitionerConverter.PractitionerId}"),
                OnBehalfOf = new ResourceReference($"urn:uuid:{representedOrganizationConverter.OrganizationId}")
            };

            provenance.Agent.Add(agent);

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = provenance
            });
        }
    }
}
