using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Exceptions;
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
        protected override Resource PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            Dictionary<string, Resource> cache)
        {
            var condition = (Condition)base.PerformElementConversion(bundle, element, namespaceManager, cache);

            // Get the value from the observation element
            var xPath = "../../n1:entry/n1:observation/n1:value";
            var valueEl = element
                .XPathSelectElement(xPath, namespaceManager)?
                .ToFhirElementBasedOnType(new[] { "co", "cd" }, "Condition.code");

            if (valueEl == null)
            {
                throw new RequiredValueNotFoundException(
                    element,
                    "../../entry/observation/value",
                    "Condition.code");
            }

            condition.Code = (CodeableConcept)valueEl;
            condition.Category.Add(new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/condition-category",
                "health-concern",
                "Health Concern",
                null));

            return condition;
        }
    }
}
