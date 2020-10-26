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
    /// Converter that converts various elements in the CCDA into practitioner role FHIR resources
    /// </summary>
    public class PractitionerRoleConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PractitionerRoleConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public PractitionerRoleConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "n1:ClinicalDocument/n1:author/n1:assignedAuthor";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var id = Guid.NewGuid().ToString();
            var role = new PractitionerRole
            {
                Id = id,
                Meta = new Meta()
            };

            role.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-practitionerrole"));

            try
            {
                var practitionerConverter = new PractitionerConverter(PatientId);
                var practitioner = practitionerConverter
                    .AddToBundle(new List<XElement> { element }, context)
                    .First();

                role.Practitioner = new ResourceReference($"urn:uuid:{practitioner.Id}");
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            try
            {
                var representedOrgEl = element.Element(Defaults.DefaultNs + "representedOrganization");
                if (representedOrgEl == null)
                {
                    throw new RequiredValueNotFoundException(
                        element,
                        "representedOrganization",
                        "PractitionerRole.organization");
                }

                var organizationConverter = new OrganizationConverter();
                var organization = organizationConverter.AddToBundle(representedOrgEl, context);
                role.Organization = new ResourceReference($"urn:uuid:{organization.Id}");
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            try
            {
                var telecom = element
                    .Element(Defaults.DefaultNs + "telecom")?
                    .ToContactPoint("PractitionerRole.telecom");

                if (telecom == null)
                {
                    throw new RequiredValueNotFoundException(
                        element,
                        "telecom",
                        "PractitionerRole.telecom");
                }

                role.Telecom.Add(telecom);
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            context.Bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = role
            });

            return role;
        }
    }
}
