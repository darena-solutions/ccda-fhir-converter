using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Exceptions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts vital signs into FHIR observation resources
    /// </summary>
    public class VitalSignObservationConverter : BaseObservationConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VitalSignObservationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public VitalSignObservationConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='8716-3']/../n1:entry/n1:organizer/n1:component/n1:observation";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var observation = (Observation)base.PerformElementConversion(element, context);
            observation.Meta = new Meta();
            observation.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/StructureDefinition/vitalsigns"));

            if (observation.Effective == null)
            {
                try
                {
                    throw new RequiredValueNotFoundException(element, "effectiveTime", "Observation.effective");
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            observation
                .Category
                .First()
                .Coding
                .First()
                .Code = "vital-signs";

            return observation;
        }
    }
}
