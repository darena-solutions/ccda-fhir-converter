﻿using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// The base converter that all other converters must derive from
    /// </summary>
    public abstract class BaseConverter : IResourceConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        protected BaseConverter(string patientId)
        {
            Resources = new List<Resource>();
            PatientId = patientId;
        }

        /// <inheritdoc />
        public List<Resource> Resources { get; }

        /// <summary>
        /// Gets the id of the patient referenced in the CCDA
        /// </summary>
        protected string PatientId { get; }

        /// <inheritdoc />
        /// <remarks>
        /// This method is used in situations where the elements to be converted are not known, and the converter must find
        /// these elements. The method, <see cref="GetPrimaryElements"/>, is called in this overload which will then lead
        /// to <see cref="PerformElementConversion"/> being called for each element
        /// </remarks>
        public virtual void AddToBundle(
            Bundle bundle,
            XDocument cCda,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            Resources.Clear();

            var elements = GetPrimaryElements(cCda, namespaceManager);
            foreach (var element in elements)
            {
                PerformElementConversion(
                    bundle,
                    element,
                    namespaceManager,
                    cacheManager);
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method is used in situations where the elements have already been retrieved and the converter does not
        /// need to perform finding elements. In this overload, <see cref="GetPrimaryElements"/> IS NOT called and is skipped.
        /// For each element, <see cref="PerformElementConversion"/> will be called
        /// </remarks>
        public void AddToBundle(
            Bundle bundle,
            IEnumerable<XElement> elements,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            Resources.Clear();

            foreach (var element in elements)
            {
                PerformElementConversion(
                    bundle,
                    element,
                    namespaceManager,
                    cacheManager);
            }
        }

        /// <summary>
        /// Retrieves the first resource in <see cref="Resources"/> cast to a specific type
        /// </summary>
        /// <typeparam name="T">The type to cast to</typeparam>
        /// <returns>The first resource in <see cref="Resources"/> cast to the specified type</returns>
        public T GetFirstResourceAsType<T>()
            where T : Resource
        {
            return (T)Resources.First();
        }

        /// <summary>
        /// Retrieves the primary elements that will need to be converted by the converter. This method will be called by
        /// the <c>AddToBundle</c> overload that accepts a single XDocument. This method is used in situations where the
        /// elements to convert are not known beforehand and the converter must find those elements
        /// </summary>
        /// <param name="cCda">The root CCDA document</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the root CCDA document</param>
        /// <returns>The primary elements that will need to be converted by the converter</returns>
        protected abstract IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager);

        /// <summary>
        /// This method is called for each iteration of an element. This is the method that should perform actual conversion
        /// from a CCDA element to a FHIR resource
        /// </summary>
        /// <param name="bundle">The bundle to add the converted resource to as an entry</param>
        /// <param name="element">The element to convert</param>
        /// <param name="namespaceManager">A namespace manager that can be used to further navigate the element</param>
        /// <param name="cacheManager">A cache manager that can be used to determine if a particular resource has already
        /// been converted and added to the bundle. It is up to each implementation to add entries to this cache.</param>
        protected abstract void PerformElementConversion(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager);
    }
}
