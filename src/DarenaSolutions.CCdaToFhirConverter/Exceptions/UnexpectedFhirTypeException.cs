using System;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Extensions;

namespace DarenaSolutions.CCdaToFhirConverter.Exceptions
{
    /// <summary>
    /// An exception that is thrown when a 'value' element is expected to be a certain type, but is not the expected type
    /// </summary>
    public class UnexpectedFhirTypeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedFhirTypeException"/> class
        /// </summary>
        /// <param name="element">The 'value' element</param>
        /// <param name="unexpectedType">The unexpected type</param>
        /// <param name="message">Optionally specify a custom error message if the default error message is not desired</param>
        public UnexpectedFhirTypeException(
            XElement element,
            string unexpectedType,
            string message = null)
            : base(CreateDefaultMessage(element, unexpectedType, message))
        {
            Element = element;
            UnexpectedType = unexpectedType;
        }

        /// <summary>
        /// Gets the 'value' element
        /// </summary>
        public XElement Element { get; }

        /// <summary>
        /// Gets the unexpected type
        /// </summary>
        public string UnexpectedType { get; }

        private static string CreateDefaultMessage(
            XElement element,
            string unexpectedType,
            string message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? $"The type, '{unexpectedType}', at path '{element.GetAbsolutePath()}' is unexpected"
                : message;
        }
    }
}
