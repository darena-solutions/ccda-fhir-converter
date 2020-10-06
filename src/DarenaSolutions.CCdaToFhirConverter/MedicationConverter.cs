using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts various elements in the CCDA into medication FHIR resources
    /// </summary>
    public class MedicationConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MedicationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public MedicationConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.1.1']/../n1:entry";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override void PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            var substanceAdministrationElement = element.Element(Defaults.DefaultNs + "substanceAdministration");
            if (substanceAdministrationElement == null)
                return;

            var medicationStatementConverter = new MedicationStatementConverter(PatientId);
            medicationStatementConverter.AddToBundle(
                bundle,
                new List<XElement> { substanceAdministrationElement },
                namespaceManager,
                cacheManager);

            var manufacturedMaterialXPath = "n1:consumable/n1:manufacturedProduct/n1:manufacturedMaterial";
            var manufacturedMaterialElement = substanceAdministrationElement.XPathSelectElement(manufacturedMaterialXPath, namespaceManager);

            if (manufacturedMaterialElement != null)
            {
                var id = Guid.NewGuid().ToString();
                var medication = new Medication
                {
                    Id = id,
                    Meta = new Meta()
                };

                medication.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-medication"));

                var codeableConcept = manufacturedMaterialElement
                    .FindCodeElementWithTranslation()?
                    .ToCodeableConcept();

                if (codeableConcept == null)
                    throw new InvalidOperationException($"Could not find a medication code in: {manufacturedMaterialElement}");

                medication.Code = codeableConcept;
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = medication
                });

                Resources.Add(medication);
                medicationStatementConverter.GetFirstResourceAsType<MedicationStatement>().Medication = new ResourceReference($"urn:uuid:{id}");

                var entryRelationshipElements = substanceAdministrationElement.Elements(Defaults.DefaultNs + "entryRelationship");
                foreach (var entryRelationshipElement in entryRelationshipElements)
                {
                    var supplyElement = entryRelationshipElement.Element(Defaults.DefaultNs + "supply");
                    if (supplyElement != null)
                    {
                        var medicationRequestConverter = new MedicationRequestConverter(PatientId);
                        medicationRequestConverter.AddToBundle(
                            bundle,
                            new List<XElement> { supplyElement },
                            namespaceManager,
                            cacheManager);

                        medicationRequestConverter.GetFirstResourceAsType<MedicationRequest>().Medication = new ResourceReference($"urn:uuid:{id}");
                    }
                }
            }

            var representedOrganizationXPath = "n1:informant/n1:assignedEntity/n1:representedOrganization";
            var representedOrganizationElement = substanceAdministrationElement.XPathSelectElement(representedOrganizationXPath, namespaceManager);
            if (representedOrganizationElement != null)
            {
                var representedOrganizationConverter = new OrganizationConverter();
                representedOrganizationConverter.AddToBundle(
                    bundle,
                    representedOrganizationElement,
                    namespaceManager,
                    cacheManager);
            }
        }
    }
}
