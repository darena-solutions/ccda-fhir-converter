using System;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Extensions;

namespace DarenaSolutions.CCdaToFhirConverter.Exceptions
{
    /// <summary>
    /// An exception that is thrown when a value that is read from the CCDA is unrecognized
    /// </summary>
    public class UnrecognizedValueException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnrecognizedValueException"/> class
        /// </summary>
        /// <param name="element">The element where the value was read</param>
        /// <param name="unrecognizedValue">The unrecognized value</param>
        /// <param name="additionalXPath">Optionally specify if an additional xpath was applied to <paramref name="element"/>
        /// to get to the unrecognized value</param>
        /// <param name="elementAttributeName">Optionally specify if the value was read from an attribute in <paramref name="element"/></param>
        /// <param name="message">Optionally specify a custom error message if the default error message is not desired</param>
        public UnrecognizedValueException(
            XElement element,
            string unrecognizedValue,
            string additionalXPath = null,
            string elementAttributeName = null,
            string message = null)
            : base(CreateDefaultMessage(element, unrecognizedValue, additionalXPath, elementAttributeName, message))
        {
            Element = element;
            UnrecognizedValue = unrecognizedValue;
            AdditionalXPath = additionalXPath;
            ElementAttributeName = elementAttributeName;
        }

        /// <summary>
        /// Gets the element where the value was read
        /// </summary>
        public XElement Element { get; }

        /// <summary>
        /// Gets the unrecognized value
        /// </summary>
        public string UnrecognizedValue { get; }

        /// <summary>
        /// Gets an additional xpath that was applied to <see cref="Element"/> to get to the unrecognized value
        /// </summary>
        public string AdditionalXPath { get; }

        /// <summary>
        /// Gets the attribute name if the unrecognized value was read from an attribute in <see cref="Element"/>
        /// </summary>
        public string ElementAttributeName { get; }

        private static string CreateDefaultMessage(
            XElement element,
            string unrecognizedValue,
            string additionalXPath,
            string elementAttributeName,
            string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                return message;

            var fullPath = element.GetAbsolutePath();
            if (!string.IsNullOrWhiteSpace(additionalXPath))
                fullPath += additionalXPath.StartsWith("/") ? additionalXPath : $"/{additionalXPath}";

            if (!string.IsNullOrWhiteSpace(elementAttributeName))
                fullPath += $"[@{elementAttributeName}]";

            return $"The value, '{unrecognizedValue}', and path '{fullPath}' is unrecognized";
        }
    }
}
