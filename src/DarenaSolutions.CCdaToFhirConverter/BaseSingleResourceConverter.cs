using System.Xml;
using System.Xml.Linq;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// The base class that all single resource converters must derive from
    /// </summary>
    public abstract class BaseSingleResourceConverter : ISingleResourceConverter
    {
        /// <inheritdoc />
        public Resource AddToBundle(XDocument cCda, ConversionContext context)
        {
            var element = GetPrimaryElement(cCda, context.NamespaceManager);
            if (element == null)
                return null;

            return PerformElementConversion(element, context);
        }

        /// <inheritdoc />
        public virtual Resource AddToBundle(XElement element, ConversionContext context)
        {
            if (element == null)
                return null;

            return PerformElementConversion(element, context);
        }

        /// <summary>
        /// Gets the primary element that will need to be converted to a FHIR resource
        /// </summary>
        /// <param name="cCda">The root CCDA document element</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the root CCDA document</param>
        /// <returns>The primary element that will be converted to a FHIR resource</returns>
        protected abstract XElement GetPrimaryElement(XDocument cCda, XmlNamespaceManager namespaceManager);

        /// <summary>
        /// Performs the actual conversion of an element into the relevant FHIR resource
        /// </summary>
        /// <param name="element">The element to convert</param>
        /// <param name="context">The conversion context that contains necessary data to perform conversion</param>
        /// <returns>The resource that was converted and added to the bundle</returns>
        protected abstract Resource PerformElementConversion(XElement element, ConversionContext context);
    }
}
