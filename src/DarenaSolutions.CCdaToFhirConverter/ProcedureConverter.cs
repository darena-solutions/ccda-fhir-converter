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
                    Meta = new Meta(),
                    Subject = new ResourceReference($"urn:uuid:{_patientId}"),
                    Status = EventStatus.Completed
                };

                procedure.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-procedure"));

                var identifierElements = element.Elements(Defaults.DefaultNs + "id");
                foreach (var identifierElement in identifierElements)
                {
                    procedure.Identifier.Add(identifierElement.ToIdentifier());
                }

                procedure.Code = element
                    .FindCodeElementWithTranslation()?
                    .ToCodeableConcept();

                if (procedure.Code == null)
                    throw new InvalidOperationException($"No code element was found in: {element}");

                procedure.Performed = element
                    .Element(Defaults.DefaultNs + "effectiveTime")?
                    .ToDateTimeElement();

                if (procedure.Performed == null)
                    throw new InvalidOperationException($"A procedure occurrence date time could not be found in: {element}");

                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{id}",
                    Resource = procedure
                });

                var participantRoleEl = element
                    .Element(Defaults.DefaultNs + "participant")?
                    .Element(Defaults.DefaultNs + "participantRole");

                if (participantRoleEl != null)
                {
                    var deviceConverter = new DeviceConverter(_patientId);
                    deviceConverter.AddToBundle(
                        bundle,
                        new List<XElement> { participantRoleEl },
                        namespaceManager,
                        cacheManager);
                }
            }
        }
    }
}
