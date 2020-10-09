using System;
using System.Xml.Linq;
using DarenaSolutions.CCdaToFhirConverter.Extensions;

namespace DarenaSolutions.CCdaToFhirConverter.Exceptions
{
    /// <summary>
    /// An exception that is thrown when a value is required, but the element in the CCDA that contains the value is not
    /// found
    /// </summary>
    public class RequiredValueNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequiredValueNotFoundException"/> class
        /// </summary>
        /// <param name="parent">The parent element that should have contained the desired element</param>
        /// <param name="xPathToRequired">the XPath to the required element that was not found from the context of <paramref name="parent"/></param>
        /// <param name="message">Optionally specify a custom error message if the default error message is not desired</param>
        public RequiredValueNotFoundException(
            XElement parent,
            string xPathToRequired,
            string message = null)
            : base(CreateDefaultMessage(parent, xPathToRequired, message))
        {
            Parent = parent;
            XPathToRequired = xPathToRequired;
        }

        /// <summary>
        /// Gets the parent element that should have contained the desired element
        /// </summary>
        public XElement Parent { get; }

        /// <summary>
        /// Gets the XPath to the required element that was not found from the context of <see cref="Parent"/>
        /// </summary>
        public string XPathToRequired { get; }

        private static string CreateDefaultMessage(XElement parent, string xPathToRequired, string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                return message;

            var fullPath = parent.GetAbsolutePath();
            if (!string.IsNullOrWhiteSpace(xPathToRequired))
            {
                fullPath += xPathToRequired.StartsWith("/") || xPathToRequired.StartsWith("[")
                    ? xPathToRequired
                    : $"/{xPathToRequired}";
            }

            return $"Required value at path '{fullPath}' could not be found";
        }
    }
}
