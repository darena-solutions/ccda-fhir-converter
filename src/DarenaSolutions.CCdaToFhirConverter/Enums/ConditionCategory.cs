namespace DarenaSolutions.CCdaToFhirConverter.Enums
{
    /// <summary>
    /// US Core Condition Category codes according to valueset,
    /// http://terminology.hl7.org/CodeSystem/condition-category
    /// </summary>
    public enum ConditionCategory
    {
        /// <summary>
        /// No applicable concept in value set
        /// </summary>
        Extensible,

        /// <summary>
        /// An item on a problem list that can be managed over time and
        /// can be expressed by a practitioner (e.g. physician, nurse), patient, or related person.
        /// </summary>
        ProblemList,

        /// <summary>
        /// A point in time diagnosis (e.g. from a physician or nurse) in context of an encounter.
        /// </summary>
        EncounterDiagnosis,

        /// <summary>
        /// Additional health concerns from other stakeholders which are outside the provider’s problem list.
        /// </summary>
        HealthConcern
    }
}
