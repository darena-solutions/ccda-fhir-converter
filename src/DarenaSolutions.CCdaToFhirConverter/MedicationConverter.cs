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
    /// <inheritdoc />
    public class MedicationConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="MedicationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public MedicationConverter(string patientId)
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
                var substanceAdministrationElement = element.Element(Defaults.DefaultNs + "substanceAdministration");
                if (substanceAdministrationElement == null)
                    continue;

                var medicationStatementConverter = new MedicationStatementConverter(_patientId);
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

                    var translationElement = manufacturedMaterialElement
                        .Element(Defaults.DefaultNs + "code")?
                        .Element(Defaults.DefaultNs + "translation");

                    if (translationElement != null)
                    {
                        medication.Code = translationElement.ToCodeableConcept();
                    }
                    else
                    {
                        var codeElement = manufacturedMaterialElement.Element(Defaults.DefaultNs + "code");
                        if (codeElement == null)
                            throw new InvalidOperationException($"Could not find a medication code in: {manufacturedMaterialElement}");

                        medication.Code = codeElement.ToCodeableConcept();
                    }

                    bundle.Entry.Add(new Bundle.EntryComponent
                    {
                        FullUrl = $"urn:uuid:{id}",
                        Resource = medication
                    });

                    medicationStatementConverter.MedicationStatement.Medication = new ResourceReference($"urn:uuid:{id}");

                    var entryRelationshipElements = substanceAdministrationElement.Elements(Defaults.DefaultNs + "entryRelationship");
                    foreach (var entryRelationshipElement in entryRelationshipElements)
                    {
                        var supplyElement = entryRelationshipElement.Element(Defaults.DefaultNs + "supply");
                        if (supplyElement != null)
                        {
                            var medicationRequestConverter = new MedicationRequestConverter(_patientId, id);
                            medicationRequestConverter.AddToBundle(
                                bundle,
                                new List<XElement> { supplyElement },
                                namespaceManager,
                                cacheManager);
                        }
                    }
                }

                var representedOrganizationXPath = "n1:informant/n1:assignedEntity/n1:representedOrganization";
                var representedOrganizationElement = substanceAdministrationElement.XPathSelectElement(representedOrganizationXPath, namespaceManager);
                if (representedOrganizationElement != null)
                {
                    var representedOrganizationConverter = new RepresentedOrganizationConverter();
                    representedOrganizationConverter.AddToBundle(
                        bundle,
                        new List<XElement> { representedOrganizationElement },
                        namespaceManager,
                        cacheManager);
                }
            }
        }
    }
}
