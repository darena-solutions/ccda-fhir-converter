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
            var cacheManager = new ConvertedCacheManager();
            var bundle = new Bundle
            {
                Type = Bundle.BundleType.Collection,
                Timestamp = DateTimeOffset.UtcNow
            };

            var primaryOrg = ExecuteOrganizationConversion(cCda, bundle, cacheManager);
            var primaryPatient = ExecutePatientConversion(cCda, bundle, cacheManager, primaryOrg?.Id);

            if (primaryPatient == null)
                throw new InvalidOperationException("A patient could not be found");

            ////ExecuteAllergyIntoleranceConversion(cCda, bundle, cacheManager, primaryPatient.Id);
            ////ExecuteMedicationConversion(cCda, bundle, cacheManager, primaryPatient.Id);
            ////ExecuteEncounterDiagnosisConversion(cCda, bundle, cacheManager, primaryPatient.Id);

            ////var healthConcernXPath = "//n1:section/n1:code[@code='75310-3']/../n1:entry/n1:act";
            ////AddConversionToBundle(healthConcernXPath, () => new BaseConditionConverter(patientConverter.PatientId, ConditionCategory.HealthConcern));

            ////var healthConcernObservationXPath = "//n1:section/n1:code[@code='75310-3']/../n1:entry/n1:observation";
            ////AddConversionToBundle(healthConcernObservationXPath, () => new StatusObservationConverter(patientConverter.PatientId));

            ////var problemXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.5.1']/../n1:entry/n1:act/n1:entryRelationship/n1:observation";
            ////AddConversionToBundle(problemXPath, () => new BaseConditionConverter(patientConverter.PatientId, ConditionCategory.ProblemList));

            ////var immunizationXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.2.1']/../n1:entry/n1:substanceAdministration";
            ////AddConversionToBundle(immunizationXPath, () => new ImmunizationConverter(patientConverter.PatientId));

            // Vital Signs
            ////var vitalSignsXPath = "//n1:section/n1:code[@code='8716-3']/../n1:entry/n1:organizer/n1:component/n1:observation";
            ////AddConversionToBundle(vitalSignsXPath, () => new VitalSignObservationConverter(patientConverter.PatientId));

            ////var procedureXPath = "//n1:section/n1:code[@code='47519-4']/../n1:entry/n1:procedure";
            ////AddConversionToBundle(procedureXPath, () => new ProcedureConverter(patientConverter.PatientId));

            var deviceXPath = "//n1:section/n1:code[@code='46264-8']/../n1:entry/n1:procedure/n1:participant/n1:participantRole";
            AddConversionToBundle(deviceXPath, () => new DeviceConverter(patientConverter.PatientId));

            ////var goalXPath = "//n1:section/n1:code[@code='61146-7']/../n1:entry/n1:observation";
            ////AddConversionToBundle(goalXPath, () => new GoalConverter(patientConverter.PatientId));

            // Social History - Smoking Status
            ////var smokingStatusXPath = "//n1:section/n1:code[@code='29762-2']/../n1:entry/n1:observation/n1:code[@code='72166-2']/..";
            ////AddConversionToBundle(smokingStatusXPath, () => new SmokingStatusObservationConverter(patientConverter.PatientId));

            ////var referralXPath = "//n1:section/n1:code[@code='42349-1']/..";
            ////AddConversionToBundle(referralXPath, () => new TextSpecificCarePlanConverter(patientConverter.PatientId));

            ////var assessmentXPath = "//n1:section/n1:code[@code='51848-0']/..";
            ////AddConversionToBundle(assessmentXPath, () => new TextSpecificCarePlanConverter(patientConverter.PatientId));

            ////var resultXPath = "//n1:section/n1:code[@code='30954-2']/../n1:entry/n1:organizer/n1:component/n1:observation";
            ////AddConversionToBundle(resultXPath, () => new ResultObservationConverter(patientConverter.PatientId));

            ////var consultationNotesXPath = "//n1:section/n1:code[@code='11488-4']/../n1:entry/n1:act";
            ////AddConversionToBundle(consultationNotesXPath, () => new ClinicalImpressionConverter(patientConverter.PatientId));

            ////var functionalStatusXPath = "//n1:section/n1:code[@code='47420-5']/../n1:entry/n1:observation";
            ////AddConversionToBundle(functionalStatusXPath, () => new StatusObservationConverter(patientConverter.PatientId));

            ////var mentalStatusXPath = "//n1:section/n1:code[@code='10190-7']/../n1:entry/n1:observation";
            ////AddConversionToBundle(mentalStatusXPath, () => new StatusObservationConverter(patientConverter.PatientId));

            return bundle;
        }

        protected Organization ExecuteOrganizationConversion(XDocument cCda, Bundle bundle, ConvertedCacheManager cacheManager)
        {
            var xPath = "n1:ClinicalDocument/n1:author/n1:assignedAuthor/n1:representedOrganization";
            var converter = ExecuteConversionSingle(
                cCda,
                bundle,
                cacheManager,
                xPath,
                () => new OrganizationConverter());

            return (Organization)converter.Resource;
        }

        protected Patient ExecutePatientConversion(
            XDocument cCda,
            Bundle bundle,
            ConvertedCacheManager cacheManager,
            string managingOrganizationId)
        {
            var xPath = "n1:ClinicalDocument/n1:recordTarget/n1:patientRole";
            var converter = ExecuteConversionSingle(
                cCda,
                bundle,
                cacheManager,
                xPath,
                () => new PatientConverter(managingOrganizationId));

            return (Patient)converter.Resource;
        }

        protected void ExecuteAllergyIntoleranceConversion(
            XDocument cCda,
            Bundle bundle,
            ConvertedCacheManager cacheManager,
            string patientId)
        {
            var xPath = "//n1:component/n1:section/n1:code[@code='48765-2']/../n1:entry/n1:act/n1:entryRelationship/n1:observation";
            ExecuteConversionMulti(
                cCda,
                bundle,
                cacheManager,
                xPath,
                () => new AllergyIntoleranceConverter(patientId));
        }

        protected void ExecuteMedicationConversion(
            XDocument cCda,
            Bundle bundle,
            ConvertedCacheManager cacheManager,
            string patientId)
        {
            var xPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.1.1']/../n1:entry";
            ExecuteConversionMulti(
                cCda,
                bundle,
                cacheManager,
                xPath,
                () => new MedicationConverter(patientId));
        }

        protected void ExecuteEncounterDiagnosisConversion(
            XDocument cCda,
            Bundle bundle,
            ConvertedCacheManager cacheManager,
            string patientId)
        {
            var xPath =
                "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.22.1']/.." +
                "/n1:entry/n1:encounter/n1:entryRelationship/n1:act/n1:entryRelationship/n1:observation";

            ExecuteConversionMulti(
                cCda,
                bundle,
                cacheManager,
                xPath,
                () => new BaseConditionConverter(patientId, ConditionCategory.EncounterDiagnosis));
        }

        private T ExecuteConversionSingle<T>(
            XDocument cCda,
            Bundle bundle,
            ConvertedCacheManager cacheManager,
            string xPath,
            Func<T> factory)
            where T : IResourceConverter
        {
            var element = cCda.XPathSelectElement(xPath, _namespaceManager);
            if (element == null)
                return default;

            var converter = factory();
            converter.AddToBundle(
                bundle,
                element,
                _namespaceManager,
                cacheManager);

            return converter;
        }

        private void ExecuteConversionMulti<T>(
            XDocument cCda,
            Bundle bundle,
            ConvertedCacheManager cacheManager,
            string xPath,
            Func<T> factory)
            where T : IResourceConverter
        {
            var elements = cCda.XPathSelectElements(xPath, _namespaceManager);
            foreach (var element in elements)
            {
                var converter = factory();
                converter.AddToBundle(
                    bundle,
                    element,
                    _namespaceManager,
                    cacheManager);
            }
        }
    }
}
