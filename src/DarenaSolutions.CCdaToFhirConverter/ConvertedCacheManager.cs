using System.Collections.Generic;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// A cache manager that contains a list of entries that indicate resources that have already been mapped and added
    /// to the bundle. The identifiers are used to determine this existence. EG: If a practitioner resource with identifier
    /// system = http://example.com/npi and identifier value = 123456789 has been added to the bundle, this cache will contain
    /// "Patient|http://example.com/npi|123456789" as an entry. This can then be checked to ensure that the same practitioner
    /// is not added again.
    ///
    /// This cache manager should be newly instantiated whenever <see cref="CCdaToFhirExecutor.Execute"/> is called
    /// </summary>
    public class ConvertedCacheManager
    {
        private readonly Dictionary<string, Resource> _dictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertedCacheManager"/> class
        /// </summary>
        public ConvertedCacheManager()
        {
            _dictionary = new Dictionary<string, Resource>();
        }

        /// <summary>
        /// Adds an entry to the cache
        /// </summary>
        /// <param name="resource">The resource</param>
        /// <param name="identifierSystem">The identifier system</param>
        /// <param name="identifierValue">The identifier value</param>
        public void Add(Resource resource, string identifierSystem, string identifierValue)
        {
            var key = GetKey(resource.ResourceType, identifierSystem, identifierValue);
            if (_dictionary.ContainsKey(key))
                return;

            _dictionary.Add(key, resource);
        }

        /// <summary>
        /// Attempts to retrieve a resource entry if it exists
        /// </summary>
        /// <param name="resourceType">The resource type</param>
        /// <param name="identifierSystem">The identifier system</param>
        /// <param name="identifierValue">The identifier value</param>
        /// <param name="resource">The resource that was retrieved, if it was found</param>
        /// <returns><c>true</c> if the resource entry was found</returns>
        public bool TryGetResource(ResourceType resourceType, string identifierSystem, string identifierValue, out Resource resource)
        {
            var key = GetKey(resourceType, identifierSystem, identifierValue);
            return _dictionary.TryGetValue(key, out resource);
        }

        /// <summary>
        /// Indicates if an entry already exists in the cache. This will determine if a specific resource has already been
        /// converted
        /// </summary>
        /// <param name="resourceType">The resource type</param>
        /// <param name="identifierSystem">The identifier system</param>
        /// <param name="identifierValue">The identifier value</param>
        /// <returns><c>true</c> if the entry already exists in the cache</returns>
        public bool Contains(ResourceType resourceType, string identifierSystem, string identifierValue)
        {
            return _dictionary.ContainsKey(GetKey(resourceType, identifierSystem, identifierValue));
        }

        private string GetKey(ResourceType resourceType, string identifierSystem, string identifierValue)
        {
            return $"{resourceType}|{identifierSystem}|{identifierValue}";
        }
    }
}
