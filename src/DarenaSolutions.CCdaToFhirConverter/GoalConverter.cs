using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <inheritdoc />
    public class GoalConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoalConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public GoalConverter(string patientId)
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
                var goal = new Goal
                {
                    Id = id,
                    Meta = new Meta()
                };

                goal.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-goal"));

                var identifierElements = element.Elements(Defaults.DefaultNs + "id");
                foreach (var identifierElement in identifierElements)
                {
                    goal.Identifier.Add(identifierElement.ToIdentifier());
                }

                var statusCodeValue = element
                    .Element(Defaults.DefaultNs + "statusCode")?
                    .Attribute("code")?
                    .Value;

                if (!string.IsNullOrWhiteSpace(statusCodeValue) && statusCodeValue.ToLowerInvariant().Equals("active"))
                {
                    goal.LifecycleStatus = Goal.GoalLifecycleStatus.Active;
                }
                else
                {
                    goal.LifecycleStatus = Goal.GoalLifecycleStatus.Proposed;
                }

                var valueElementText = element.Element(Defaults.DefaultNs + "value")?.Value;
                if (string.IsNullOrWhiteSpace(valueElementText))
                    throw new InvalidOperationException($"No value element was found in: {element}");

                CodeableConcept valueCodeableConcept = new CodeableConcept
                {
                    Text = valueElementText
                };
                goal.Description = valueCodeableConcept;
                goal.Target.Add(new Goal.TargetComponent { Measure = valueCodeableConcept });
                goal.Subject = new ResourceReference($"urn:uuid:{_patientId}");

                var effectiveTimeElement = element.Element(Defaults.DefaultNs + "effectiveTime");
                goal.StatusDateElement = effectiveTimeElement?.ToFhirDate();

                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = goal
                });
            }
        }
    }
}
