using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts various elements in the CCDA to condition FHIR resources. This converter is specifically
    /// for encounter diagnoses sections of the CCDA
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
            var xPath =
                "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.22.1']/.." +
                "/n1:entry/n1:encounter/n1:entryRelationship/n1:act/n1:entryRelationship/n1:observation";

            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            var condition = (Condition)base.PerformElementConversion(bundle, element, namespaceManager, cacheManager);
            var categoryCodeableConcept = element
                .FindCodeElementWithTranslation()?
                .ToCodeableConcept();

            if (categoryCodeableConcept == null)
                throw new InvalidOperationException($"A condition category was not found in: {element}");

            condition.Category.Add(new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/condition-category",
                "encounter-diagnosis",
                "Encounter Diagnosis",
                null));

            return condition;
        }
    }
}
