using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
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
        protected override void CustomizeMapping(XElement element, Observation observation)
        {
            observation.Meta = new Meta();
            observation.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-smokingstatus"));

            observation
                .Category
                .First()
                .Coding
                .First()
                .Code = "social-history";

            if (!(observation.Effective is FhirDateTime dateTimeElement))
            {
                throw new InvalidOperationException(
                    $"Expected the issued datetime to be a datetime, however, the issued value is of type " +
                    $"{observation.Effective.GetType().Name}");
            }

            observation.Effective = null;
            observation.Issued = dateTimeElement.ToDateTimeOffset(TimeSpan.Zero);
        }
    }
}
