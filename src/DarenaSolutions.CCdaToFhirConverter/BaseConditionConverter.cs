// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="BaseConditionConverter.cs" company="Darena Solutions LLC">
// Copyright (c) Darena Solutions LLC. All rights reserved.
// Licensed under the Apache License Version 2.0. See LICENSE file in the project root for full license information.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

using System;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Base converter that contains the common mapping between all condition type resources
    /// </summary>
    public abstract class BaseConditionConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseConditionConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        protected BaseConditionConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var id = Guid.NewGuid().ToString();
            var condition = new Condition
            {
                Id = id,
                Meta = new Meta(),
                Subject = new ResourceReference($"urn:uuid:{PatientId}"),
                VerificationStatus = new CodeableConcept(
                    "http://terminology.hl7.org/CodeSystem/condition-ver-status",
                    "confirmed",
                    "Confirmed",
                    null),
                ClinicalStatus = new CodeableConcept(
                    "http://terminology.hl7.org/CodeSystem/condition-clinical",
                    "active",
                    "Active",
                    null)
            };

            condition.Meta.ProfileElement.Add(new FhirUri("http://hl7.org/fhir/us/core/StructureDefinition/us-core-condition"));
            var cachedResource = element.SetIdentifiers(context, condition);
            if (cachedResource != null)
                return cachedResource;

            condition.Onset = element
                .Element(Namespaces.DefaultNs + "effectiveTime")?
                .ToDateTimeDataType();

            context.Bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = condition
            });

            return condition;
        }
    }
}
