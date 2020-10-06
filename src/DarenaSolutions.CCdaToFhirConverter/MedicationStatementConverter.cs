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
    /// <inheritdoc />
    public class MedicationStatementConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="MedicationStatementConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public MedicationStatementConverter(string patientId)
        {
            _patientId = patientId;
        }

        /// <inheritdoc />
        public Resource Resource { get; private set; }

        /// <inheritdoc />
        public virtual void AddToBundle(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            var id = Guid.NewGuid().ToString();
            var medicationStatement = new MedicationStatement
            {
                Id = id,
                Subject = new ResourceReference($"urn:uuid:{_patientId}")
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
                var practitionerConverter = new PractitionerConverter();
                practitionerConverter.AddToBundle(
                    bundle,
                    new List<XElement> { assignedAuthorElement },
                    namespaceManager,
                    cacheManager);

                medicationStatement.InformationSource = new ResourceReference($"urn:uuid:{practitionerConverter.PractitionerId}");
            }

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = medicationStatement
            });

            Resource = medicationStatement;
        }
    }
}
