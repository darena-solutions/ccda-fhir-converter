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
                "problem-list-item",
                "Problem List Item",
                null));

            // Provenance
            var authorElement = element.Elements(Defaults.DefaultNs + "author").FirstOrDefault();
            if (authorElement == null)
                return condition;

            try
            {
                var provenanceConverter = new ProvenanceConverter(PatientId);
                var provenanceResources = provenanceConverter.AddToBundle(new List<XElement> { authorElement }, context);

                var provenance = provenanceResources.GetFirstResourceAsType<Provenance>();
                provenance.Target.Add(new ResourceReference($"{ResourceType.Condition}/{condition.Id}"));
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            return condition;
        }
    }
}
