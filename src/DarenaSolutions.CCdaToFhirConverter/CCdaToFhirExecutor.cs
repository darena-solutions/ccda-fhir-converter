// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="CCdaToFhirExecutor.cs" company="Darena Solutions LLC">
// Copyright (c) Darena Solutions LLC. All rights reserved.
// Licensed under the Apache License Version 2.0. See LICENSE file in the project root for full license information.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

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
        private readonly Dictionary<Type, Func<ConverterFactoryContext, IResourceConverter>> _converterTypes;

        private bool _patientOnlyConversion;

        /// <summary>
        /// Initializes a new instance of the <see cref="CCdaToFhirExecutor"/> class
        /// </summary>
        /// <param name="organizationConverter">Optionally specify an implementation of <see cref="ISingleResourceConverter"/>
        /// for the primary organization. If one is not provided, <see cref="OrganizationConverter"/> will be used by default.
        /// The argument passed here is ignored if <paramref name="patientOnlyConversion"/> is set to <c>true</c></param>
        /// <param name="patientConverter">Optionally specify an implementation of <see cref="ISingleResourceConverter"/>
        /// for the primary patient. If one is not provided, <see cref="PatientConverter"/> will be used by default</param>
        /// <param name="addDefaultConverters">Optionally specify if the default converters of this library should be added
        /// to the converter collection. This argument is ignored if <paramref name="patientOnlyConversion"/> is set to
        /// <c>true</c>. (Default: <c>true</c>)</param>
        /// <param name="patientOnlyConversion">Optionally indicate if only the patient resource should be converted from
        /// the CCDA. If this is set to <c>true</c> then the <paramref name="addDefaultConverters"/> and <paramref name="organizationConverter"/>
        /// arguments are ignored. (Default: <c>false</c>).</param>
        public CCdaToFhirExecutor(
            ISingleResourceConverter organizationConverter = null,
            ISingleResourceConverter patientConverter = null,
            bool addDefaultConverters = true,
            bool patientOnlyConversion = false)
        {
            _organizationConverter = organizationConverter;
            if (_organizationConverter == null && !patientOnlyConversion)
                _organizationConverter = new OrganizationConverter();

            _patientConverter = patientConverter ?? new PatientConverter();
            _patientOnlyConversion = patientOnlyConversion;

            _converterTypes = new Dictionary<Type, Func<ConverterFactoryContext, IResourceConverter>>();

            _namespaceManager = new XmlNamespaceManager(new NameTable());
            _namespaceManager.AddNamespace("n1", Namespaces.DefaultNs.NamespaceName);
            _namespaceManager.AddNamespace("sdtc", Namespaces.SdtcNs.NamespaceName);

            if (!_patientOnlyConversion && addDefaultConverters)
            {
                ReplaceConverter<AllergyIntoleranceConverter>()
                    .ReplaceConverter<ClinicalImpressionConverter>()
                    .ReplaceConverter<DeviceConverter>()
                    .ReplaceConverter<EncounterConverter>()
                    .ReplaceConverter<GoalConverter>()
                    .ReplaceConverter<HealthConcernConditionConverter>()
                    .ReplaceConverter<ImmunizationConverter>()
                    .ReplaceConverter<MedicationStatementConverter>()
                    .ReplaceConverter<ProblemListConditionConverter>()
                    .ReplaceConverter<ProcedureConverter>()
                    .ReplaceConverter<ResultObservationConverter>()
                    .ReplaceConverter<SmokingStatusObservationConverter>()
                    .ReplaceConverter<StatusObservationConverter>()
                    .ReplaceConverter<TextSpecificCarePlanConverter>()
                    .ReplaceConverter<VitalSignObservationConverter>()
                    .ReplaceConverter<CareTeamConverter>()
                    .ReplaceConverter<LaboratoryResultDiagnosticReportConverter>()
                    .ReplaceConverter<MedicationRequestConverter>()
                    .ReplaceConverter<PractitionerRoleConverter>();
            }
        }

        /// <summary>
        /// Replaces a specified converter. If the converter does not exist, it is added to the collection. Converters added
        /// to the collection will be instantiated by the library. If the converter type is derived from <see cref="BaseConverter"/>,
        /// then an attempt to create the converter using the default constructor of <see cref="BaseConverter"/> which requires
        /// a patient id argument will be performed. If the converter type does not derive from <see cref="BaseConverter"/>,
        /// then a factory method must be passed into <paramref name="factory"/>
        /// </summary>
        /// <typeparam name="T">The converter type</typeparam>
        /// <param name="factory">Optionally provide a factory method to manually instantiate the converter. This will take
        /// precedence over trying to instantiate a converter type through a default constructor</param>
        /// <returns>This instance of the executor to be used for chaining</returns>
        public CCdaToFhirExecutor ReplaceConverter<T>(Func<ConverterFactoryContext, IResourceConverter> factory = null)
            where T : IResourceConverter
        {
            var type = typeof(T);
            if (type.IsAbstract)
                throw new InvalidOperationException($"The type '{type}' cannot be instantiated");

            if (!type.IsSubclassOf(typeof(BaseConverter)) && factory == null)
            {
                throw new ArgumentException(
                    "If the converter type does not derive from BaseConverter, then a factory method is required");
            }

            if (_converterTypes.ContainsKey(type))
                _converterTypes.Remove(type);

            _converterTypes.Add(type, factory);
            return this;
        }

        /// <summary>
        /// Converts a given CCDA document into its FHIR bundle representation as a bundle
        /// </summary>
        /// <param name="cCda">The CCDA document</param>
        /// <returns>The FHIR bundle representation</returns>
        public Bundle Execute(XDocument cCda)
        {
            if (!_patientOnlyConversion && !_converterTypes.Any())
                throw new InvalidOperationException("There are no converters in the collection");

            var bundle = new Bundle
            {
                Type = Bundle.BundleType.Collection,
                Timestamp = DateTimeOffset.UtcNow
            };

            var context = new ConversionContext(bundle, cCda, _namespaceManager);
            var patientConverterResult = _patientConverter.AddToBundle(cCda, context);

            if (patientConverterResult?.TypeName != ResourceType.Patient.ToString())
                throw new InvalidOperationException("The patient converter did not produce a patient resource");

            var patient = (Patient)patientConverterResult;

            if (!_patientOnlyConversion)
            {
                var organization = _organizationConverter.AddToBundle(cCda, context);
                if (organization?.TypeName != ResourceType.Organization.ToString())
                    throw new InvalidOperationException("The organization converter did not produce an organization resource");

                patient.ManagingOrganization = new ResourceReference($"urn:uuid:{organization.Id}");

                foreach (var entry in _converterTypes)
                {
                    IResourceConverter instance;
                    if (entry.Value != null)
                        instance = entry.Value(new ConverterFactoryContext(patient.Id, organization.Id, bundle));
                    else
                        instance = (BaseConverter)Activator.CreateInstance(entry.Key, patient.Id);

                    try
                    {
                        instance.AddToBundle(cCda, context);
                    }
                    catch (Exception exception)
                    {
                        context.Exceptions.Add(exception);
                    }
                }
            }

            if (context.Exceptions.Any())
                throw new AggregateException(context.Exceptions);

            return bundle;
        }
    }
}
