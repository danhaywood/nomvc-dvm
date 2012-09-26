## What is this? ##

This is a contribution to [Naked Objects MVC](http://nakedobjects.codeplex.com) to provide support for dynamic view models.

## Background ##

Naked Objects MVC automatically renders domain objects within an ASP.NET MVC app.  Typically these domain objects are persisted, and their structure is defined at compile time.

However, NO MVC can also render non-persisted (transient) objects, moreover the `System.Type` of these objects can be generated at runtime.  This capability enables NO MVC to support data-driven view models.

## Dynamic View Models ##

The `DynamicViewModel` class is both the supertype for all dynamically created subtypes, and also provides a factory method to instantiate the type.  This factory method takes a list of `PropertySpec`s which specify the properties of the type.

For example, the following specifies the list of properties for such a type:

<pre>
var propertySpecs = new List<DynamicViewModel.PropertySpec>
  {
      new DynamicViewModel.PropertySpec
      {
          Name = "FirstName",
          Type = typeof(string),
          MemberOrder = 1,
          TypicalLength = 10,
          MaxLength = 20
      },
      new DynamicViewModel.PropertySpec
      {
          Name = "LastName",
          Type = typeof(string),
          MemberOrder = 2,
          TypicalLength = 10,
          MaxLength = 20
      },
      new DynamicViewModel.PropertySpec
      {
          Name = "DateOfBirth",
          Type = typeof(DateTime),
          MemberOrder = 3
      },
      new DynamicViewModel.PropertySpec
      {
          Name = "PreferredPaymentMethod",
          Type = typeof(string),
          MemberOrder = 4,
          MaxLength = 10,
          Choices = new List<string> {"Visa", "Mastercard", "Amex", "PayPal"}
          Value = "Visa"
      },
      new DynamicViewModel.PropertySpec
      {
          Name = "Notes",
          Type = typeof(string),
          MemberOrder = 5,
          Mandatory = false,
          MaxLength = 200,
          MultiLineNumberOfLines = 5,
          MultiLineWidth = 40
      },
  };
</pre>


The object is then instantiated using:
<pre>
var dvm = DynamicViewModel.Create<MyDynamicViewModel>(
             "MyCompany", "MyCompany.MyDynamicViewModel_1", propertySpecs);
</pre>

where `MyDynamicViewModel` is any subtype of `DynamicViewModel` (and so can contain any custom business logic common to all dynamic view models).

The above code defines a class equivalent to:
<pre>
public class MyDynamicViewModel_1 : MyDynamicViewModel
{
    [MemberOrder("1"), TypicalLength(10), MaxLength(20)]
    public string FirstName {get; set;}
    
    [MemberOrder("2"), TypicalLength(10), MaxLength(20)]
    public string LastName {get; set;}

    [MemberOrder("3")]
    public DateTime DateOfBirth {get; set;}

    [MemberOrder("4"), MaxLength(10)]
    public string PreferredPaymentMethod {get; set;}
    public List<string> ChoicesPreferredPaymentMethod
    {
        return ToList("Visa", "Mastercard", "Amex", "PayPal");
    }

    [MemberOrder("5"), Optionally, MaxLength(200), MultiLine(NumberOfLines=5, Width=40)]
    public string Notes {get; set;}
}
</pre>

Any `PropertySpec`s that define a non-null `Value` will be used to initialize the appropriate property of the `dvm` instance.

This object can then be returned, eg as the result of an action.  No MVC does not care that the type is created dynamically at runtime; it merely builds its metamodel for the type and renders the instance as normal.
