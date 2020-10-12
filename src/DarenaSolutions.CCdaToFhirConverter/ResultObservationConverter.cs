using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts laboratory results into FHIR observation resources
    /// </summary>
    public class ResultObservationConverter : BaseObservationConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResultObservationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public ResultObservationConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='30954-2']/../n1:entry/n1:organizer/n1:component/n1:observation";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            Dictionary<string, Resource> cache)
        {
            var observation = (Observation)base.PerformElementConversion(bundle, element, namespaceManager, cache);
            observation.Meta = new Meta();
            observation.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-observation-lab"));

            observation
                .Category
                .First()
                .Coding
                .First()
                .Code = "laboratory";

            return observation;
        }
    }
}
