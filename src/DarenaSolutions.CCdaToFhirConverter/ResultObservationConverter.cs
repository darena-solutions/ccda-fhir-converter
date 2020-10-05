using System.Linq;
using System.Xml.Linq;
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
        protected override void CustomizeMapping(XElement element, Observation observation)
        {
            observation.Meta = new Meta();
            observation.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-observation-lab"));

            observation
                .Category
                .First()
                .Coding
                .First()
                .Code = "laboratory";
        }
    }
}
