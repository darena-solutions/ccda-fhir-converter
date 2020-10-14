using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts various elements in the CCDA into observation FHIR resources. These elements indicate a
    /// some physical
    /// status observation of the patient
    /// </summary>
    public class StatusObservationConverter : BaseObservationConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusObservationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public StatusObservationConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            return
                GetFunctionalStatusElements(cCda, namespaceManager)
                    .Concat(GetMentalStatusElements(cCda, namespaceManager))
                    .Concat(GetHealthConcernElements(cCda, namespaceManager));
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var observation = (Observation)base.PerformElementConversion(element, context);
            observation
                .Category
                .First()
                .Coding
                .First()
                .Code = "exam";

            return observation;
        }

        /// <summary>
        /// Gets the functional status elements from the CCDA
        /// </summary>
        /// <param name="cCda">The root CCDA document</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the root CCDA document</param>
        /// <returns>The functional status elements from the CCDA</returns>
        protected virtual IEnumerable<XElement> GetFunctionalStatusElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='47420-5']/../n1:entry/n1:observation";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <summary>
        /// Gets the mental status elements from the CCDA
        /// </summary>
        /// <param name="cCda">The root CCDA document</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the root CCDA document</param>
        /// <returns>The mental status elements from the CCDA</returns>
        protected virtual IEnumerable<XElement> GetMentalStatusElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='10190-7']/../n1:entry/n1:observation";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <summary>
        /// Gets the health concern elements from the CCDA
        /// </summary>
        /// <param name="cCda">The root CCDA document</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the root CCDA document</param>
        /// <returns>The health concern elements from the CCDA</returns>
        protected virtual IEnumerable<XElement> GetHealthConcernElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='75310-3']/../n1:entry/n1:observation";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }
    }
}
