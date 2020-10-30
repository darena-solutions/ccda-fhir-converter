using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Exceptions;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts various elements in the CCDA into practitioner FHIR resources
    /// </summary>
    public class PractitionerConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PractitionerConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public PractitionerConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            throw new InvalidOperationException(
                "This converter is not intended to be used as a standalone converter. Practitioner elements must " +
                "be determined before using this converter. This converter itself cannot determine practitioner resources");
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var id = Guid.NewGuid().ToString();
            var practitioner = new Practitioner
            {
                Id = id,
                Meta = new Meta()
            };

            practitioner.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-practitioner"));
            var cachedResource = element.SetIdentifiers(context, practitioner);
            if (cachedResource != null)
                return cachedResource;

            try
            {
                var nameElements = element
                    .Element(Namespaces.DefaultNs + "assignedPerson")?
                    .Elements(Namespaces.DefaultNs + "name")
                    .ToList();

                if (nameElements == null || !nameElements.Any())
                    throw new RequiredValueNotFoundException(element, "assignedPerson/name", "Practitioner.name");

                foreach (var nameElement in nameElements)
                {
                    try
                    {
                        var humanName = nameElement.ToHumanName("Practitioner.name");
                        if (string.IsNullOrWhiteSpace(humanName.Family))
                            throw new RequiredValueNotFoundException(nameElement, "family", "Practitioner.name.family");

                        practitioner.Name.Add(humanName);
                    }
                    catch (Exception exception)
                    {
                        context.Exceptions.Add(exception);
                    }
                }
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            var addressElements = element.Elements(Namespaces.DefaultNs + "addr");
            foreach (var addressElement in addressElements)
            {
                try
                {
                    practitioner.Address.Add(addressElement.ToAddress("Practitioner.address"));
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            var telecomElements = element.Elements(Namespaces.DefaultNs + "telecom");
            foreach (var telecomElement in telecomElements)
            {
                try
                {
                    practitioner.Telecom.Add(telecomElement.ToContactPoint("Practitioner.telecom"));
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            var qualificationCodes = element.Elements(Namespaces.DefaultNs + "code");
            foreach (var qualificationCode in qualificationCodes)
            {
                try
                {
                    practitioner.Qualification.Add(new Practitioner.QualificationComponent
                    {
                        Code = qualificationCode.ToCodeableConcept("Practitioner.qualification.code")
                    });
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            context.Bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = practitioner
            });

            return practitioner;
        }
    }
}
