// -------------------------------------------------------------------------------------------------------------------------
// <copyright file="ConversionContext.cs" company="Darena Solutions LLC">
// Copyright (c) Darena Solutions LLC. All rights reserved.
// Licensed under the Apache License Version 2.0. See LICENSE file in the project root for full license information.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using Hl7.Fhir.Model;

namespace DarenaSolutions.CCdaToFhirConverter
{
    /// <summary>
    /// Class that contains the data necessary to perform CCDA element conversion into FHIR resources
    /// </summary>
    public class ConversionContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConversionContext"/> class
        /// </summary>
        /// <param name="bundle">The bundle to add converted resources to as entries</param>
        /// <param name="cCda">The original CCDA document</param>
        /// <param name="namespaceManager">The namespace manager that can be used to further navigate XML elements</param>
        public ConversionContext(Bundle bundle, XDocument cCda, XmlNamespaceManager namespaceManager)
        {
            Bundle = bundle;
            CCda = cCda;
            NamespaceManager = namespaceManager;
            Cache = new Dictionary<string, Resource>();
            Exceptions = new List<Exception>();
        }

        /// <summary>
        /// Gets the bundle to add converted resources to as entries
        /// </summary>
        public Bundle Bundle { get; }

        /// <summary>
        /// Gets the original CCDA document
        /// </summary>
        public XDocument CCda { get; }

        /// <summary>
        /// Gets the namespace manager that can be used to further navigate XML elements
        /// </summary>
        public XmlNamespaceManager NamespaceManager { get; }

        /// <summary>
        /// Gets a cache that can be used to determine if a particular resource has already been converted and added to
        /// the bundle. It is up to each resource converter implementation to add entries to this cache
        /// </summary>
        public Dictionary<string, Resource> Cache { get; }

        /// <summary>
        /// Gets a list of exceptions that occurred during conversion. It is recommended that each resource converter when
        /// coming across an exception add to this list rather than throw right away. This way a collection of exceptions
        /// can be thrown at the end of the process when all converters have executed. These exceptions will be thrown as
        /// an <see cref="AggregateException"/> exception after all converters have processed
        /// </summary>
        public List<Exception> Exceptions { get; }
    }
}
