using System;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Extensions;

namespace DarenaSolutions.CCdaToFhirConverter.Exceptions
{
    /// <summary>
    /// An exception that is thrown when a 'value' element is expected to be a certain type, but is not the expected type
    /// </summary>
    public class UnexpectedValueTypeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedValueTypeException"/> class
        /// </summary>
        /// <param name="element">The 'value' element</param>
        /// <param name="unexpectedType">The unexpected type</param>
        /// <param name="fhirPropertyPath">Optionally specify the path to the FHIR property that can provide additional
        /// context to this exception</param>
        /// <param name="message">Optionally specify a custom error message if the default error message is not desired</param>
        public UnexpectedValueTypeException(
            XElement element,
            string unexpectedType,
            string fhirPropertyPath = null,
            string message = null)
            : base(CreateDefaultMessage(element, unexpectedType, fhirPropertyPath, message))
        {
            Element = element;
            UnexpectedType = unexpectedType;
            FhirPropertyPath = fhirPropertyPath;
        }

        /// <summary>
        /// Gets the 'value' element
        /// </summary>
        public XElement Element { get; }

        /// <summary>
        /// Gets the unexpected type
        /// </summary>
        public string UnexpectedType { get; }

        /// <summary>
        /// Gets the path to the FHIR property that can provide additional context to this exception
        /// </summary>
        public string FhirPropertyPath { get; }

        private static string CreateDefaultMessage(
            XElement element,
            string unexpectedType,
            string fhirPropertyPath,
            string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                return message;

            var errorMessage = $"The type, '{unexpectedType}', at path '{element.GetAbsolutePath()}' is unexpected";
            if (!string.IsNullOrWhiteSpace(fhirPropertyPath))
                errorMessage += $". FHIR property context: {fhirPropertyPath}";

            return errorMessage;
        }
    }
}
