using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts various elements in the CCDA into care team FHIR resources
    /// </summary>
    public class CareTeamConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CareTeamConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public CareTeamConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        public override List<Resource> AddToBundle(
            Bundle bundle,
            XDocument cCda,
            XmlNamespaceManager namespaceManager,
            Dictionary<string, Resource> cache)
        {
            var elements = GetPrimaryElements(cCda, namespaceManager);
            return AddToBundle(
                bundle,
                elements,
                namespaceManager,
                cache);
        }

        /// <inheritdoc />
        public override List<Resource> AddToBundle(
            Bundle bundle,
            IEnumerable<XElement> elements,
            XmlNamespaceManager namespaceManager,
            Dictionary<string, Resource> cache)
        {
            var id = Guid.NewGuid().ToString();
            var careTeam = new CareTeam
            {
                Id = id,
                Meta = new Meta(),
                Subject = new ResourceReference($"urn:uuid:{PatientId}")
            };

            careTeam.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-careteam"));

            foreach (var element in elements)
            {
                var practitioner = PerformElementConversion(
                    bundle,
                    element,
                    namespaceManager,
                    cache);

                careTeam.Participant.Add(new CareTeam.ParticipantComponent
                {
                    Member = new ResourceReference($"urn:uuid:{practitioner.Id}")
                });
            }

            return new List<Resource> { careTeam };
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var elements = cCda.XPathSelectElements(
                "n1:ClinicalDocument/n1:documentationOf/n1:serviceEvent/n1:performer/n1:assignedEntity",
                namespaceManager);

            elements = elements.Concat(cCda.XPathSelectElements(
                "n1:ClinicalDocument/n1:componentOf/n1:encompassingEncounter/n1:responsibleParty/n1:assignedEntity",
                namespaceManager));

            elements = elements.Concat(cCda.XPathSelectElements(
                "n1:ClinicalDocument/n1:author/n1:assignedAuthor/n1:assignedPerson/..",
                namespaceManager));

            elements = elements.Concat(cCda.XPathSelectElements(
                "n1:ClinicalDocument/n1:legalAuthenticator/n1:assignedEntity/n1:assignedPerson/..",
                namespaceManager));

            elements = elements.Concat(cCda.XPathSelectElements(
                "n1:ClinicalDocuments/n1:authenticator/n1:assignedEntity/n1:assignedPerson/..",
                namespaceManager));

            elements = elements.Concat(cCda.XPathSelectElements(
                "n1:ClinicalDocument/n1:informationRecipient/n1:intendedRecipient/n1:informationRecipient/..",
                namespaceManager));

            elements = elements.Concat(cCda.XPathSelectElements(
                "n1:ClinicalDocument/n1:participant/n1:associatedEntity/n1:associatedPerson/..",
                namespaceManager));

            return elements;
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            Dictionary<string, Resource> cache)
        {
            var practitionerConverter = new PractitionerConverter(PatientId);
            return practitionerConverter
                .AddToBundle(bundle, new List<XElement> { element }, namespaceManager, cache)
                .First();
        }
    }
}
