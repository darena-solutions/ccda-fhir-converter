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
    public class ProcedureConverter : IResourceConverter
    {
        private readonly string _patientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcedureConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public ProcedureConverter(string patientId)
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
                var procedure = new Procedure
                {
                    Id = id,
                    Meta = new Meta()
                };

                procedure.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-procedure"));

                var identifierElements = element.Elements(Defaults.DefaultNs + "id");
                foreach (var identifierElement in identifierElements)
                {
                    procedure.Identifier.Add(identifierElement.ToIdentifier());
                }

                var codeValue = element
                    .Element(Defaults.DefaultNs + "statusCode")?
                    .Attribute("code")?
                    .Value;

                if (!string.IsNullOrWhiteSpace(codeValue) && codeValue.ToLowerInvariant().Equals("completed"))
                {
                    procedure.Status = EventStatus.Completed;
                }
                else
                {
                    procedure.Status = EventStatus.Unknown;
                }

                var codeableConcept = element
                    .FindCodeElementWithTranslation()?
                    .ToCodeableConcept();

                if (codeableConcept == null)
                    throw new InvalidOperationException($"No code element was found in: {element}");

                procedure.Code = codeableConcept;
                procedure.Subject = new ResourceReference($"urn:uuid:{_patientId}");

                var effectiveTimeElement = element.Element(Defaults.DefaultNs + "effectiveTime");
                var effectiveTime = effectiveTimeElement?.ToDateTimeElement();

                if (effectiveTime == null)
                    throw new InvalidOperationException($"A procedure occurrence date time could not be found in: {element}");

                procedure.Performed = effectiveTime;

                foreach (var targetSiteElement in element.Elements(Defaults.DefaultNs + "targetSiteCode"))
                {
                    procedure.BodySite.Add(targetSiteElement.ToCodeableConcept());
                }

                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = procedure
                });
            }
        }
    }
}
