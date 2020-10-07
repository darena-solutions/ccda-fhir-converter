using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// An interface that contains abstractions for converting CCDA XML elements into their respective FHIR resource representations
    /// </summary>
    public interface IResourceConverter
    {
        /// <summary>
        /// Finds the relevant elements in the root CCDA document and converts them into their FHIR resource representations.
        /// Use this method when the converter should choose the elements to convert
        /// </summary>
        /// <param name="bundle">The bundle to add the converted resources to as entries</param>
        /// <param name="cCda">The root CCDA document element</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the root CCDA document</param>
        /// <param name="cacheManager">A cache manager that can be used to determine if a particular resource has already
        /// been converted and added to the bundle. It is up to each implementation to add entries to this cache.</param>
        /// <returns>The list of resources that were converted and added to the bundle</returns>
        List<Resource> AddToBundle(
            Bundle bundle,
            XDocument cCda,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager);

        /// <summary>
        /// Takes the list of given elements and converts them into their FHIR resource representations. The resources that
        /// Use this method when the list of elements have already been retrieved for the converter
        /// </summary>
        /// <param name="bundle">The bundle to add the converted resources to as entries</param>
        /// <param name="elements">The list of elements to convert</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the elements</param>
        /// <param name="cacheManager">A cache manager that can be used to determine if a particular resource has already
        /// been converted and added to the bundle. It is up to each implementation to add entries to this cache.</param>
        /// <returns>The list of resources that were converted and added to the bundle</returns>
        List<Resource> AddToBundle(
            Bundle bundle,
            IEnumerable<XElement> elements,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager);
    }
}
