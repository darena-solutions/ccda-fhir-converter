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
                    Meta = new Meta(),
                    Subject = new ResourceReference($"urn:uuid:{_patientId}"),
                    LifecycleStatus = Goal.GoalLifecycleStatus.Active
                };

                goal.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-goal"));

                var identifierElements = element.Elements(Defaults.DefaultNs + "id");
                foreach (var identifierElement in identifierElements)
                {
                    goal.Identifier.Add(identifierElement.ToIdentifier());
                }

                var effectiveTime = element
                    .Element(Defaults.DefaultNs + "effectiveTime")?
                    .ToDateTimeElement();

                if (effectiveTime is Period period)
                {
                    goal.Start = period.StartElement.ToDate();

                    goal.Target.Add(new Goal.TargetComponent
                    {
                        Due = period.EndElement.ToDate()
                    });
                }
                else if (effectiveTime is FhirDateTime dateTime)
                {
                    goal.Start = dateTime.ToDate();
                }

                var descriptionEl = element.Element(Defaults.DefaultNs + "value");
                var description = descriptionEl?.ToFhirElementBasedOnType();

                if (description == null)
                    throw new InvalidOperationException($"The goal description could not be found in: {element}");

                if (!(description is FhirString descriptionStr))
                {
                    throw new InvalidOperationException(
                        $"The goal description is expected to be a plain text value. However, an unrecognized " +
                        $"value was found in: {description}");
                }

                goal.Description = new CodeableConcept
                {
                    Text = descriptionStr.Value
                };

                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = goal
                });
            }
        }
    }
}
