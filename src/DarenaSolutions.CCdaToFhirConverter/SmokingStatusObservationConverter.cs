using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Exceptions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts smoking status elements into FHIR observation resources
    /// </summary>
    public class SmokingStatusObservationConverter : BaseObservationConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmokingStatusObservationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public SmokingStatusObservationConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='29762-2']/../n1:entry/n1:observation/n1:code[@code='72166-2']/..";
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
            observation.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-smokingstatus"));

            if (observation.Value == null)
                throw new RequiredValueNotFoundException(element, "value");

            var valueEl = element.Element(Defaults.DefaultNs + "value");
            if (!(observation.Value is CodeableConcept))
                throw new UnexpectedValueTypeException(valueEl, valueEl.Attribute(Defaults.XsiNs + "type").Value);

            observation
                .Category
                .First()
                .Coding
                .First()
                .Code = "social-history";

            if (observation.Effective == null)
                throw new RequiredValueNotFoundException(element, "effectiveTime");

            if (observation.Effective is FhirDateTime dateTimeElement)
                observation.Issued = dateTimeElement.ToDateTimeOffset(TimeSpan.Zero);
            else if (observation.Effective is Period periodElement)
                observation.Issued = periodElement.StartElement.ToDateTimeOffset(TimeSpan.Zero);

            observation.Effective = null;
            return observation;
        }
    }
}
