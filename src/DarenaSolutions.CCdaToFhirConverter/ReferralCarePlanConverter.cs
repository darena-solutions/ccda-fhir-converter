using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <inheritdoc />
    public class ReferralCarePlanConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferralCarePlanConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public ReferralCarePlanConverter(string patientId)
        {
            _patientId = patientId;
        }

        /// <inheritdoc />
        public void AddToBundle(
            Bundle bundle,
            IEnumerable<XElement> elements,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            foreach (var element in elements)
            {
                var id = Guid.NewGuid().ToString();
                var referral = new CarePlan
                {
                    Id = id,
                    Status = RequestStatus.Active,
                    Intent = CarePlan.CarePlanIntent.Plan,
                    Subject = new ResourceReference($"urn:uuid:{_patientId}")
                };

                var textEl = element.Element(Defaults.DefaultNs + "text")?.GetContentsAsString();
                if (string.IsNullOrWhiteSpace(textEl))
                    throw new InvalidOperationException($"Could not find any text for the referral in: {element}");

                referral.Text = new Narrative
                {
                    Div = textEl,
                    Status = Narrative.NarrativeStatus.Generated
                };

                var codeableConcept = element
                    .FindCodeElementWithTranslation()?
                    .ToCodeableConcept();

                if (codeableConcept != null)
                    referral.Category.Add(codeableConcept);

                // Commit Resource
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = referral
                });
            }
        }
    }
}
