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
            var xPath = "//n1:section/n1:code[@code='10160-0']/../n1:entry/n1:substanceAdministration";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var id = Guid.NewGuid().ToString();
            var medicationStatement = new MedicationStatement
            {
                Id = id,
                Subject = new ResourceReference($"urn:uuid:{PatientId}"),
                Status = MedicationStatement.MedicationStatusCodes.Active
            };

            var cachedResource = element.SetIdentifiers(context, medicationStatement);
            if (cachedResource != null)
                return cachedResource;

            medicationStatement.Effective = element
                .Element(Defaults.DefaultNs + "effectiveTime")?
                .ToDateTimeElement();

            try
            {
                var medicationXPath = "n1:consumable/n1:manufacturedProduct/n1:manufacturedMaterial";
                var medicationEl = element.XPathSelectElement(medicationXPath, context.NamespaceManager);
                if (medicationEl == null)
                {
                    throw new RequiredValueNotFoundException(
                        element,
                        "consumable/manufacturedProduct/manufacturedMaterial",
                        "MedicationStatement.medication");
                }

                var medicationConverter = new MedicationConverter(PatientId);
                var medication = medicationConverter
                    .AddToBundle(new List<XElement> { medicationEl }, context)
                    .First();

                medicationStatement.Medication = new ResourceReference($"urn:uuid:{medication.Id}");
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            try
            {
                var authorEl = element.Element(Defaults.DefaultNs + "author");
                if (authorEl != null)
                {
                    var provenanceConverter = new ProvenanceConverter(PatientId);
                    var provenance = provenanceConverter
                        .AddToBundle(new List<XElement> { authorEl }, context)
                        .GetFirstResourceAsType<Provenance>();

                    provenance.Target.Add(new ResourceReference($"urn:uuid:{id}"));
                }
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            context.Bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = medicationStatement
            });

            return medicationStatement;
        }
    }
}
