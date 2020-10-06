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
        /// Gets the list of resources that were converted and added to the bundle
        /// </summary>
        public List<Resource> Resources { get; }

        /// <summary>
        /// Finds the relevant elements in the root CCDA document and converts them into their FHIR resource representations.
        /// The resources that were converted and added to the bundle will be stored in <see cref="Resources"/>. Use this
        /// method when the converter should choose the elements to convert
        /// </summary>
        /// <param name="bundle">The bundle to add the converted resources to as entries</param>
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
        /// Takes the list of given elements and converts them into their FHIR resource representations. The resources that
        /// were converted and added tot he bundle will be stored in <see cref="Resources"/>. Use this method when the list
        /// of elements have already been retrieved for the converter
        /// </summary>
        /// <param name="bundle">The bundle to add the converted resources to as entries</param>
        /// <param name="elements">The list of elements to convert</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the elements</param>
        /// <param name="cacheManager">A cache manager that can be used to determine if a particular resource has already
        /// been converted and added to the bundle. It is up to each implementation to add entries to this cache.</param>
        void AddToBundle(
            Bundle bundle,
            IEnumerable<XElement> elements,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager);
    }
}
