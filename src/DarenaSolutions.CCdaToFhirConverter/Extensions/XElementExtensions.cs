using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DarenaSolutions.CCdaToFhirConverter.Constants;
using DarenaSolutions.CCdaToFhirConverter.Exceptions;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter.Extensions
{
    /// <summary>
    /// A class that contains extensions to the <see cref="XElement"/> class
    /// </summary>
    public static class XElementExtensions
    {
        /// <summary>
        /// Converts an element into its FHIR <see cref="Coding"/> representation
        /// </summary>
        /// <param name="self">The source element</param>
        /// <returns>The FHIR <see cref="Coding"/> representation of the source element</returns>
        public static Coding ToCoding(this XElement self)
        {
            return new Coding(
                self.Attribute("codeSystem")?.Value,
                self.Attribute("code")?.Value,
                self.Attribute("displayName")?.Value);
        }

        /// <summary>
        /// Converts an element into its FHIR <see cref="CodeableConcept"/> representation
        /// </summary>
        /// <param name="self">The source element</param>
        /// <returns>The FHIR <see cref="CodeableConcept"/> representation of the source element</returns>
        public static CodeableConcept ToCodeableConcept(this XElement self)
        {
            var codeableConcept = new CodeableConcept(
                ConvertKnownSystemOid(self.Attribute("codeSystem")?.Value),
                self.Attribute("code")?.Value.Trim(),
                self.Attribute("displayName")?.Value,
                null);

            var coding = codeableConcept.Coding.First();
            if (string.IsNullOrWhiteSpace(coding.Code))
            {
                var nullFlavorValue = self.Attribute("nullFlavor")?.Value;

                if (!string.IsNullOrWhiteSpace(nullFlavorValue))
                {
                    if (!nullFlavorValue.IsValidNullFlavorValue())
                        throw new UnrecognizedValueException(self, nullFlavorValue, elementAttributeName: "nullFlavor");

                    coding.CodeElement = new Code
                    {
                        Extension = new List<Extension>
                        {
                            new Extension(Defaults.NullFlavorSystem, new Code(nullFlavorValue))
                        }
                    };
                }
            }

            return codeableConcept;
        }

        /// <summary>
        /// Converts an element into its FHIR <see cref="Address"/> representation
        /// </summary>
        /// <param name="self">The source element</param>
        /// <returns>The FHIR <see cref="Address"/> representation of the source element</returns>
        public static Address ToAddress(this XElement self)
        {
            var address = new Address
            {
                City = self.Element(Defaults.DefaultNs + "city")?.Value,
                State = self.Element(Defaults.DefaultNs + "state")?.Value,
                PostalCode = self.Element(Defaults.DefaultNs + "postalCode")?.Value,
                Country = self.Element(Defaults.DefaultNs + "country")?.Value
            };

            var lineElements = self.Elements(Defaults.DefaultNs + "streetAddressLine");
            foreach (var lineElement in lineElements)
            {
                address.LineElement.Add(new FhirString(lineElement.Value));
            }

            var useValue = self.Attribute("use")?.Value;
            switch (useValue)
            {
                case "HP":
                case "H":
                    address.Use = Address.AddressUse.Home;
                    break;
                case "WP":
                    address.Use = Address.AddressUse.Work;
                    break;
                case "TMP":
                    address.Use = Address.AddressUse.Temp;
                    break;
                case "BAD":
                    address.Use = Address.AddressUse.Old;
                    break;
                case null:
                    break;
                default:
                    throw new UnrecognizedValueException(self, useValue, elementAttributeName: "use");
            }

            return address;
        }

        /// <summary>
        /// Converts an element into its FHIR <see cref="HumanName"/> representation
        /// </summary>
        /// <param name="self">The source element</param>
        /// <returns>The FHIR <see cref="HumanName"/> representation of the source element</returns>
        public static HumanName ToHumanName(this XElement self)
        {
            HumanName.NameUse? use;
            var useValue = self.Attribute("use")?.Value;
            switch (useValue)
            {
                case "C":
                case "L":
                    use = HumanName.NameUse.Usual;
                    break;
                case "P":
                    use = HumanName.NameUse.Nickname;
                    break;
                case null:
                    use = null;
                    break;
                default:
                    throw new UnrecognizedValueException(self, useValue, elementAttributeName: "use");
            }

            var name = new HumanName
            {
                Use = use,
                Family = self.Element(Defaults.DefaultNs + "family")?.Value
            };

            var givenElements = self.Elements(Defaults.DefaultNs + "given");
            foreach (var givenElement in givenElements)
            {
                name.GivenElement.Add(new FhirString(givenElement.Value));
            }

            var prefixElements = self.Elements(Defaults.DefaultNs + "prefix");
            foreach (var prefixElement in prefixElements)
            {
                name.PrefixElement.Add(new FhirString(prefixElement.Value));
            }

            var suffixElements = self.Elements(Defaults.DefaultNs + "suffix");
            foreach (var suffixElement in suffixElements)
            {
                name.SuffixElement.Add(new FhirString(suffixElement.Value));
            }

            return name;
        }

        /// <summary>
        /// Converts an element into its FHIR <see cref="ContactPoint"/> representation
        /// </summary>
        /// <param name="self">The source element</param>
        /// <param name="fhirPropertyPath">Optionally specify the path to the FHIR property where the value will be ultimately
        /// mapped to. If a path is provided, it will be included in the <see cref="RequiredValueNotFoundException"/> exception</param>
        /// <returns>The FHIR <see cref="ContactPoint"/> representation of the source element</returns>
        public static ContactPoint ToContactPoint(this XElement self, string fhirPropertyPath = null)
        {
            var value = self.Attribute("value")?.Value;
            if (string.IsNullOrWhiteSpace(value))
                throw new RequiredValueNotFoundException(self, "[@value]", $"{fhirPropertyPath}.value");

            ContactPoint.ContactPointUse? contactPointUse;
            var use = self.Attribute("use")?.Value;

            switch (use)
            {
                case "HP":
                case "H":
                    contactPointUse = ContactPoint.ContactPointUse.Home;
                    break;
                case "MC":
                    contactPointUse = ContactPoint.ContactPointUse.Mobile;
                    break;
                case "WP":
                    contactPointUse = ContactPoint.ContactPointUse.Work;
                    break;
                case "TMP":
                    contactPointUse = ContactPoint.ContactPointUse.Temp;
                    break;
                case "BAD":
                    contactPointUse = ContactPoint.ContactPointUse.Old;
                    break;
                case null:
                    contactPointUse = null;
                    break;
                default:
                    throw new UnrecognizedValueException(self, use, elementAttributeName: "use");
            }

            ContactPoint.ContactPointSystem system;
            if (value.StartsWith("tel:"))
            {
                system = ContactPoint.ContactPointSystem.Phone;
                value = value.Replace("tel:", string.Empty);
            }
            else if (value.StartsWith("mailto:"))
            {
                system = ContactPoint.ContactPointSystem.Email;
                value = value.Replace("mailto:", string.Empty);
            }
            else
            {
                system = ContactPoint.ContactPointSystem.Phone;
            }

            var contactPoint = new ContactPoint(
                system,
                contactPointUse,
                value);

            return contactPoint;
        }

        /// <summary>
        /// Converts an element into its FHIR <see cref="Identifier"/> representation
        /// </summary>
        /// <param name="self">The source element</param>
        /// <param name="systemAndValueMustExist">Optionally indicate if system and value must exist. If one of the values
        /// do not exist a <see cref="InvalidOperationException"/> exception will be thrown. (Default: <c>false</c>)</param>
        /// <param name="fhirPropertyPath">Optionally specify the path to the FHIR property where the value will be ultimately
        /// mapped to. If a path is provided and <paramref name="systemAndValueMustExist"/> is equal to <c>true</c>, then
        /// the path will be included in the <see cref="RequiredValueNotFoundException"/> exception</param>
        /// <returns>The FHIR <see cref="Identifier"/> representation of the source element</returns>
        public static Identifier ToIdentifier(this XElement self, bool systemAndValueMustExist = false, string fhirPropertyPath = null)
        {
            var systemValue = self.Attribute("root")?.Value;
            if (systemAndValueMustExist && string.IsNullOrWhiteSpace(systemValue))
                throw new RequiredValueNotFoundException(self, "[@root]", $"{fhirPropertyPath}.system");

            var codeValue = self.Attribute("extension")?.Value;
            if (systemAndValueMustExist && string.IsNullOrWhiteSpace(codeValue))
                throw new RequiredValueNotFoundException(self, "[@extension]", $"{fhirPropertyPath}.value");

            var identifier = new Identifier(ConvertKnownSystemOid(systemValue), codeValue);

            var assigningAuthorityNameValue = self.Attribute("assigningAuthorityName")?.Value;
            if (!string.IsNullOrWhiteSpace(assigningAuthorityNameValue))
                identifier.Assigner = new ResourceReference { Display = assigningAuthorityNameValue };

            return identifier;
        }

        /// <summary>
        /// Converts an element into its FHIR <see cref="Period"/> representation. This extension will only return a period
        /// if the source element has both the &lt;low&gt; AND &lt;high&gt; elements and both those elements have a value
        /// in them. Otherwise, a <c>null</c> will be returned.
        ///
        /// EG:
        /// &lt;effectiveTime&gt;
        ///   &lt;low value='20200101' /&gt;
        ///   &lt;high value='20210101' /&gt;
        /// &lt;/effectiveTime&gt;
        ///
        /// will return a value, however, the following will return <c>null</c>:
        /// &lt;effectiveTime&gt;
        ///   &lt;low value='20200101' /&gt;
        /// &lt;/effectiveTime&gt;
        ///
        /// This is because the second element does not have a &lt;high&gt; element
        /// </summary>
        /// <param name="self">The source element that must contain &lt;low&gt; and &lt;high&gt; child elements</param>
        /// <returns>The FHIR <see cref="Period"/> representation of the source element</returns>
        public static Period ToPeriod(this XElement self)
        {
            var lowValue = self
                .Element(Defaults.DefaultNs + "low")?
                .Attribute("value")?
                .Value;

            if (string.IsNullOrWhiteSpace(lowValue))
                return null;

            var highValue = self
                .Element(Defaults.DefaultNs + "high")?
                .Attribute("value")?
                .Value;

            if (string.IsNullOrWhiteSpace(highValue))
                return null;

            return new Period
            {
                StartElement = new FhirDateTime(lowValue.ParseCCdaDateTimeOffset()),
                EndElement = new FhirDateTime(highValue.ParseCCdaDateTimeOffset())
            };
        }

        /// <summary>
        /// Converts an element into its FHIR <see cref="Date"/> representation. The date is derived from one of
        /// two places. If the source element has a child &lt;low&gt; element, then the value of that element will be used.
        /// If a &lt;low&gt; child element does not exist or there is no value for it, then the source elements 'value'
        /// attribute will be used. If the source element also has no 'value' attribute, then <c>null</c> will be returned
        /// </summary>
        /// <param name="self">The source element</param>
        /// <returns>The FHIR <see cref="Date"/> representation of the source element</returns>
        public static Date ToFhirDate(this XElement self)
        {
            var lowValue = self
                .Element(Defaults.DefaultNs + "low")?
                .Attribute("value")?
                .Value;

            if (string.IsNullOrWhiteSpace(lowValue))
            {
                lowValue = self.Attribute("value")?.Value;
                if (string.IsNullOrWhiteSpace(lowValue))
                    return null;
            }

            var parsedDate = lowValue.ParseCCdaDateTime();
            return new Date(parsedDate.Year, parsedDate.Month, parsedDate.Day);
        }

        /// <summary>
        /// Converts an element into its FHIR <see cref="FhirDateTime"/> representation. The date is derived from one of
        /// two places. If the source element has a child &lt;low&gt; element, then the value of that element will be used.
        /// If a &lt;low&gt; child element does not exist or there is no value for it, then the source elements 'value'
        /// attribute will be used. If the source element also has no 'value' attribute, then <c>null</c> will be returned
        /// </summary>
        /// <param name="self">The source element</param>
        /// <returns>The FHIR <see cref="FhirDateTime"/> representation of the source element</returns>
        public static FhirDateTime ToFhirDateTime(this XElement self)
        {
            var lowValue = self
                .Element(Defaults.DefaultNs + "low")?
                .Attribute("value")?
                .Value;

            if (string.IsNullOrWhiteSpace(lowValue))
            {
                lowValue = self.Attribute("value")?.Value;
                if (string.IsNullOrWhiteSpace(lowValue))
                    return null;
            }

            return new FhirDateTime(lowValue.ParseCCdaDateTimeOffset());
        }

        /// <summary>
        /// This method will return either a <see cref="Period"/> or <see cref="FhirDateTime"/> type object. If the source
        /// element has &lt;low&gt; and &lt;high&gt; child elements and both those elements have values, then a <see cref="Period"/>
        /// will be returned. Otherwise, a <see cref="FhirDateTime"/> will be returned. If no date value could be found
        /// to construct either a <see cref="Period"/> and <see cref="FhirDateTime"/> then <c>null</c> will be returned
        /// </summary>
        /// <param name="self">The source element</param>
        /// <returns>The FHIR <see cref="Period"/> or <see cref="FhirDateTime"/> representation of the source element</returns>
        public static Element ToDateTimeElement(this XElement self)
        {
            var period = self.ToPeriod();
            if (period != null)
                return period;

            return self.ToFhirDateTime();
        }

        /// <summary>
        /// Converts a system value OID into the FHIR URI that represents that OID, only if the system OID is known. Otherwise,
        /// the OID is returned without any conversion
        /// </summary>
        /// <param name="system">The source system value OID</param>
        /// <returns>The FHIR URI that represents the specified system value OID if known. Otherwise, the system value oid
        /// is returned without any conversion</returns>
        public static string ConvertKnownSystemOid(string system)
        {
            switch (system)
            {
                case "2.16.840.1.113883.6.1":
                    return "http://loinc.org";
                case "2.16.840.1.113883.6.96":
                    return "http://snomed.info/sct";
                case "2.16.840.1.113883.6.88":
                    return "http://www.nlm.nih.gov/research/umls/rxnorm";
                case "2.16.840.1.113883.3.88.12.3221.8.9":
                    return "http://snomed.info/sct";
                case "2.16.840.1.113883.5.83":
                    return "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation";
                case "2.16.840.1.113883.4.1":
                    return "http://hl7.org/fhir/sid/us-ssn";
                case "2.16.840.1.113883.4.6":
                    return "http://hl7.org/fhir/sid/us-npi";
                case "2.16.840.1.113883.4.572":
                    return "http://hl7.org/fhir/sid/us-medicare";
                case "2.16.840.1.113883.4.927":
                    return "http://hl7.org/fhir/sid/us-mbi";
                case "2.16.840.1.113883.6.59":
                    return "http://hl7.org/fhir/sid/cvx";
                case "2.16.840.1.113883.6.101":
                    return "http://nucc.org/provider-taxonomy";
                default:
                    return system;
            }
        }

        /// <summary>
        /// Converts an element into its FHIR <see cref="SimpleQuantity" /> representation
        /// </summary>
        /// <param name="self">The source element</param>
        /// <returns>The FHIR <see cref="SimpleQuantity"/> representation of the source element</returns>
        public static SimpleQuantity ToSimpleQuantity(this XElement self)
        {
            var value = self.Attribute("value")?.Value;

            if (!decimal.TryParse(value, out var dValue))
                throw new UnrecognizedValueException(self, value, elementAttributeName: "value");

            return new SimpleQuantity { Value = dValue, Unit = self.Attribute("unit")?.Value };
        }

        /// <summary>
        /// Loops through all child nodes of an element and returns the value of the first text node that is found
        /// </summary>
        /// <param name="self">The source element</param>
        /// <returns>The value of the first text node of the element</returns>
        public static string GetFirstTextNode(this XElement self)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            var nodes = self.Nodes();
            foreach (var node in nodes)
            {
                if (!(node is XText textNode))
                    continue;

                return textNode.Value;
            }

            return null;
        }

        /// <summary>
        /// Finds the code element in an element while also including the translation element. The translation element will
        /// take precedence if it exists, otherwise, the code element will be returned if found
        /// </summary>
        /// <param name="self">The source element</param>
        /// <param name="initialXPath">Optionally provide an initial xpath to first run before applying the check for the
        /// code and translation elements</param>
        /// <param name="namespaceManager">Optionally provide a namespace manager. This is required if <paramref name="initialXPath"/>
        /// is given</param>
        /// <param name="codeElementName">The name of the element that contains the code. The default is 'code'. This can
        /// be optionally changed to another value in situations where the element name can be something else, such as 'value'</param>
        /// <param name="translationOnly">If true, do not consider the "code" element, if the translation element is not found</param>
        /// <returns>The translation element or the code element in a given element. The translation element takes precedence,
        /// so this is what will be returned if both translation and code elements are found</returns>
        public static XElement FindCodeElementWithTranslation(
            this XElement self,
            string initialXPath = null,
            XmlNamespaceManager namespaceManager = null,
            string codeElementName = "code",
            bool translationOnly = false)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            if (!string.IsNullOrWhiteSpace(initialXPath) && namespaceManager == null)
            {
                throw new ArgumentException(
                    "If an initial xpath is provided, then a namespace manager must also be provided");
            }

            var initialEl = string.IsNullOrWhiteSpace(initialXPath)
                ? self
                : self.XPathSelectElement(initialXPath, namespaceManager);

            var el = initialEl?
                .Element(Defaults.DefaultNs + codeElementName)?
                .Element(Defaults.DefaultNs + "translation");

            if (el == null && !translationOnly)
                el = initialEl?.Element(Defaults.DefaultNs + codeElementName);

            return el;
        }

        /// <summary>
        /// Reads all inner content (including all descendants) and returns it as a string
        /// </summary>
        /// <param name="self">The source element</param>
        /// <returns>All inner content including descendants and their tags</returns>
        public static string GetContentsAsString(this XElement self)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            using var reader = self.CreateReader();
            reader.MoveToContent();
            return reader.ReadInnerXml();
        }

        /// <summary>
        /// Converts a 'value' element which can be several possible types into its FHIR represented type. The type is determined
        /// by reading the 'xsi:type' attribute of the 'value' element. Note that only 'value' elements will work with this
        /// extension
        /// </summary>
        /// <param name="self">The source element</param>
        /// <param name="expectedTypes">Optionally provide a list of expected types. If a list is provided and the type
        /// that is read does not exist in this list, an exception will be thrown</param>
        /// <param name="fhirPropertyPath">Optionally specify the path to the FHIR property where the value will be ultimately
        /// mapped to. If a path is provided, it will be included in exceptions thrown by this extension method</param>
        /// <returns>The FHIR representation of the source element, based on reading the 'xsi:type' attribute</returns>
        public static Element ToFhirElementBasedOnType(this XElement self, string[] expectedTypes = null, string fhirPropertyPath = null)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            if (self.Name.LocalName != "value")
                throw new ArgumentException("Only 'value' elements can be used with this extension");

            var type = self.Attribute(Defaults.XsiNs + "type")?.Value.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(type))
                throw new RequiredValueNotFoundException(self, "[@type]", fhirPropertyPath);

            if (expectedTypes != null && expectedTypes.Any() && !expectedTypes.Select(x => x.ToLowerInvariant()).Contains(type))
                throw new UnexpectedValueTypeException(self, type, fhirPropertyPath);

            switch (type)
            {
                case "cd":
                case "co":
                    // Concept descriptor -> Codeable concept
                    // Coded data -> Codeable concept
                    return self.ToCodeableConcept();
                case "st":
                    // Character string -> String
                    return new FhirString(self.GetFirstTextNode());
                case "pq":
                    // Dimensioned quantity -> Simple quantity
                    return self.ToSimpleQuantity();
                case "ts":
                    // Point in time -> DateTime or Period element
                    return self.ToDateTimeElement();
                default:
                    throw new UnrecognizedValueException(self, type, elementAttributeName: "type");
            }
        }

        /// <summary>
        /// Gets the absolute path of the element
        /// </summary>
        /// <param name="self">The source element</param>
        /// <returns>The absolute path of this element</returns>
        public static string GetAbsolutePath(this XElement self)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            return BuildAbsolutePath(self, new Stack<string>());
        }

        private static string BuildAbsolutePath(XElement element, Stack<string> path)
        {
            var name = element.Name.LocalName;
            var parent = element.Parent;

            if (parent != null)
            {
                var i = -1;
                var childElements = parent
                    .Elements()
                    .Where(x => x.Name.LocalName == element.Name.LocalName)
                    .ToList();

                foreach (var childElement in childElements)
                {
                    i++;
                    if (childElement == element)
                        break;
                }

                if (childElements.Count > 1)
                    name += $"[{i}]";
            }

            path.Push(name);
            return parent == null
                ? $"/{string.Join("/", path)}"
                : BuildAbsolutePath(parent, path);
        }
    }
}
