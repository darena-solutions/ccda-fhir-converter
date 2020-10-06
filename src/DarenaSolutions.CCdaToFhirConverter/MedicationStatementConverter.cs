using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts various elements in the CCDA into medication statement FHIR resources
    /// </summary>
    public class MedicationStatementConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MedicationStatementConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public MedicationStatementConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            throw new InvalidOperationException(
                "This converter is not intended to be used as a standalone converter. Medication statement elements must " +
                "be determined before using this converter. This converter itself cannot determine medication statement resources");
        }

        /// <inheritdoc />
        protected override void PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            var id = Guid.NewGuid().ToString();
            var medicationStatement = new MedicationStatement
            {
                Id = id,
                Subject = new ResourceReference($"urn:uuid:{PatientId}")
            };

            var identifierElements = element.Elements(Defaults.DefaultNs + "id");
            foreach (var identifierElement in identifierElements)
            {
                medicationStatement.Identifier.Add(identifierElement.ToIdentifier());
            }

            var statusCodeValue = element
                .Element(Defaults.DefaultNs + "statusCode")?
                .Attribute("code")?
                .Value;

            if (!string.IsNullOrWhiteSpace(statusCodeValue))
            {
                var statusCode = EnumUtility.ParseLiteral<MedicationStatement.MedicationStatusCodes>(statusCodeValue, true);
                if (statusCode == null)
                    throw new InvalidOperationException($"Could not determine medication status code from value: {statusCodeValue}");

                medicationStatement.Status = statusCode;
            }

            var effectiveTimeElement = element.Element(Defaults.DefaultNs + "effectiveTime");
            medicationStatement.Effective = effectiveTimeElement?.ToDateTimeElement();

            var doseQuantityElement = element.Element(Defaults.DefaultNs + "doseQuantity");
            if (doseQuantityElement != null)
            {
                Quantity quantity;

                var nullFlavorValue = doseQuantityElement.Attribute("nullFlavor")?.Value;
                if (!string.IsNullOrWhiteSpace(nullFlavorValue))
                {
                    if (!nullFlavorValue.IsValidNullFlavorValue())
                        throw new InvalidOperationException($"The null flavor value '{nullFlavorValue}' is invalid: {doseQuantityElement}");

                    quantity = new Quantity
                    {
                        Extension = new List<Extension>
                        {
                            new Extension(Defaults.NullFlavorSystem, new Code(nullFlavorValue))
                        }
                    };
                }
                else
                {
                    var quantityValue = doseQuantityElement.Attribute("value")?.Value;
                    if (string.IsNullOrWhiteSpace(quantityValue))
                        throw new InvalidOperationException($"Could not find dosage quantity in: {doseQuantityElement}");

                    quantity = new Quantity
                    {
                        Value = decimal.Parse(quantityValue)
                    };

                    var unitValue = doseQuantityElement.Attribute("unit")?.Value;
                    if (!string.IsNullOrWhiteSpace(unitValue))
                        quantity.Unit = unitValue;
                }

                medicationStatement.Dosage.Add(new Dosage
                {
                    DoseAndRate = new List<Dosage.DoseAndRateComponent>
                    {
                        new Dosage.DoseAndRateComponent
                        {
                            Dose = quantity
                        }
                    }
                });
            }

            var assignedAuthorElement = element
                .Element(Defaults.DefaultNs + "author")?
                .Element(Defaults.DefaultNs + "assignedAuthor");

            if (assignedAuthorElement != null)
            {
                var practitionerConverter = new PractitionerConverter(PatientId);
                practitionerConverter.AddToBundle(
                    bundle,
                    new List<XElement> { assignedAuthorElement },
                    namespaceManager,
                    cacheManager);

                medicationStatement.InformationSource = new ResourceReference($"urn:uuid:{practitionerConverter.Resources[0].Id}");
            }

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = medicationStatement
            });

            Resources.Add(medicationStatement);
        }
    }
}
