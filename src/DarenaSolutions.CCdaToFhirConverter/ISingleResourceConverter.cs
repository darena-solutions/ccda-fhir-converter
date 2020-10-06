using System.Xml;
using System.Xml.Linq;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// An interface that contains abstractions for converting a single CCDA element into its respective FHIR resource representation
    /// </summary>
    public interface ISingleResourceConverter
    {
        /// <summary>
        /// Gets the resource that was converted and added to the bundle
        /// </summary>
        public Resource Resource { get; }

        /// <summary>
        /// Finds the relevant element in the root CCDA document and converts it into its FHIR resource representation.
        /// The resource that was converted and added to the bundle will be stored in <see cref="Resource"/>. Use this method
        /// when the converter should choose the element to convert
        /// </summary>
        /// <param name="bundle">The bundle to add the converted resource to as an entry</param>
        /// <param name="cCda">The root CCDA document element</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the root CCDA document</param>
        /// <param name="cacheManager">A cache manager that can be used to determine if a particular resource has already
        /// been converted and added to the bundle. It is up to each implementation to add entries to this cache.</param>
        void AddToBundle(
            Bundle bundle,
            XDocument cCda,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager);

        /// <summary>
        /// Takes the given element and converts it into its FHIR resource representation. The resource that was converted
        /// and added to the bundle will be stored in <see cref="Resource"/>. Use this method when the element to convert
        /// has already been retrieved for the converter
        /// </summary>
        /// <param name="bundle">The bundle to add the converted resource to as an entry</param>
        /// <param name="element">The element to convert</param>
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
