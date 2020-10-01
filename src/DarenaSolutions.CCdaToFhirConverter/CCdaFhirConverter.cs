using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Enums;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// A converter that takes a CCDA document and converts it to a FHIR bundle representation
    /// </summary>
    public class CCdaFhirConverter
    {
        private readonly XmlNamespaceManager _namespaceManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CCdaFhirConverter"/> class
        /// </summary>
        public CCdaFhirConverter()
        {
            _namespaceManager = new XmlNamespaceManager(new NameTable());
            _namespaceManager.AddNamespace("n1", Defaults.DefaultNs.NamespaceName);
            _namespaceManager.AddNamespace("sdtc", Defaults.SdtcNs.NamespaceName);
        }

        /// <summary>
        /// Converts a given CCDA document into its FHIR bundle representation
        /// </summary>
        /// <param name="cCda">The CCDA document</param>
        /// <returns>The FHIR bundle representation</returns>
        public Bundle ConvertCCda(XDocument cCda)
        {
            var bundle = new Bundle
            {
                Type = Bundle.BundleType.Collection,
                Timestamp = DateTimeOffset.UtcNow
            };

            var cacheManager = new ConvertedCacheManager();

            T AddConversionToBundle<T>(string xPath, Func<T> factory)
                where T : IResourceConverter
            {
                var elements = cCda.XPathSelectElements(xPath, _namespaceManager);
                var converter = factory();
                converter.AddToBundle(bundle, elements, _namespaceManager, cacheManager);

                return converter;
            }

            var representedOrganizationConverter = AddConversionToBundle(
                "n1:ClinicalDocument/n1:author/n1:assignedAuthor/n1:representedOrganization",
                () => new RepresentedOrganizationConverter());

            if (string.IsNullOrWhiteSpace(representedOrganizationConverter.OrganizationId))
                throw new InvalidOperationException("A represented organization could not be found");

            var patientConverter = AddConversionToBundle(
                "n1:ClinicalDocument/n1:recordTarget/n1:patientRole",
                () => new PatientConverter(representedOrganizationConverter.OrganizationId));

            if (string.IsNullOrWhiteSpace(patientConverter.PatientId))
                throw new InvalidOperationException("A patient could not be found");

            var allergyIntoleranceXPath =
                "//n1:component/n1:section/n1:templateId[@root='2.16.840.1.113883.10.20.22.2.6.1']/.." +
                "/n1:entry/n1:act[n1:entryRelationship/n1:observation/n1:templateId[@root='2.16.840.1.113883.10.20.22.4.7']]";

            AddConversionToBundle(allergyIntoleranceXPath, () => new AllergyIntoleranceConverter(patientConverter.PatientId));

            var medicationXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.1.1']/../n1:entry";
            AddConversionToBundle(medicationXPath, () => new MedicationConverter(patientConverter.PatientId));

            var encounterDiagnosisXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.22.1']/.." +
                "/n1:entry/n1:encounter/n1:entryRelationship/n1:act/n1:entryRelationship/n1:observation";

            AddConversionToBundle(encounterDiagnosisXPath, () => new ConditionConverter(patientConverter.PatientId, ConditionCategory.EncounterDiagnosis));

            var healthConcernXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.58']/../n1:entry/n1:observation";
            AddConversionToBundle(healthConcernXPath, () => new ConditionConverter(patientConverter.PatientId, ConditionCategory.HealthConcern));

            var problemXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.5.1']/../n1:entry/n1:act/n1:entryRelationship/n1:observation";
            AddConversionToBundle(problemXPath, () => new ConditionConverter(patientConverter.PatientId, ConditionCategory.ProblemList));

            var immunizationXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.2.1']/../n1:entry/n1:substanceAdministration";
            AddConversionToBundle(immunizationXPath, () => new ImmunizationConverter(patientConverter.PatientId));

            // Vital Signs - 1 entry - 1 organizer - 1 to many Components by Template Id
            var vitalSignsXPath =
                "//n1:component/n1:section/n1:templateId[@root='2.16.840.1.113883.10.20.22.2.4.1']/.." +
                "/n1:entry/n1:organizer/n1:component/n1:observation/n1:templateId[@root='2.16.840.1.113883.10.20.22.4.27']/..";

            AddConversionToBundle(vitalSignsXPath, () => new VitalSignObservationConverter(patientConverter.PatientId));

            var procedureXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.7.1']/../n1:entry/n1:procedure";
            AddConversionToBundle(procedureXPath, () => new ProcedureConverter(patientConverter.PatientId));

            var deviceXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.23']/../n1:entry/n1:procedure";
            AddConversionToBundle(deviceXPath, () => new DeviceConverter(patientConverter.PatientId));

            var goalXPath = "//n1:section/n1:code[@code='61146-7']/../n1:entry/n1:observation";
            AddConversionToBundle(goalXPath, () => new GoalConverter(patientConverter.PatientId));

            // Social History - Smoking Status
            var smokingStatusXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.17']/../" +
                "/n1:entry/n1:observation/n1:templateId[@root='2.16.840.1.113883.10.20.22.4.78']/..";

            AddConversionToBundle(smokingStatusXPath, () => new SmokingStatusObservationConverter(patientConverter.PatientId));

            var labOrderXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.10']/../n1:entry/n1:observation/n1:templateId[@root='2.16.840.1.113883.10.20.22.4.44']/..";
            AddConversionToBundle(labOrderXPath, () => new LabOrderServiceRequestConverter(patientConverter.PatientId));

            var referralXPath = "//n1:section/n1:code[@code='42349-1']/..";
            AddConversionToBundle(referralXPath, () => new ReferralCarePlanConverter(patientConverter.PatientId));

            var resultXPath = "//n1:section/n1:code[@code='30954-2']/../n1:entry/n1:organizer/n1:component/n1:observation";
            AddConversionToBundle(resultXPath, () => new ResultObservationConverter(patientConverter.PatientId));

            var consultationNotesXPath = "//n1:section/n1:code[@code='11488-4']/../n1:entry/n1:act";
            AddConversionToBundle(consultationNotesXPath, () => new ClinicalImpressionConverter(patientConverter.PatientId));

            var functionalStatusXPath = "//n1:section/n1:code[@code='47420-5']/../n1:entry/n1:observation";
            AddConversionToBundle(functionalStatusXPath, () => new StatusObservationConverter(patientConverter.PatientId));

            var mentalStatusXPath = "//n1:section/n1:code[@code='10190-7']/../n1:entry/n1:observation";
            AddConversionToBundle(mentalStatusXPath, () => new StatusObservationConverter(patientConverter.PatientId));

            return bundle;
        }
    }
}
