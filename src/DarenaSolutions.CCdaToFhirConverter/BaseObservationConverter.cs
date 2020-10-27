using System;
using System.Collections.Generic;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Exceptions;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// A base converter that contains the common mapping between all observation type resources
    /// </summary>
    public abstract class BaseObservationConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseObservationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        protected BaseObservationConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var id = Guid.NewGuid().ToString();
            var observation = new Observation
            {
                Id = id,
                Status = ObservationStatus.Final,
                Subject = new ResourceReference($"urn:uuid:{PatientId}")
            };

            var cachedResource = element.SetIdentifiers(context, observation);
            if (cachedResource != null)
                return cachedResource;

            // Category
            // The category code should be included by all derived instances of this base converter
            var codeConcept = new CodeableConcept
            {
                Coding = new List<Coding>
                {
                    new Coding
                    {
                        System = "http://terminology.hl7.org/CodeSystem/observation-category"
                    }
                }
            };

            observation.Category.Add(codeConcept);

            try
            {
                // Code
                observation.Code = element
                    .FindCodeElementWithTranslation()?
                    .ToCodeableConcept("Observation.code");

                if (observation.Code == null)
                    throw new RequiredValueNotFoundException(element, "code", "Observation.code");
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            observation.Effective = element
                .Element(Defaults.DefaultNs + "effectiveTime")?
                .ToDateTimeElement();

            try
            {
                observation.Value = element
                    .Element(Defaults.DefaultNs + "value")?
                    .ToFhirElementBasedOnType(fhirPropertyPath: "Observation.value");
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            // Commit Resource
            context.Bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = observation
            });

            return observation;
        }
    }
}
