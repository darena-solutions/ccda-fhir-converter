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
            throw new InvalidOperationException(
                "This converter is not intended to be used as a standalone converter. Medication elements must be " +
                "determined before using this converter. This converter itself cannot determine medication resources");
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            Dictionary<string, Resource> cache)
        {
            var id = Guid.NewGuid().ToString();
            var medication = new Medication
            {
                Id = id,
                Meta = new Meta()
            };

            medication.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-medication"));
            medication.Code = element
                .Element(Defaults.DefaultNs + "code")?
                .ToCodeableConcept();

            if (medication.Code == null)
                throw new RequiredValueNotFoundException(element, "code");

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = medication
            });

            return medication;
        }
    }
}
