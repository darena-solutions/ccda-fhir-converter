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
    /// Converter that converts various elements in the CCDA into care plan FHIR resources. These elements are text-specific
    /// free-form elements that will be converted
    /// </summary>
    public class TextSpecificCarePlanConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextSpecificCarePlanConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public TextSpecificCarePlanConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            return
                GetReferralElements(cCda, namespaceManager)
                    .Concat(GetAssessmentElements(cCda, namespaceManager))
                    .Concat(GetCarePlanElements(cCda, namespaceManager));
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            var id = Guid.NewGuid().ToString();
            var carePlan = new CarePlan
            {
                Id = id,
                Meta = new Meta(),
                Status = RequestStatus.Active,
                Intent = CarePlan.CarePlanIntent.Plan,
                Subject = new ResourceReference($"urn:uuid:{PatientId}")
            };

            carePlan.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-careplan"));

            var textEl = element.Element(Defaults.DefaultNs + "text")?.GetContentsAsString();
            if (string.IsNullOrWhiteSpace(textEl))
                throw new RequiredValueNotFoundException(element, "text");

            carePlan.Text = new Narrative
            {
                Div = textEl,
                Status = Narrative.NarrativeStatus.Generated
            };

            carePlan.Category.Add(new CodeableConcept(
                "http://hl7.org/fhir/us/core/CodeSystem/careplan-category",
                "assess-plan"));

            var codeableConcept = element
                .FindCodeElementWithTranslation()?
                .ToCodeableConcept();

            if (codeableConcept != null)
                carePlan.Category.Add(codeableConcept);

            // Commit Resource
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = carePlan
            });

            return carePlan;
        }

        /// <summary>
        /// Gets the referral elements from the CCDA
        /// </summary>
        /// <param name="cCda">The root CCDA document</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the root CCDA document</param>
        /// <returns>The referral elements from the CCDA</returns>
        protected virtual IEnumerable<XElement> GetReferralElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='42349-1']/..";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <summary>
        /// Gets the assessment elements from the CCDA
        /// </summary>
        /// <param name="cCda">The root CCDA document</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the root CCDA document</param>
        /// <returns>The assessment elements from the CCDA</returns>
        protected virtual IEnumerable<XElement> GetAssessmentElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='51848-0']/..";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <summary>
        /// Gets the care plan elements from the CCDA
        /// </summary>
        /// <param name="cCda">The root CCDA document</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the root CCDA document</param>
        /// <returns>The care plan elements from the CCDA</returns>
        protected virtual IEnumerable<XElement> GetCarePlanElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='18776-5']/..";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }
    }
}
