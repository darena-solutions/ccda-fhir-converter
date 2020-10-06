using System;
using System.Collections.Generic;
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
    public class ProvenanceConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProvenanceConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public ProvenanceConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            throw new InvalidOperationException(
                "This converter is not intended to be used as a standalone converter. Provenance elements must be " +
                "determined before using this converter. This converter itself cannot determine provenance resources");
        }

        /// <inheritdoc />
        protected override void PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
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

            // Date Recorded
            var dateRecordedValue = element
                .Element(Defaults.DefaultNs + "time")?
                .Attribute("value")?
                .Value;

            if (string.IsNullOrWhiteSpace(dateRecordedValue))
                throw new InvalidOperationException($"Could not find an authored time in: {element}");

            provenance.Recorded = dateRecordedValue.ParseCCdaDateTimeOffset();

            // Agent
            var assignedAuthorElement = element.Element(Defaults.DefaultNs + "assignedAuthor");
            if (assignedAuthorElement == null)
                throw new InvalidOperationException($"Could not find an assigned author in: {element}");

            var practitionerConverter = new PractitionerConverter(PatientId);
            practitionerConverter.AddToBundle(
                bundle,
                new List<XElement> { assignedAuthorElement },
                namespaceManager,
                cacheManager);

            var representedOrganizationElement =
                assignedAuthorElement.Element(Defaults.DefaultNs + "representedOrganization");
            var representedOrganizationConverter = new OrganizationConverter();
            representedOrganizationConverter.AddToBundle(
                bundle,
                representedOrganizationElement,
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
                Who = new ResourceReference($"urn:uuid:{practitionerConverter.Resources[0].Id}"),
                OnBehalfOf = new ResourceReference($"urn:uuid:{representedOrganizationConverter.Resource.Id}")
            };

            provenance.Agent.Add(agent);

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = provenance
            });

            Resources.Add(provenance);
        }
    }
}
