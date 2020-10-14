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
        /// Finds the relevant element in the root CCDA document and converts it into its FHIR resource representation.
        /// The resource that was converted and added to the bundle will be stored in <see cref="Resource"/>. Use this method
        /// when the converter should choose the element to convert
        /// </summary>
        /// <param name="cCda">The root CCDA document element</param>
        /// <param name="context">The conversion context that contains necessary data to perform conversion</param>
        /// <returns>The resource that was converted and added to the bundle</returns>
        Resource AddToBundle(XDocument cCda, ConversionContext context);

        /// <summary>
        /// Takes the given element and converts it into its FHIR resource representation. The resource that was converted
        /// and added to the bundle will be stored in <see cref="Resource"/>. Use this method when the element to convert
        /// has already been retrieved for the converter
        /// </summary>
        /// <param name="element">The element to convert</param>
        /// <param name="context">The conversion context that contains necessary data to perform conversion</param>
        /// <returns>The resource that was converted and added to the bundle</returns>
        Resource AddToBundle(XElement element, ConversionContext context);
    }
}
