using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <inheritdoc />
    public class StatusObservationConverter : BaseObservationConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusObservationConverter"/> class
        /// </summary>
        /// <param name="patientId">The id of the patient referenced in the CCDA</param>
        public StatusObservationConverter(string patientId)
            : base(patientId)
        {
        }

        /// <inheritdoc />
        public override void AddToBundle(
            Bundle bundle,
            XElement element,
            XmlNamespaceManager namespaceManager,
            ConvertedCacheManager cacheManager)
        {
            base.AddToBundle(bundle, element, namespaceManager, cacheManager);

            var observation = (Observation)Resource;
            observation
                .Category
                .First()
                .Coding
                .First()
                .Code = "exam";
        }
    }
}
