using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Exceptions;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts various elements in the CCDA to condition FHIR resources.
    /// This converter is specifically for encounter diagnoses entries for an encounter in a CCDA
    /// </summary>
    public class EncounterDiagnosesConditionConverter : BaseConditionConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncounterDiagnosesConditionConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public EncounterDiagnosesConditionConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            throw new InvalidOperationException(
                "This converter is not intended to be used as a standalone converter. Encounter diagnosis elements must " +
                "be determined from the encounter resource before using this converter. This converter itself cannot determine encounter diagnosis resources");
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var condition = (Condition)base.PerformElementConversion(element, context);

            try
            {
                condition.Code = element
                    .FindCodeElementWithTranslation(codeElementName: "value")?
                    .ToCodeableConcept("Condition.code");

                if (condition.Code == null)
                    throw new RequiredValueNotFoundException(element, "value", "Condition.code");
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            condition.Category.Add(new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/condition-category",
                "encounter-diagnosis",
                "Encounter Diagnosis",
                null));

            return condition;
        }
    }
}
