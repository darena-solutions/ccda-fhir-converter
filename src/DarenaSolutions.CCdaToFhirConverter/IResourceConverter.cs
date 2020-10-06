using System.Xml;
using System.Xml.Linq;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// An interface that contains abstractions for converting CCDA XML elements into their respective FHIR resources
    /// </summary>
    public interface IResourceConverter
    {
        /// <summary>
        /// Gets the resource that was added to the bundle
        /// </summary>
        public Resource Resource { get; }

        /// <summary>
        /// Converts an elements into its FHIR resource representation. The converted resource is then added as an entry
        /// to the given bundle
        /// </summary>
        /// <param name="bundle">The bundle to add the converted resource to as an entry</param>
        /// <param name="element">The element to convert to a FHIR resource</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the element</param>
        /// <param name="cacheManager">A cache manager that can be used to determine if a particular resource has already
        /// been converted and added to the bundle. It is up to each implementation to add entries to this cache.</param>
        void AddToBundle(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager);
    }
}
