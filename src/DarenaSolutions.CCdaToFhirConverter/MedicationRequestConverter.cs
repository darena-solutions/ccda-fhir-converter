using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <inheritdoc />
    public class MedicationRequestConverter : IResourceConverter
    {
        private readonly string _patientId;
        private readonly string _medicationId;

        /// <summary>
        /// Initializes a new instance of the <see cref="MedicationRequestConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        /// <param name="medicationId">The id of the medication that is being referenced</param>
        public MedicationRequestConverter(string patientId, string medicationId)
        {
            _patientId = patientId;
            _medicationId = medicationId;
        }

        /// <inheritdoc />
        public void AddToBundle(
            Bundle bundle,
            IEnumerable<XElement> elements,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            var element = elements.FirstOrDefault();
            if (element == null)
                return;

            var id = Guid.NewGuid().ToString();
            var medicationRequest = new MedicationRequest
            {
                Id = id,
                Meta = new Meta(),
                Intent = MedicationRequest.medicationRequestIntent.Order,
                Subject = new ResourceReference($"urn:uuid:{_patientId}"),
                Medication = new ResourceReference($"urn:uuid:{_medicationId}")
            };

            medicationRequest.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-medicationrequest"));

            var identifierElements = element.Elements(Defaults.DefaultNs + "id");
            foreach (var identifierElement in identifierElements)
            {
                medicationRequest.Identifier.Add(identifierElement.ToIdentifier());
            }

            var statusCodeValue = element
                .Element(Defaults.DefaultNs + "statusCode")?
                .Attribute("code")?
                .Value;

            if (string.IsNullOrWhiteSpace(statusCodeValue))
                throw new InvalidOperationException($"No medication request found in: {element}");

            var statusCode = EnumUtility.ParseLiteral<MedicationRequest.medicationrequestStatus>(statusCodeValue, true);
            if (statusCode == null)
                throw new InvalidOperationException($"Could not determine medication request status from '{statusCodeValue}'");

            medicationRequest.Status = statusCode;

            var authorElement = element.Element(Defaults.DefaultNs + "author");
            if (authorElement == null)
                throw new InvalidOperationException($"Could not find a medication author in: {element}");

            var assignedAuthorElement = authorElement.Element(Defaults.DefaultNs + "assignedAuthor");
            if (assignedAuthorElement == null)
                throw new InvalidOperationException($"Could not find an assigned author in: {authorElement}");

            var timeValue = authorElement
                .Element(Defaults.DefaultNs + "time")?
                .Attribute("value")?
                .Value;

            if (string.IsNullOrWhiteSpace(timeValue))
                throw new InvalidOperationException($"Could not find an authored time in: {authorElement}");

            medicationRequest.AuthoredOnElement = new FhirDateTime(timeValue.ParseCCdaDateTimeOffset());

            var practitionerConverter = new PractitionerConverter();
            practitionerConverter.AddToBundle(
                bundle,
                new List<XElement> { assignedAuthorElement },
                namespaceManager,
                cacheManager);

            medicationRequest.Requester = new ResourceReference($"urn:uuid:{practitionerConverter.PractitionerId}");

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = medicationRequest
            });
        }
    }
}
