using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Exceptions;
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
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var id = Guid.NewGuid();
            var provenance = new Provenance
            {
                Id = id.ToString(),
                Meta = new Meta(),
                Agent = new List<Provenance.AgentComponent>()
            };

            try
            {
                // Date Recorded
                var dateRecordedValue = element
                    .Element(Defaults.DefaultNs + "time")?
                    .Attribute("value")?
                    .Value;

                if (string.IsNullOrWhiteSpace(dateRecordedValue))
                    throw new RequiredValueNotFoundException(element, "time[@value]", "Provenance.recorded");

                provenance.Recorded = dateRecordedValue.ParseCCdaDateTimeOffset();
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            // Agent
            var agent = new Provenance.AgentComponent
            {
                Type = new CodeableConcept
                {
                    Coding = new List<Coding>
                    {
                        new Coding
                        {
                            System =
                                "http://terminology.hl7.org/CodeSystem/provenance-participant-type",
                            Code = "author"
                        }
                    }
                }
            };

            try
            {
                var assignedAuthorElement = element.Element(Defaults.DefaultNs + "assignedAuthor");
                if (assignedAuthorElement == null)
                    throw new RequiredValueNotFoundException(element, "assignedAuthor", "Provenance.agent.who");

                // Meta
                provenance.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-provenance"));
                var cachedResource = assignedAuthorElement.SetIdentifiers(context, provenance);
                if (cachedResource != null)
                    return cachedResource;

                try
                {
                    var practitionerConverter = new PractitionerConverter(PatientId);
                    var practitioners = practitionerConverter.AddToBundle(new List<XElement> { assignedAuthorElement }, context);
                    agent.Who = new ResourceReference($"urn:uuid:{practitioners[0].Id}");
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }

                try
                {
                    var representedOrganizationElement = assignedAuthorElement.Element(Defaults.DefaultNs + "representedOrganization");
                    var representedOrganizationConverter = new OrganizationConverter();
                    var representedOrganization = representedOrganizationConverter.AddToBundle(representedOrganizationElement, context);

                    if (representedOrganization != null)
                        agent.OnBehalfOf = new ResourceReference($"urn:uuid:{representedOrganization.Id}");
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            provenance.Agent.Add(agent);

            context.Bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = provenance
            });

            return provenance;
        }
    }
}
