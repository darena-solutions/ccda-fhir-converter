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
    /// Converter that converts various elements in the CCDA to condition FHIR resources. This converter is specifically
    /// for problem list sections of the CCDA
    /// </summary>
    public class ProblemListConditionConverter : BaseConditionConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProblemListConditionConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public ProblemListConditionConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='11450-4']/../n1:entry/n1:act/n1:entryRelationship/n1:observation";
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
            condition.Code = element
                .FindCodeElementWithTranslation(codeElementName: "value")?
                .ToCodeableConcept("Condition.code");

            if (condition.Code == null)
                throw new RequiredValueNotFoundException(element, "value", "Condition.code");

            condition.Category.Add(new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/condition-category",
                "problem-list-item",
                "Problem List Item",
                null));

            // Provenance
            var authorElement = element.Elements(Defaults.DefaultNs + "author").FirstOrDefault();
            if (authorElement == null)
                return condition;

            var provenanceConverter = new ProvenanceConverter(PatientId);
            var provenanceResources = provenanceConverter.AddToBundle(
                bundle,
                new List<XElement> { authorElement },
                namespaceManager,
                cache);

            var provenance = provenanceResources.GetFirstResourceAsType<Provenance>();
            provenance.Target.Add(new ResourceReference($"{ResourceType.Condition}/{condition.Id}"));

            return condition;
        }
    }
}
