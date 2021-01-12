// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="GoalConverter.cs" company="Darena Solutions LLC">
// Copyright (c) Darena Solutions LLC. All rights reserved.
// Licensed under the Apache License Version 2.0. See LICENSE file in the project root for full license information.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

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
    /// Converter that converts various elements in the CCDA into goal FHIR resources
    /// </summary>
    public class GoalConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GoalConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public GoalConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='61146-7']/../n1:entry/n1:observation";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var id = Guid.NewGuid().ToString();
            var goal = new Goal
            {
                Id = id,
                Meta = new Meta(),
                Subject = new ResourceReference($"urn:uuid:{PatientId}"),
                LifecycleStatus = Goal.GoalLifecycleStatus.Active
            };

            goal.Meta.ProfileElement.Add(new FhirUri("http://hl7.org/fhir/us/core/StructureDefinition/us-core-goal"));
            var cachedResource = element.SetIdentifiers(context, goal);
            if (cachedResource != null)
                return cachedResource;

            var effectiveTime = element
                .Element(Namespaces.DefaultNs + "effectiveTime")?
                .ToDateTimeDataType();

            if (effectiveTime is Period period)
            {
                goal.Start = period.StartElement.ToDate();

                goal.Target.Add(new Goal.TargetComponent
                {
                    Due = period.EndElement.ToDate()
                });
            }
            else if (effectiveTime is FhirDateTime dateTime)
            {
                goal.Start = dateTime.ToDate();
            }

            try
            {
                var descriptionEl = element.Element(Namespaces.DefaultNs + "value");
                var description = descriptionEl?.ToFhirDataTypeBasedOnType(new[] { "st" }, "Goal.description.text");

                if (description == null)
                    throw new RequiredValueNotFoundException(element, "value", "Goal.description.text");

                goal.Description = new CodeableConcept
                {
                    Text = ((FhirString)description).Value
                };
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            context.Bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = goal
            });

            return goal;
        }
    }
}
