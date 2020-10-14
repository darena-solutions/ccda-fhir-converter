using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Exceptions;
using DarenaSolutions.CCdaToFhirConverter.Extensions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Convert that converts an element in the CCDA to a patient FHIR resource
    /// </summary>
    public class PatientConverter : BaseSingleResourceConverter
    {
        /// <inheritdoc />
        protected override XElement GetPrimaryElement(XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            var xPath = "n1:ClinicalDocument/n1:recordTarget/n1:patientRole";
            return cCda.XPathSelectElement(xPath, namespaceManager);
        }

        /// <inheritdoc />
        protected override Resource PerformElementConversion(XElement element, ConversionContext context)
        {
            var id = Guid.NewGuid().ToString();
            var patient = new Patient
            {
                Id = id,
                Meta = new Meta()
            };

            patient.Meta.ProfileElement.Add(new Canonical("http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"));

            var identifierElements = element.Elements(Defaults.DefaultNs + "id");
            foreach (var identifierElement in identifierElements)
            {
                try
                {
                    patient.Identifier.Add(identifierElement.ToIdentifier(true, "Patient.identifier"));
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            if (!patient.Identifier.Any())
            {
                try
                {
                    throw new RequiredValueNotFoundException(element, "id", "Patient.identifier");
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            try
            {
                var addressElement = element.Element(Defaults.DefaultNs + "addr");
                if (addressElement != null)
                    patient.Address.Add(addressElement.ToAddress("Patient.address"));
            }
            catch (Exception exception)
            {
                context.Exceptions.Add(exception);
            }

            var telecomElements = element.Elements(Defaults.DefaultNs + "telecom");
            foreach (var telecomElement in telecomElements)
            {
                try
                {
                    patient.Telecom.Add(telecomElement.ToContactPoint("Patient.telecom"));
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            var patientElement = element.Element(Defaults.DefaultNs + "patient");
            if (patientElement != null)
            {
                var nameElements = patientElement.Elements(Defaults.DefaultNs + "name");
                foreach (var nameElement in nameElements)
                {
                    try
                    {
                        patient.Name.Add(nameElement.ToHumanName("Patient.name"));
                    }
                    catch (Exception exception)
                    {
                        context.Exceptions.Add(exception);
                    }
                }

                if (!patient.Name.Any())
                {
                    try
                    {
                        throw new RequiredValueNotFoundException(patientElement, "patient/name", "Patient.name");
                    }
                    catch (Exception exception)
                    {
                        context.Exceptions.Add(exception);
                    }
                }

                try
                {
                    var genderValue = patientElement
                        .Element(Defaults.DefaultNs + "administrativeGenderCode")?
                        .Attribute("displayName")?
                        .Value;

                    if (string.IsNullOrWhiteSpace(genderValue))
                        throw new RequiredValueNotFoundException(patientElement, "administrativeGenderCode[@displayName]", "Patient.gender");

                    var administrativeGender = EnumUtility.ParseLiteral<AdministrativeGender>(genderValue, true);
                    if (administrativeGender == null)
                    {
                        throw new UnrecognizedValueException(
                            patientElement,
                            genderValue,
                            "administrativeGenderCode",
                            "displayName",
                            "Patient.gender");
                    }

                    patient.Gender = administrativeGender;
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }

                try
                {
                    var birthDateValue = patientElement
                        .Element(Defaults.DefaultNs + "birthTime")?
                        .Attribute("value")?
                        .Value;

                    if (!string.IsNullOrWhiteSpace(birthDateValue))
                    {
                        var dt = DateTime.ParseExact(birthDateValue, "yyyyMMdd", CultureInfo.InvariantCulture);
                        patient.BirthDateElement = new Date(dt.Year, dt.Month, dt.Day);
                    }
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }

                try
                {
                    var maritalStatusElement = patientElement.Element(Defaults.DefaultNs + "maritalStatusCode");
                    if (maritalStatusElement != null)
                        patient.MaritalStatus = maritalStatusElement.ToCodeableConcept("Patient.maritalStatus");
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }

                var defaultCodeElements = new List<XElement>();
                var stdcCodeElements = new List<XElement>();

                defaultCodeElements.AddRange(patientElement.Elements(Defaults.DefaultNs + "raceCode"));
                stdcCodeElements.AddRange(patientElement.Elements(Defaults.SdtcNs + "raceCode"));

                var raceExtension = GetRaceOrEthnicGroupExtension(defaultCodeElements, stdcCodeElements, true, context);
                if (raceExtension != null)
                    patient.Extension.Add(raceExtension);

                defaultCodeElements.Clear();
                stdcCodeElements.Clear();

                var ethnicGroupCode = patientElement.Element(Defaults.DefaultNs + "ethnicGroupCode");
                if (ethnicGroupCode != null)
                    defaultCodeElements.Add(ethnicGroupCode);

                var stdcEthnicGroupCode = patientElement.Element(Defaults.SdtcNs + "ethnicGroupCode");
                if (stdcEthnicGroupCode != null)
                    stdcCodeElements.Add(stdcEthnicGroupCode);

                var ethnicityExtension = GetRaceOrEthnicGroupExtension(defaultCodeElements, stdcCodeElements, false, context);
                if (ethnicityExtension != null)
                    patient.Extension.Add(ethnicityExtension);

                var birthPlaceAddressElement = patientElement
                    .Element(Defaults.DefaultNs + "birthplace")?
                    .Element(Defaults.DefaultNs + "place")?
                    .Element(Defaults.DefaultNs + "addr");

                if (birthPlaceAddressElement != null)
                {
                    patient.Extension.Add(new Extension(
                        "http://hl7.org/fhir/StructureDefinition/birthPlace",
                        birthPlaceAddressElement.ToAddress("Patient.extension")));
                }

                var guardianElement = patientElement.Element(Defaults.DefaultNs + "guardian");
                if (guardianElement != null)
                {
                    patient.Contact.Add(new Patient.ContactComponent());

                    try
                    {
                        var codeElement = guardianElement.Element(Defaults.DefaultNs + "code");
                        if (codeElement != null)
                            patient.Contact[0].Relationship.Add(codeElement.ToCodeableConcept("Patient.contact.relationship"));
                    }
                    catch (Exception exception)
                    {
                        context.Exceptions.Add(exception);
                    }

                    try
                    {
                        var guardianAddressElement = guardianElement.Element(Defaults.DefaultNs + "addr");
                        if (guardianAddressElement != null)
                            patient.Contact[0].Address = guardianAddressElement.ToAddress("Patient.contact.address");
                    }
                    catch (Exception exception)
                    {
                        context.Exceptions.Add(exception);
                    }

                    try
                    {
                        var guardianTelecomElement = guardianElement.Element(Defaults.DefaultNs + "telecom");
                        if (guardianTelecomElement != null)
                            patient.Contact[0].Telecom.Add(guardianTelecomElement.ToContactPoint("Patient.contact.telecom"));
                    }
                    catch (Exception exception)
                    {
                        context.Exceptions.Add(exception);
                    }

                    try
                    {
                        var guardianNameElement = guardianElement.Element(Defaults.DefaultNs + "name");
                        if (guardianNameElement != null)
                            patient.Contact[0].Name = guardianNameElement.ToHumanName("Patient.contact.name");
                    }
                    catch (Exception exception)
                    {
                        context.Exceptions.Add(exception);
                    }
                }

                var communicationElement = patientElement.Element(Defaults.DefaultNs + "languageCommunication");
                var communicationCodeElement = communicationElement?.Element(Defaults.DefaultNs + "languageCode");

                if (communicationCodeElement != null)
                {
                    try
                    {
                        var communicationComponent = new Patient.CommunicationComponent
                        {
                            Language = communicationCodeElement.ToCodeableConcept("Patient.communication.language")
                        };

                        communicationComponent.Language.Coding[0].System = "urn:ietf:bcp:47";

                        switch (communicationComponent.Language.Coding[0].Code.ToLowerInvariant())
                        {
                            case "en":
                                communicationComponent.Language.Coding[0].Display = "English";
                                break;
                            case "en-au":
                                communicationComponent.Language.Coding[0].Display = "English (Australia)";
                                break;
                            case "en-ca":
                                communicationComponent.Language.Coding[0].Display = "English (Canada)";
                                break;
                            case "en-in":
                                communicationComponent.Language.Coding[0].Display = "English (India)";
                                break;
                            case "en-gb":
                                communicationComponent.Language.Coding[0].Display = "English (Great Britain)";
                                break;
                            case "en-nz":
                                communicationComponent.Language.Coding[0].Display = "English (New Zeland)";
                                break;
                            case "en-sg":
                                communicationComponent.Language.Coding[0].Display = "English (Singapore)";
                                break;
                            case "en-us":
                                communicationComponent.Language.Coding[0].Display = "English (United States)";
                                break;
                            case "es":
                                communicationComponent.Language.Coding[0].Display = "Spanish";
                                break;
                            case "de":
                                communicationComponent.Language.Coding[0].Display = "German";
                                break;
                            case "da":
                                communicationComponent.Language.Coding[0].Display = "Danish";
                                break;
                            case "fr":
                                communicationComponent.Language.Coding[0].Display = "French";
                                break;
                            default:
                                throw new UnrecognizedValueException(
                                    communicationCodeElement,
                                    communicationComponent.Language.Coding[0].Code,
                                    elementAttributeName: "code",
                                    fhirPropertyPath: "Patient.communication.language.coding.code");
                        }

                        try
                        {
                            var preferredValue = communicationElement
                                .Element(Defaults.DefaultNs + "preferenceInd")?
                                .Attribute("value")?
                                .Value;

                            if (!string.IsNullOrWhiteSpace(preferredValue))
                                communicationComponent.Preferred = bool.Parse(preferredValue);
                        }
                        catch (Exception exception)
                        {
                            context.Exceptions.Add(exception);
                        }

                        patient.Communication.Add(communicationComponent);
                    }
                    catch (Exception exception)
                    {
                        context.Exceptions.Add(exception);
                    }
                }
            }

            context.Bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{id}",
                Resource = patient
            });

            return patient;
        }

        private Extension GetRaceOrEthnicGroupExtension(
            List<XElement> defaultCodes,
            List<XElement> stdcCodes,
            bool isRace,
            ConversionContext context)
        {
            // Don't interact with stdc codes if there aren't any default codes
            if (!defaultCodes.Any())
                return null;

            var extension = new Extension
            {
                Url = isRace
                    ? "http://hl7.org/fhir/us/core/StructureDefinition/us-core-race"
                    : "http://hl7.org/fhir/us/core/StructureDefinition/us-core-ethnicity"
            };

            var combinedElements = defaultCodes.Concat(stdcCodes);
            var ombCategoryCount = 0;

            foreach (var element in combinedElements)
            {
                var innerExtension = new Extension();

                var nullFlavorValue = element.Attribute("nullFlavor")?.Value;
                if (!string.IsNullOrWhiteSpace(nullFlavorValue))
                {
                    var coding = new Coding("http://terminology.hl7.org/CodeSystem/v3-NullFlavor", nullFlavorValue)
                    {
                        Display = string.Equals("ASKU", nullFlavorValue, StringComparison.InvariantCultureIgnoreCase)
                            ? "Asked but no answer"
                            : "Unknown"
                    };

                    innerExtension.Url = "ombCategory";
                    innerExtension.Value = coding;
                    ombCategoryCount++;
                }
                else
                {
                    var coding = element.ToCoding();

                    if (isRace)
                    {
                        switch (coding.Code)
                        {
                            case "1002-5":
                            case "2028-9":
                            case "2054-5":
                            case "2076-8":
                            case "2106-3":
                                innerExtension.Url = "ombCategory";
                                ombCategoryCount++;
                                break;
                            default:
                                innerExtension.Url = "detailed";
                                break;
                        }
                    }
                    else
                    {
                        switch (coding.Code)
                        {
                            case "2135-2":
                            case "2186-5":
                                innerExtension.Url = "ombCategory";
                                ombCategoryCount++;
                                break;
                            default:
                                innerExtension.Url = "detailed";
                                break;
                        }
                    }

                    innerExtension.Value = coding;
                }

                extension.Extension.Add(innerExtension);
            }

            if (isRace && ombCategoryCount > 5)
            {
                try
                {
                    throw new ProfileRelatedException(
                        defaultCodes[0].Parent,
                        "More than 5 omb category race codes were found",
                        defaultCodes[0].Name.LocalName,
                        "Patient.extension");
                }
                catch (Exception exception)
                {
                    context.Exceptions.Add(exception);
                }
            }

            var textExtension = new Extension { Url = "text" };

            if (stdcCodes.Any())
            {
                textExtension.Value = new FhirString("Mixed");
            }
            else
            {
                var nullFlavorValue = defaultCodes[0].Attribute("nullFlavor")?.Value;
                if (!string.IsNullOrWhiteSpace(nullFlavorValue))
                {
                    var strValue = string.Equals("ASKU", nullFlavorValue, StringComparison.InvariantCultureIgnoreCase)
                        ? "Asked but no answer"
                        : "Unknown";

                    textExtension.Value = new FhirString(strValue);
                }
                else
                {
                    var displayNameValue = defaultCodes[0].Attribute("displayName")?.Value;
                    textExtension.Value = new FhirString(displayNameValue);
                }
            }

            extension.Extension.Add(textExtension);
            return extension;
        }
    }
}
