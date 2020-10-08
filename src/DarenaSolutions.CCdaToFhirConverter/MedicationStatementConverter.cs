using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts various elements in the CCDA into medication statement FHIR resources
    /// </summary>
    public class MedicationStatementConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MedicationStatementConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public MedicationStatementConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            throw new InvalidOperationException(
                "This converter is not intended to be used as a standalone converter. Medication statement elements must " +
                "be determined before using this converter. This converter itself cannot determine medication statement resources");
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            var id = Guid.NewGuid().ToString();
            var medicationStatement = new MedicationStatement
            {
                Id = id,
                Subject = new ResourceReference($"urn:uuid:{PatientId}"),
                Status = MedicationStatement.MedicationStatusCodes.Active
            };

            var identifierElements = element.Elements(Defaults.DefaultNs + "id");
            foreach (var identifierElement in identifierElements)
            {
                medicationStatement.Identifier.Add(identifierElement.ToIdentifier());
            }

            medicationStatement.Effective = element
                .Element(Defaults.DefaultNs + "effectiveTime")?
                .ToDateTimeElement();

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = medicationStatement
            });

            var medicationXPath = "n1:consumable/n1:manufacturedProduct/n1:manufacturedMaterial";
            var medicationEl = element.XPathSelectElement(medicationXPath, namespaceManager);
            if (medicationEl != null)
            {
                var medicationConverter = new MedicationConverter(PatientId);
                var medication = medicationConverter
                    .AddToBundle(bundle, new List<XElement> { medicationEl }, namespaceManager, cacheManager)
                    .First();

                medicationStatement.Medication = new ResourceReference($"urn:uuid:{medication.Id}");
            }

            return medicationStatement;
        }
    }
}
