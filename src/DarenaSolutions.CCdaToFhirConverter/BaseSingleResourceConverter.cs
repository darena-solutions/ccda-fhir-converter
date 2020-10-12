using System.Collections.Generic;
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
        public Resource AddToBundle(
            Bundle bundle,
            XDocument cCda,
            XmlNamespaceManager namespaceManager,
            Dictionary<string, Resource> cache)
        {
            var element = GetPrimaryElement(cCda, namespaceManager);
            if (element == null)
                return null;

            return PerformElementConversion(
                bundle,
                element,
                namespaceManager,
                cache);
        }

        /// <inheritdoc />
        public virtual Resource AddToBundle(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            Dictionary<string, Resource> cache)
        {
            if (element == null)
                return null;

            return PerformElementConversion(
                bundle,
                element,
                namespaceManager,
                cache);
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
        /// <param name="bundle">The bundle to add the converted resource to as an entry</param>
        /// <param name="element">The element to convert</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the element</param>
        /// <param name="cache">A cache that can be used to determine if a particular resource has already been converted
        /// and added to the bundle. It is up to each implementation to add entries to this cache.</param>
        /// <returns>The resource that was converted and added to the bundle</returns>
        protected abstract Resource PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            Dictionary<string, Resource> cache);
    }
}
