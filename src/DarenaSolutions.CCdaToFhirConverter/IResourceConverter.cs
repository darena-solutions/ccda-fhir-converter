using System.Collections.Generic;
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
        /// Converts a list of elements into their FHIR resource representation. The converted resources are then added
        /// as entries to a given bundle
        /// </summary>
        /// <param name="bundle">The bundle to add the converted resources to as entries</param>
        /// <param name="elements">The list of elements to convert to FHIR resources</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the list of elements</param>
        /// <param name="cacheManager">A cache manager that can be used to determine if a particular resource has already
        /// been converted and added to the bundle. It is up to each implementation to add entries to this cache.</param>
        void AddToBundle(
            Bundle bundle,
            IEnumerable<XElement> elements,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager);
    }
}
