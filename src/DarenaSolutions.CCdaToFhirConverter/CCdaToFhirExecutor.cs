using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// A converter that takes a CCDA document and converts it to a FHIR bundle representation
    /// </summary>
    public class CCdaToFhirExecutor
    {
        private readonly ISingleResourceConverter _organizationConverter;
        private readonly ISingleResourceConverter _patientConverter;
        private readonly XmlNamespaceManager _namespaceManager;
        private readonly HashSet<Type> _converterTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="CCdaToFhirExecutor"/> class
        /// </summary>
        /// <param name="organizationConverter">Optionally specify an implementation of <see cref="ISingleResourceConverter"/>
        /// for the primary organization. If one is not provided, <see cref="OrganizationConverter"/> will be used by default</param>
        /// <param name="patientConverter">Optionally specify an implementation of <see cref="ISingleResourceConverter"/>
        /// for the primary patient. If one is not provided, <see cref="PatientConverter"/> will be used by default</param>
        /// <param name="addDefaultConverters">Optionally specify if the default converters of this library should be added
        /// to the converter collection. (Default: <c>true</c>)</param>
        public CCdaToFhirExecutor(
            ISingleResourceConverter organizationConverter = null,
            ISingleResourceConverter patientConverter = null,
            bool addDefaultConverters = true)
        {
            _organizationConverter = organizationConverter ?? new OrganizationConverter();
            _patientConverter = patientConverter ?? new PatientConverter();

            _converterTypes = new HashSet<Type>();

            _namespaceManager = new XmlNamespaceManager(new NameTable());
            _namespaceManager.AddNamespace("n1", Defaults.DefaultNs.NamespaceName);
            _namespaceManager.AddNamespace("sdtc", Defaults.SdtcNs.NamespaceName);

            if (addDefaultConverters)
            {
                ReplaceConverter<AllergyIntoleranceConverter>()
                    .ReplaceConverter<ClinicalImpressionConverter>()
                    .ReplaceConverter<DeviceConverter>()
                    .ReplaceConverter<EncounterDiagnosesConditionConverter>()
                    .ReplaceConverter<GoalConverter>()
                    .ReplaceConverter<HealthConcernConditionConverter>()
                    .ReplaceConverter<ImmunizationConverter>()
                    .ReplaceConverter<MedicationConverter>()
                    .ReplaceConverter<ProblemListConditionConverter>()
                    .ReplaceConverter<ProcedureConverter>()
                    .ReplaceConverter<ResultObservationConverter>()
                    .ReplaceConverter<SmokingStatusObservationConverter>()
                    .ReplaceConverter<StatusObservationConverter>()
                    .ReplaceConverter<TextSpecificCarePlanConverter>()
                    .ReplaceConverter<VitalSignObservationConverter>();
            }
        }

        /// <summary>
        /// Replaces a specified converter. If the converter does not exist, it is added to the collection
        /// </summary>
        /// <typeparam name="T">The converter type</typeparam>
        /// <returns>This instance of the executor to be used for chaining</returns>
        public CCdaToFhirExecutor ReplaceConverter<T>()
            where T : BaseConverter
        {
            var type = typeof(T);
            if (type.IsAbstract)
                throw new InvalidOperationException($"The type '{type}' cannot be instantiated");

            _converterTypes.Remove(type);
            _converterTypes.Add(type);

            return this;
        }

        /// <summary>
        /// Converts a given CCDA document into its FHIR bundle representation as a bundle
        /// </summary>
        /// <param name="cCda">The CCDA document</param>
        /// <returns>The FHIR bundle representation</returns>
        public Bundle Execute(XDocument cCda)
        {
            if (!_converterTypes.Any())
                throw new InvalidOperationException("There are no converters in the collection");

            var cacheManager = new ConvertedCacheManager();
            var bundle = new Bundle
            {
                Type = Bundle.BundleType.Collection,
                Timestamp = DateTimeOffset.UtcNow
            };

            var organization = _organizationConverter.AddToBundle(
                bundle,
                cCda,
                _namespaceManager,
                cacheManager);

            if (organization?.ResourceType != ResourceType.Organization)
                throw new InvalidOperationException("The organization converter did not produce an organization resource");

            var patientConverterResult = _patientConverter.AddToBundle(
                bundle,
                cCda,
                _namespaceManager,
                cacheManager);

            if (patientConverterResult?.ResourceType != ResourceType.Patient)
                throw new InvalidOperationException("The patient converter did not produce a patient resource");

            var patient = (Patient)patientConverterResult;
            patient.ManagingOrganization = new ResourceReference($"urn:uuid:{organization.Id}");

            foreach (var type in _converterTypes)
            {
                var instance = (BaseConverter)Activator.CreateInstance(type, patient.Id);
                instance.AddToBundle(
                    bundle,
                    cCda,
                    _namespaceManager,
                    cacheManager);
            }

            return bundle;
        }
    }
}
