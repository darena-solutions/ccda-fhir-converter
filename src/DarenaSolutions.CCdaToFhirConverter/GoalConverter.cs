using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Converter that converts various elements in the CCDA into goal FHIR resources
    /// </summary>
    public class GoalConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GoalConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public GoalConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='61146-7']/../n1:entry/n1:observation";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            var id = Guid.NewGuid().ToString();
            var goal = new Goal
            {
                Id = id,
                Meta = new Meta(),
                Subject = new ResourceReference($"urn:uuid:{PatientId}"),
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

            return goal;
        }
    }
}
