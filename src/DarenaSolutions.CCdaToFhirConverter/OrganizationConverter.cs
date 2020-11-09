// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="OrganizationConverter.cs" company="Darena Solutions LLC">
// Copyright (c) Darena Solutions LLC. All rights reserved.
// Licensed under the Apache License Version 2.0. See LICENSE file in the project root for full license information.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

using System;
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
    /// Converter that converts an element in the CCDA to an organization FHIR resource
    /// </summary>
    public class OrganizationConverter : BaseSingleResourceConverter
    {
        /// <inheritdoc />
        protected override XElement GetPrimaryElement(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "n1:ClinicalDocument/n1:author/n1:assignedAuthor/n1:representedOrganization";
            return cCda.XPathSelectElement(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var id = Guid.NewGuid().ToString();
            var organization = new Organization
            {
                Id = id,
                Meta = new Meta(),
                Name = element.Element(Namespaces.DefaultNs + "name")?.Value,
                Active = true
            };

            if (string.IsNullOrWhiteSpace(organization.Name))
            {
                try
                {
                    throw new RequiredValueNotFoundException(element, "name", "Organization.name");
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            organization.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-organization"));
            var cachedResource = element.SetIdentifiers(context, organization);
            if (cachedResource != null)
                return cachedResource;

            var telecoms = element.Elements(Namespaces.DefaultNs + "telecom");
            foreach (var telecom in telecoms)
            {
                try
                {
                    organization.Telecom.Add(telecom.ToContactPoint("Organization.telecom"));
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            var addressElements = element.Elements(Namespaces.DefaultNs + "addr");
            foreach (var addressElement in addressElements)
            {
                try
                {
                    var address = addressElement.ToAddress("Organization.address");
                    if (address.LineElement.Count > 4)
                    {
                        throw new ProfileRelatedException(
                            addressElement,
                            "More than 4 address lines were provided",
                            "streetAddressLine",
                            "Organization.address.line");
                    }

                    organization.Address.Add(address);
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            context.Bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = organization
            });

            return organization;
        }
    }
}
