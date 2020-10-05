﻿using System.Linq;
using System.Xml.Linq;
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
        protected override void CustomizeMapping(XElement element, Observation observation)
        {
            observation.Meta = new Meta();
            observation.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/StructureDefinition/vitalsigns"));

            observation
                .Category
                .First()
                .Coding
                .First()
                .Code = "vital-signs";
        }
    }
}
