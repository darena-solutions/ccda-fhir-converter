using System;
using System.Collections.Generic;
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
    /// Converter that converts various elements in the CCDA into immunization FHIR resources
    /// </summary>
    public class ImmunizationConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmunizationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public ImmunizationConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='11369-6']/../n1:entry/n1:substanceAdministration";
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
            var immunization = new Immunization
            {
                Id = id,
                Meta = new Meta(),
                Patient = new ResourceReference($"urn:uuid:{PatientId}"),
                PrimarySource = true
            };

            immunization.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-immunization"));

            var identifierElements = element.Elements(Defaults.DefaultNs + "id");
            foreach (var identifierElement in identifierElements)
            {
                immunization.Identifier.Add(identifierElement.ToIdentifier());
            }

            var status = element
                .Element(Defaults.DefaultNs + "statusCode")?
                .Attribute("code")?
                .Value;

            immunization.Status = status == "completed"
                ? Immunization.ImmunizationStatusCodes.Completed
                : Immunization.ImmunizationStatusCodes.NotDone;

            immunization.Occurrence = element
                .Element(Defaults.DefaultNs + "effectiveTime")?
                .ToFhirDateTime();

            if (immunization.Occurrence == null)
                throw new RequiredValueNotFoundException(element, "effectiveTime");

            var manufacturedMaterialXPath = "n1:consumable/n1:manufacturedProduct/n1:manufacturedMaterial";
            var manufacturedMaterialEl = element.XPathSelectElement(manufacturedMaterialXPath, namespaceManager);
            if (manufacturedMaterialEl != null)
            {
                immunization.VaccineCode = manufacturedMaterialEl
                    .Element(Defaults.DefaultNs + "code")?
                    .ToCodeableConcept();

                immunization.LotNumber = manufacturedMaterialEl
                    .Element(Defaults.DefaultNs + "lotNumberText")?
                    .GetFirstTextNode();
            }

            if (immunization.VaccineCode == null)
                throw new RequiredValueNotFoundException(element, "consumable/manufacturedProduct/manufacturedMaterial/code");

            var statusReasonCodeXPath = "n1:entryRelationship/n1:observation/n1:code";
            immunization.StatusReason = element
                .XPathSelectElement(statusReasonCodeXPath, namespaceManager)?
                .ToCodeableConcept();

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = immunization
            });

            return immunization;
        }
    }
}
