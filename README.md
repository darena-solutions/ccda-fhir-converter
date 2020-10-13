# Installation
`PM> Install-Package DarenaSolutions.CCdaToFhirConverter`

# About
This is a small mapping library that will take in a CCDA and convert it to a bundle of FHIR resources. Currently, the default
implementation attempts to map values from the CCDA such that it conforms to USCDI and US Core. However, the library is
built to be extendible and customizable. Users of the library can customize the library to fit any other scenario that they
need.

# General Structure
To map a CCDA to FHIR resources, the library contains several converters that perform this mapping. Each converter takes
a specific set of elements in the CCDA and converts it into its primary FHIR resource. Ideally, each converter manages only
one type of FHIR resource. If a converter requires a FHIR resource it does not handle, it should create/instantiate the
converter that does handle that resource and use the resource that was mapped by that converter. This will be clarified
in a later section.

The `CCdaToFhirExecutor` is the main class that contains a collection of all the converters. Once the executor is started,
the executor will iterate over all converters and execute them. The order of execution uses the basic FIFO method. However,
it is strongly recommended that converters are developed in such a way that the converter is not dependent in the order
of execution.

# Quick Start
To begin, an instance of the `CCdaToFhirExecutor` will need to be created.

```csharp
var executor = new CCdaToFhirExecutor();
```

You must also have an `XDocument` reference to the CCDA that you want to map.

```csharp
var cCda = XDocument.Parse(cCdaText); // or XDocument.Load();
```

Once you have these instances, you simply call the following to obtain the FHIR bundle:

```csharp
var bundle = executor.Execute(cCda);
```

And that's all there is to it. The bundle that is generated is a model from the dependent library `Hl7.Fhir.R4`. If you
would like to understand the dependent library, [head on over to their github page](https://github.com/FirelyTeam/fhir-net-api).

# Customization
We understand that our default mapping implementation may not cover all use-cases. There may be situations where you would
like to map an additional CCDA element into a FHIR resource that is not provided in the default implementation (though we
do recommend that you let us know. We may want to add that into the library as a default implementation!), you might want
to add additional converted fields in an exsiting converter, maybe you want to remove a converter entirely, etc.

The library is built to be customizable to better fit business needs.

## Converters
All converters must implement `IResourceConverter`. The interface contains two methods that must be implemented, which
are overloads. The first method takes in the CCDA `XDocument` object. In this method the converter will search the CCDA
document and obtain the necessary elements it would like to convert.

The second method takes in an `IEnumerable<XElement>` object. This method is used in situations where the elements have
already been retrieved by some earlier process and the converter should just use those elements instead of trying to search
in the CCDA. This is helpful in edge-cases where the retrieval of elements may be different from the primary way of retrieving
those elements.

We have created a `BaseConverter` class that implements this interface to make it a little easier to get started on a
converter. This base class is an abstract class and converters that derive from this class must implement those methods.
Essentially, the base class requires that the converter implement a `GetPrimaryElements` and implement a `PerformElementConversion`
method.

The `GetPrimaryElements` will be called to retrieve the CCDA elements to convert. Each element will then be passed onto
`PerformElementConversion`.

The `PerformElementConversion` does the actual conversion from an element into a FHIR resource. Below is an example
converter:

```csharp
public class MyConverter : BaseConverter
{
    protected override IEnumerable<XElement> GetPrimaryElements(XDocument cCda, XmlNamespaceManager namespaceManager)
    {
        // Here you retrieve the main elements (the xpath is an example, doesn't actually retrieve anything)
        var xPath = "//n1:section/n1:code[@code='123456']/..";
        return cCda.XPathSelectElements(xPath, namespaceManager);
    }

    protected override Resource PerformElementConversion(
        Bundle bundle,
        XElement element,
        XmlNamespaceManager namespaceManager,
        Dictionary<string, Resource> cache)
    {
        // Here you perform the actual conversion to the FHIR resource
        var myResource = new Basic
        {
            Id = Guid.NewGuid().ToString()
        };

        myResource.Code = element
            .Element(Defaults.DefaultNs + "someElement")?
            .ToCodeableConcept(); 
        // .ToCodeableConcept above is an extension method. There are a lot of helpful extension methods
        // in XElementExtensions, have a look!

        bundle.Entry.Add(new Bundle.EntryComponent
        {
            FullUrl = $"urn:uuid:{myResource.Id}",
            Resource = myResource
        });

        return myResource;
    }
}
```

The `PerformElementConversion` is an abstract method which means that any derived converter can override the default
behaviour. This is true for all of the default converters in this library. If the conversion doesn't satisfy a use-case,
you can override the method and implement your own conversion.

## Executor
All converters must be added to the executor, which maintains a collection of these converters. When you create an instance
of this executor using the default constructor, the default converters are added for you and nothing else needs to be done.
However, if you want to add your own converter, you can use the following method to add the converter used in the previous
section:

```csharp
var executor = new CCdaToFhrExecutor();
executor.ReplaceConverter<MyConverter>();
```

The `ReplaceConverter` will add the converter if it doesn't already exist in the collection, or it'll replace it if one
is found.

If you don't want to add the default converters, you can pass an argument to the optional parameter:
```csharp
var executor = new CCdaToFhirExecutor(addDefaultConverters: false);
```

Additionally the executor does require a patient and organization converter as the first two converters to execute. This
is important because in almost all situations, all converters require this context in one way or another. These converters
implement `ISingleResourceConverter`. You can pass these into the executor constructor:

```csharp
var executor = new CCdaToFhirExecutor(
    new MyCustomOrganizationConverter(),
    new MyCustomPatientConverter());
```

# Improvement
If you recognize an incorrect mapping, want to include an additional converter, improve the documentation or this readme
file, or just any general question, please raise an issue and we'll look into it right away!