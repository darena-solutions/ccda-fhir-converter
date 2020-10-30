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
    /// Converter that converts vital signs into FHIR observation resources
    /// </summary>
    public class VitalSignObservationConverter : BaseObservationConverter
    {
        private const string GeneralVitalSignProfileUri = "http://hl7.org/fhir/StructureDefinition/vitalsigns";

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

            var codeCoding = observation.Code?.Coding?.FirstOrDefault();
            switch (codeCoding?.Code)
            {
                case "59576-9":
                    observation.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/pediatric-bmi-for-age"));
                    break;
                case "8289-1":
                case "74728-7":
                    // 74728-7 is a code EHR's sometimes use for circumference. Update the code to match profile
                    codeCoding.Code = "8289-1";
                    observation.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/head-occipital-frontal-circumference-percentile"));
                    break;
                case "77606-2":
                    observation.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/pediatric-weight-for-height"));
                    break;
                case "59408-5":
                case "2710-2":
                    // 2710-2 is a deprecated code, but still valid. Update it to latest code
                    codeCoding.Code = "59408-5";
                    observation.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-pulse-oximetry"));

                    // Also add additional coding required by the oxygen saturation profile
                    observation.Code.Coding.Add(new Coding(
                        "http://loinc.org",
                        "2708-6"));
                    break;
                default:
                    observation.Meta.ProfileElement.Add(new Canonical(GeneralVitalSignProfileUri));
                    break;
            }

            // Ensure correct system is being used for specific vital signs
            switch (codeCoding?.Code)
            {
                case "59576-9":
                case "8289-1":
                case "77606-2":
                case "59408-5":
                    var codeElement = element.FindCodeElementWithTranslation();

                    try
                    {
                        if (string.IsNullOrWhiteSpace(codeCoding.System))
                            throw new RequiredValueNotFoundException(codeElement, "[@codeSystem]", "Observation.code.coding.system");

                        if (codeCoding.System != "http://loinc.org")
                        {
                            throw new UnrecognizedValueException(
                                codeElement,
                                codeCoding.System,
                                elementAttributeName: "codeSystem",
                                fhirPropertyPath: "Observation.code.coding.system");
                        }
                    }
                    catch (Exception exception)
                    {
                        context.Exceptions.Add(exception);
                    }

                    break;
            }

            // Pediatric codes have a specific value quantity requirement
            switch (codeCoding?.Code)
            {
                case "59576-9":
                case "8289-1":
                case "77606-2":
                case "59408-5":
                    if (observation.Value is Quantity valueQuantity)
                    {
                        var valueElement = element.Element(Namespaces.DefaultNs + "value");

                        if (valueQuantity.Value == null)
                        {
                            try
                            {
                                throw new RequiredValueNotFoundException(valueElement, "[@value]", "Observation.valueQuantity.value");
                            }
                            catch (Exception exception)
                            {
                                context.Exceptions.Add(exception);
                            }
                        }

                        if (string.IsNullOrWhiteSpace(valueQuantity.Unit))
                        {
                            try
                            {
                                throw new RequiredValueNotFoundException(valueElement, "[@unit]", "Observation.valueQuantity.unit");
                            }
                            catch (Exception exception)
                            {
                                context.Exceptions.Add(exception);
                            }
                        }

                        valueQuantity.System = "http://unitsofmeasure.org";
                        valueQuantity.Code = "%";
                    }

                    break;
            }

            // The effective time is not required for specific vital signs, but is required for the generic vital sign
            if (observation.Meta.ProfileElement[0].Value == GeneralVitalSignProfileUri && observation.Effective == null)
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
