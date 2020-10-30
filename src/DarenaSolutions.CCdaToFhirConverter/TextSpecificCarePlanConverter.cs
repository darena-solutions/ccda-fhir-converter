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
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
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
            carePlan.Category.Add(new CodeableConcept(
                "http://hl7.org/fhir/us/core/CodeSystem/careplan-category",
                "assess-plan"));

            try
            {
                // A code element will always exist in a section, we know this because of GetPrimaryElements which will
                // take care plan elements based on reading the code value in a section
                var sectionElement = element.Name.LocalName == "section"
                    ? element
                    : element.XPathSelectElement("ancestor::n1:section", context.NamespaceManager);

                var codeableConcept = sectionElement
                    .FindCodeElementWithTranslation()
                    .ToCodeableConcept("CarePlan.category");

                var coding = codeableConcept.Coding.First();
                switch (coding.Code)
                {
                    case "51848-0":
                        // Assessments
                        var clinicalDocumentIdentifier = context.CCda
                            .XPathSelectElement("n1:ClinicalDocument/n1:id", context.NamespaceManager)?
                            .ToIdentifier("CarePlan.identifier.value");

                        if (clinicalDocumentIdentifier == null)
                        {
                            throw new RequiredValueNotFoundException(
                                context.CCda.Root,
                                "id",
                                "CarePlan.identifier.value");
                        }

                        var patientIdentifier = context.CCda
                            .XPathSelectElement("n1:ClinicalDocument/n1:recordTarget/n1:patientRole/n1:id", context.NamespaceManager)?
                            .ToIdentifier("CarePlan.identifier.value");

                        if (patientIdentifier == null)
                        {
                            throw new RequiredValueNotFoundException(
                                context.CCda.Root,
                                "recordTarget/patientRole/id",
                                "CarePlan.identifier.value");
                        }

                        var identifierValue = $"{clinicalDocumentIdentifier.Value}|{patientIdentifier.Value}|{coding.Code}";
                        var cacheKey = $"{ResourceType.CarePlan}|{Systems.SampleBbpSystem}|{identifierValue}";
                        if (context.Cache.TryGetValue(cacheKey, out var resource))
                            return resource;

                        carePlan.Identifier.Add(new Identifier(Systems.SampleBbpSystem, identifierValue));
                        context.Cache.Add(cacheKey, carePlan);
                        break;
                    default:
                        var idElement = coding.Code == "42349-1"
                            ? element.XPathSelectElement("n1:entry/n1:observation[@moodCode='INT']", context.NamespaceManager)
                            : element;

                        var cachedResource = idElement.SetIdentifiers(context, carePlan);
                        if (cachedResource != null)
                            return cachedResource;

                        break;
                }

                carePlan.Category.Add(codeableConcept);
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            try
            {
                var textEl = element.Element(Namespaces.DefaultNs + "text")?.GetContentsAsString();
                if (string.IsNullOrWhiteSpace(textEl))
                    throw new RequiredValueNotFoundException(element, "text", "CarePlan.text.div");

                carePlan.Text = new Narrative
                {
                    Div = textEl,
                    Status = Narrative.NarrativeStatus.Generated
                };
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            // Commit Resource
            context.Bundle.Entry.Add(new Bundle.EntryComponent
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
            var xPath =
                "//n1:section/n1:code[@code='18776-5']/../n1:entry/n1:observation |" +
                "//n1:section/n1:code[@code='18776-5']/../n1:entry/n1:act";

            return cCda.XPathSelectElements(xPath, namespaceManager);
        }
    }
}
