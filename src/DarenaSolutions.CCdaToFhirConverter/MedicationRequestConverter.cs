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
    /// Converter that converts various elements in the CCDA into medication request FHIR resources
    /// </summary>
    public class MedicationRequestConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MedicationRequestConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public MedicationRequestConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='10160-0']/../n1:entry/n1:substanceAdministration[@moodCode='INT']";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var id = Guid.NewGuid().ToString();
            var request = new MedicationRequest
            {
                Id = id,
                Meta = new Meta(),
                Status = MedicationRequest.medicationrequestStatus.Active,
                Intent = MedicationRequest.medicationRequestIntent.Order,
                Subject = new ResourceReference($"urn:uuid:{PatientId}")
            };

            request.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-medicationrequest"));
            var cachedResource = element.SetIdentifiers(context, request);
            if (cachedResource != null)
                return cachedResource;

            try
            {
                var medicationXPath = "n1:consumable/n1:manufacturedProduct";
                var medicationEl = element.XPathSelectElement(medicationXPath, context.NamespaceManager);
                if (medicationEl == null)
                {
                    throw new RequiredValueNotFoundException(
                        element,
                        "consumable/manufacturedProduct",
                        "MedicationRequest.medication");
                }

                var converter = new MedicationConverter(PatientId);
                var medication = converter
                    .AddToBundle(new List<XElement> { medicationEl }, context)
                    .First();

                request.Medication = new ResourceReference($"urn:uuid:{medication.Id}");
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            try
            {
                request.AuthoredOnElement = element
                    .Element(Namespaces.DefaultNs + "effectiveTime")?
                    .ToFhirDateTime();

                if (request.AuthoredOnElement == null)
                {
                    throw new RequiredValueNotFoundException(
                        element,
                        "effectiveTime",
                        "MedicationRequest.authoredOn");
                }
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            try
            {
                var authorEl = element
                    .Element(Namespaces.DefaultNs + "author")?
                    .Element(Namespaces.DefaultNs + "assignedAuthor");

                if (authorEl == null)
                {
                    throw new RequiredValueNotFoundException(
                        element,
                        "author/assignedAuthor",
                        "MedicationRequest.requester");
                }

                var practitionerConverter = new PractitionerConverter(PatientId);
                var resource = practitionerConverter
                    .AddToBundle(new List<XElement> { authorEl }, context)
                    .First();

                request.Requester = new ResourceReference($"urn:uuid:{resource.Id}");
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            var dosageInstructionXPath = "n1:entryRelationship/n1:act/n1:templateId[@root='2.16.840.1.113883.10.20.22.4.20']/../n1:text";
            var dosageInstruction = element.XPathSelectElement(dosageInstructionXPath, context.NamespaceManager);
            if (dosageInstruction != null)
            {
                request.DosageInstruction.Add(new Dosage
                {
                    Text = dosageInstruction.GetFirstTextNode()
                });
            }

            context.Bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = request
            });

            return request;
        }
    }
}
