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
    /// Converter that converts various elements in the CCDA to condition FHIR resources. This converter is specifically
    /// for health concern sections of the CCDA
    /// </summary>
    public class HealthConcernConditionConverter : BaseConditionConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HealthConcernConditionConverter"/> class.
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public HealthConcernConditionConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='75310-3']/../n1:entry/n1:act";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override void PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            base.PerformElementConversion(bundle, element, namespaceManager, cacheManager);

            var condition = (Condition)Resources[^1];

            // Get the value from the observation element
            var xPath = "../../n1:entry/n1:observation";
            var observationEl = element.XPathSelectElement(xPath, namespaceManager);

            if (observationEl == null)
                throw new InvalidOperationException($"A condition code was not found for the health concern: {element}");

            var valueEl = observationEl
                .Element(Defaults.DefaultNs + "value")?
                .ToFhirElementBasedOnType();

            if (!(valueEl is CodeableConcept codeableConcept))
            {
                throw new InvalidOperationException(
                    $"Expected the condition code for the health concern to be a codeable concept, however " +
                    $"the value type is not recognized: {observationEl}");
            }

            condition.Code = codeableConcept;
            condition.Category.Add(new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/condition-category",
                "health-concern",
                "Health Concern",
                null));
        }
    }
}
