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

                CodeableConcept clinicalStatus = null;
                var statusCodeElement = element.Element(Defaults.DefaultNs + "statusCode");
                if (statusCodeElement != null)
                {
                    clinicalStatus = statusCodeElement.ToCodeableConcept();
                    EventStatus? eventStatus = EventStatus.Unknown;

                    // Right now procedure has completed status, if required we can extend it
                    if (clinicalStatus != null)
                    {
                        switch (clinicalStatus.Coding[0].Code)
                        {
                            case "completed":
                                eventStatus = EventStatus.Completed;
                                break;
                        }

                        procedure.Status = eventStatus;
                    }
                }

                var codeElement = element.Element(Defaults.DefaultNs + "code");
                if (codeElement != null)
                {
                    var translationElement = codeElement.Element(Defaults.DefaultNs + "translation");
                    if (translationElement != null)
                    {
                        codeElement = translationElement;
                    }
                }
                else
                {
                    throw new InvalidOperationException($"No code element was found in: {codeElement}");
                }

                procedure.Code = codeElement.ToCodeableConcept();
                procedure.Subject = new ResourceReference($"urn:uuid:{_patientId}");

                var effectiveTimeElement = element.Element(Defaults.DefaultNs + "effectiveTime");
                var effectiveTime = effectiveTimeElement?.ToDateTimeElement();

                if (effectiveTime == null)
                    throw new InvalidOperationException($"A procedure occurrence date time could not be found in: {element}");

                procedure.Performed = effectiveTime;

                var targetSiteElements = element.Elements(Defaults.DefaultNs + "targetSiteCode");
                if (targetSiteElements != null)
                {
                    List<CodeableConcept> targetSiteList = new List<CodeableConcept>();
                    foreach (var targetSiteElement in targetSiteElements)
                    {
                        targetSiteList.Add(targetSiteElement.ToCodeableConcept());
                    }

                    if (targetSiteList.Count > 0)
                    {
                        procedure.BodySite = targetSiteList;
                    }
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
