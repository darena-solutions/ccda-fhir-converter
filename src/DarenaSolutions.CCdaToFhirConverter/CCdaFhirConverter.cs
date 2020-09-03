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

            var representedOrganizationXPath = "n1:ClinicalDocument/n1:author/n1:assignedAuthor/n1:representedOrganization";
            var representedOrganizationElements = cCda.XPathSelectElements(representedOrganizationXPath, _namespaceManager);
            var representedOrganizationConverter = new RepresentedOrganizationConverter();
            representedOrganizationConverter.AddToBundle(bundle, representedOrganizationElements, _namespaceManager, cacheManager);

            if (string.IsNullOrWhiteSpace(representedOrganizationConverter.OrganizationId))
                throw new InvalidOperationException("A represented organization could not be found");

            var patientXPath = "n1:ClinicalDocument/n1:recordTarget/n1:patientRole";
            var patientElements = cCda.XPathSelectElements(patientXPath, _namespaceManager);
            var patientConverter = new PatientConverter(representedOrganizationConverter.OrganizationId);
            patientConverter.AddToBundle(bundle, patientElements, _namespaceManager, cacheManager);

            if (string.IsNullOrWhiteSpace(patientConverter.PatientId))
                throw new InvalidOperationException("A patient could not be found");

            var allergyIntoleranceXPath =
                "//n1:component/n1:section/n1:templateId[@root='2.16.840.1.113883.10.20.22.2.6.1']/.." +
                "/n1:entry/n1:act[n1:entryRelationship/n1:observation/n1:templateId[@root='2.16.840.1.113883.10.20.22.4.7']]";

            var allergyIntoleranceElements = cCda.XPathSelectElements(allergyIntoleranceXPath, _namespaceManager);
            var allergyIntoleranceConverter = new AllergyIntoleranceConverter(patientConverter.PatientId);
            allergyIntoleranceConverter.AddToBundle(bundle, allergyIntoleranceElements, _namespaceManager, cacheManager);

            var medicationXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.1.1']/../n1:entry";
            var medicationElements = cCda.XPathSelectElements(medicationXPath, _namespaceManager);
            var medicationConverter = new MedicationConverter(patientConverter.PatientId);
            medicationConverter.AddToBundle(bundle, medicationElements, _namespaceManager, cacheManager);

            var encounterDiagnosisXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.22.1']/.." +
                "/n1:entry/n1:encounter/n1:entryRelationship/n1:act/n1:entryRelationship/n1:observation";
            var encounterDiagnosisElements = cCda.XPathSelectElements(encounterDiagnosisXPath, _namespaceManager);
            var encounterDiagnosisConverter = new ConditionConverter(patientConverter.PatientId, ConditionCategory.EncounterDiagnosis);
            encounterDiagnosisConverter.AddToBundle(bundle, encounterDiagnosisElements, _namespaceManager, cacheManager);

            var healthConcernXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.58']/../n1:entry/n1:observation";
            var healthConcernElements = cCda.XPathSelectElements(healthConcernXPath, _namespaceManager);
            var healthConcernConverter = new ConditionConverter(patientConverter.PatientId, ConditionCategory.HealthConcern);
            healthConcernConverter.AddToBundle(bundle, healthConcernElements, _namespaceManager, cacheManager);

            var problemXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.5.1']/../n1:entry/n1:act/n1:entryRelationship/n1:observation";
            var problemElements = cCda.XPathSelectElements(problemXPath, _namespaceManager);
            var problemConverter = new ConditionConverter(patientConverter.PatientId, ConditionCategory.ProblemList);
            problemConverter.AddToBundle(bundle, problemElements, _namespaceManager, cacheManager);

            var immunizationXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.2.1']/../n1:entry/n1:substanceAdministration";
            var immunizationElements = cCda.XPathSelectElements(immunizationXPath, _namespaceManager);
            var immunizationConverter = new ImmunizationConverter(patientConverter.PatientId);
            immunizationConverter.AddToBundle(bundle, immunizationElements, _namespaceManager, cacheManager);

            // Vital Signs - 1 entry - 1 organizer - 1 to many Components by Template Id
            var vitalSignsXPath =
                "//n1:component/n1:section/n1:templateId[@root='2.16.840.1.113883.10.20.22.2.4.1']/.." +
                "/n1:entry/n1:organizer/n1:component/n1:observation/n1:templateId[@root='2.16.840.1.113883.10.20.22.4.27']/..";

            var vitalSignsElements = cCda.XPathSelectElements(vitalSignsXPath, _namespaceManager);
            var vitalSignsConverter = new VitalSignObservationConverter(patientConverter.PatientId);
            vitalSignsConverter.AddToBundle(bundle, vitalSignsElements, _namespaceManager, cacheManager);

            var procedureXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.7.1']/../n1:entry/n1:procedure";
            var proceduresElements = cCda.XPathSelectElements(procedureXPath, _namespaceManager);
            var procedureConverter = new ProcedureConverter(patientConverter.PatientId);
            procedureConverter.AddToBundle(bundle, proceduresElements, _namespaceManager, cacheManager);

            var deviceXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.23']/../n1:entry/n1:procedure";
            var devicesElements = cCda.XPathSelectElements(deviceXPath, _namespaceManager);
            var deviceConverter = new DeviceConverter(patientConverter.PatientId);
            deviceConverter.AddToBundle(bundle, devicesElements, _namespaceManager, cacheManager);

            var goalXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.60']/../n1:entry/n1:observation";
            var goalsElements = cCda.XPathSelectElements(goalXPath, _namespaceManager);
            var goalConverter = new GoalConverter(patientConverter.PatientId);
            goalConverter.AddToBundle(bundle, goalsElements, _namespaceManager, cacheManager);

            // Social History - Smoking Status
            var smokingStatusXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.17']/../" +
                "/n1:entry/n1:observation/n1:templateId[@root='2.16.840.1.113883.10.20.22.4.78']/..";
            var smokingStatusElements = cCda.XPathSelectElements(smokingStatusXPath, _namespaceManager);
            var smokingStatusConverter = new SmokingStatusObservationConverter(patientConverter.PatientId);
            smokingStatusConverter.AddToBundle(bundle, smokingStatusElements, _namespaceManager, cacheManager);

            var labOrderXPath = "//n1:templateId[@root='2.16.840.1.113883.10.20.22.2.10']/../n1:entry/n1:observation/n1:templateId[@root='2.16.840.1.113883.10.20.22.4.44']/..";
            var labOrderElements = cCda.XPathSelectElements(labOrderXPath, _namespaceManager);
            var labOrderConverter = new LabOrderServiceRequestConverter(patientConverter.PatientId);
            labOrderConverter.AddToBundle(bundle, labOrderElements, _namespaceManager, cacheManager);

            return bundle;
        }
    }
}
