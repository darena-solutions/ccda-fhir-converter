using System.Collections.Generic;
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
        /// <param name="cCda">The root CCDA document element</param>
        /// <param name="context">The conversion context that contains necessary data to perform conversion</param>
        /// <returns>The list of resources that were converted and added to the bundle</returns>
        List<Resource> AddToBundle(XDocument cCda, ConversionContext context);

        /// <summary>
        /// Takes the list of given elements and converts them into their FHIR resource representations. The resources that
        /// Use this method when the list of elements have already been retrieved for the converter
        /// </summary>
        /// <param name="elements">The list of elements to convert</param>
        /// <param name="context">The conversion context that contains necessary data to perform conversion</param>
        /// <returns>The list of resources that were converted and added to the bundle</returns>
        List<Resource> AddToBundle(IEnumerable<XElement> elements, ConversionContext context);
    }
}
