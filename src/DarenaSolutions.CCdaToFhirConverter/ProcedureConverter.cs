// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="ProcedureConverter.cs" company="Darena Solutions LLC">
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
    /// Converter that converts various elements in the CCDA into FHIR procedure resources
    /// </summary>
    public class ProcedureConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcedureConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public ProcedureConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "//n1:section/n1:code[@code='47519-4']/../n1:entry/n1:procedure";
            return cCda.XPathSelectElements(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var id = Guid.NewGuid().ToString();
            var procedure = new Procedure
            {
                Id = id,
                Meta = new Meta(),
                Subject = new ResourceReference($"urn:uuid:{PatientId}"),
                Status = EventStatus.Completed
            };

            procedure.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-procedure"));
            var cachedResource = element.SetIdentifiers(context, procedure);
            if (cachedResource != null)
                return cachedResource;

            try
            {
                procedure.Code = element
                    .FindCodeElementWithTranslation()?
                    .ToCodeableConcept("Procedure.code");

                if (procedure.Code == null)
                    throw new RequiredValueNotFoundException(element, "code", "Procedure.code");
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            try
            {
                procedure.Performed = element
                    .Element(Namespaces.DefaultNs + "effectiveTime")?
                    .ToDateTimeElement();

                if (procedure.Performed == null)
                    throw new RequiredValueNotFoundException(element, "effectiveTime", "Procedure.performed");
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            var participantRoleEl = element
                .Element(Namespaces.DefaultNs + "participant")?
                .Element(Namespaces.DefaultNs + "participantRole");

            if (participantRoleEl != null)
            {
                try
                {
                    var deviceConverter = new DeviceConverter(PatientId);
                    deviceConverter.AddToBundle(new List<XElement> { participantRoleEl }, context);
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            context.Bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = procedure
            });

            return procedure;
        }
    }
}
