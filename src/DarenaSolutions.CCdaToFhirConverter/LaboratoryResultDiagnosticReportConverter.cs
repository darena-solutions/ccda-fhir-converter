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
    /// Converter that converts various elements in the CCDA into FHIR diagnostic report resources. These elements are specifically
    /// for laboratory results
    /// </summary>
    public class LaboratoryResultDiagnosticReportConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LaboratoryResultDiagnosticReportConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public LaboratoryResultDiagnosticReportConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='30954-2']/../n1:entry/n1:organizer";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var id = Guid.NewGuid().ToString();
            var report = new DiagnosticReport
            {
                Id = id,
                Meta = new Meta(),
                Status = DiagnosticReport.DiagnosticReportStatus.Final,
                Subject = new ResourceReference($"urn:uuid:{PatientId}")
            };

            report.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-diagnosticreport-lab"));
            var cachedResource = element.SetIdentifiers(context, report);
            if (cachedResource != null)
                return cachedResource;

            report.Category.Add(new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/v2-0074",
                "LAB",
                null,
                null));

            try
            {
                report.Code = element
                    .FindCodeElementWithTranslation()?
                    .ToCodeableConcept("DiagnosticReport.code");

                if (report.Code == null)
                    throw new RequiredValueNotFoundException(element, "code", "DiagnosticReport.code");
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            try
            {
                report.Effective = element
                    .Element(Namespaces.DefaultNs + "effectiveTime")?
                    .ToDateTimeElement();

                if (report.Effective == null)
                    throw new RequiredValueNotFoundException(element, "effectiveTime", "DiagnosticReport.effective");

                if (report.Effective is FhirDateTime dateTime)
                    report.Issued = dateTime.ToDateTimeOffset(TimeSpan.Zero);
                else if (report.Effective is Period period)
                    report.Issued = period.StartElement.ToDateTimeOffset(TimeSpan.Zero);
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            var representedOrgXPath = "n1:component/n1:observation/n1:author/n1:assignedAuthor/n1:representedOrganization";
            var representedOrgEls = element.XPathSelectElements(representedOrgXPath, context.NamespaceManager);

            var addedIds = new HashSet<string>();
            foreach (var orgEl in representedOrgEls)
            {
                try
                {
                    var organizationConverter = new OrganizationConverter();
                    var organization = organizationConverter.AddToBundle(orgEl, context);
                    if (addedIds.Contains(organization.Id))
                        continue;

                    report.Performer.Add(new ResourceReference($"urn:uuid:{organization.Id}"));
                    addedIds.Add(organization.Id);
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            context.Bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = report
            });

            return report;
        }
    }
}
