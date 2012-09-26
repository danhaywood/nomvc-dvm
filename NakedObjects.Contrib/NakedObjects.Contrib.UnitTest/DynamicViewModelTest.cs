using System;
using System.ComponentModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NakedObjects.Contrib.UnitTest
{
    [TestClass]
    public class DynamicViewModelTest
    {
        public TestContext TestContext { get; set; }

        
        [TestMethod]
        public void SketchToFigureOutHowToCallAVarargsMethodReflectively()
        {
            var methodInfo = typeof (DynamicViewModel).GetMethod("ToList", new Type[] {typeof (string[])});
            methodInfo.Should().NotBeNull();
            var invoke = methodInfo.Invoke(null, new object[] {new string[] {"foo", "bar"}});
            var returnVal = invoke as List<string>;
            returnVal.Should().NotBeNull();
            returnVal[0].Should().Be("foo");
        }

        [TestMethod]
        public void BuildType()
        {
            var propertySpecs = new List<DynamicViewModel.PropertySpec>();
            var dvm = DynamicViewModel.Create<MyDynamicViewModel>("MyCompany", "MyCompany.MyDynamicViewModel_2", propertySpecs);
            dvm.GetType().FullName.Should().Be("MyCompany.MyDynamicViewModel_2");

            // there are no additional properties
            var propertyInfo = dvm.GetType().GetProperty("FirstName");
            propertyInfo.Should().BeNull();
        }

        [TestMethod]
        public void BuildTypeWithProperty()
        {
            var propertySpecs = new List<DynamicViewModel.PropertySpec>
                                    {
                                        new DynamicViewModel.PropertySpec
                                            {
                                                Name = "FirstName",
                                                Type = typeof(string),
                                                Value = "Joe"
                                            }
                                    };
            var dvm = DynamicViewModel.Create<MyDynamicViewModel>("MyCompany", "MyCompany.MyDynamicViewModel_1", propertySpecs);

            var propertyInfo = dvm.GetType().GetProperty("FirstName");
            propertyInfo.Should().NotBeNull();
            propertyInfo.GetValue(dvm, new object[] { }).Should().Be("Joe");
        }

        [TestMethod]
        public void BuildTypeWithPropertyThatHasAnnotations()
        {
            var propertySpecs = new List<DynamicViewModel.PropertySpec>
                                    {
                                        new DynamicViewModel.PropertySpec
                                            {
                                                Name = "Comments",
                                                Type = typeof (string),
                                                MemberOrder = 7,
                                                DisplayName = "Comments here",
                                                Mandatory = false,
                                                DescribedAs = "Please enter any comments here",
                                                TypicalLength = 40,
                                                MaxLength = 200,
                                                MultiLineNumberOfLines = 10,
                                                MultiLineWidth = 20,
                                            }
                                    };
            var dvm = DynamicViewModel.Create<MyDynamicViewModel>("MyCompany", "MyCompany.MyDynamicViewModel_4", propertySpecs);

            var propertyInfo = dvm.GetType().GetProperty("Comments");
            propertyInfo.Should().NotBeNull();

            var customAttributes = propertyInfo.GetCustomAttributes(false);

            var memberOrderAttribute = GetAttribute<MemberOrderAttribute>(customAttributes);
            memberOrderAttribute.Should().NotBeNull();
            memberOrderAttribute.Sequence.Should().Be("7");

            var displayNameAttribute = GetAttribute<DisplayNameAttribute>(customAttributes);
            displayNameAttribute.Should().NotBeNull();
            displayNameAttribute.DisplayName.Should().Be("Comments here");

            var optionallyAttribute = GetAttribute<OptionallyAttribute>(customAttributes);
            optionallyAttribute.Should().NotBeNull();

            var describedAsAttribute = GetAttribute<DescribedAsAttribute>(customAttributes);
            describedAsAttribute.Should().NotBeNull();
            describedAsAttribute.Value.Should().Be("Please enter any comments here");

            var typicalLengthAttribute = GetAttribute<TypicalLengthAttribute>(customAttributes);
            typicalLengthAttribute.Should().NotBeNull();
            typicalLengthAttribute.Value.Should().Be(40);

            var maxLengthAttribute = GetAttribute<MaxLengthAttribute>(customAttributes);
            maxLengthAttribute.Should().NotBeNull();
            maxLengthAttribute.Value.Should().Be(200);

            var multiLineAttribute = GetAttribute<MultiLineAttribute>(customAttributes);
            multiLineAttribute.Should().NotBeNull();
            multiLineAttribute.NumberOfLines.Should().Be(10);
            multiLineAttribute.Width.Should().Be(20);
        }

        [TestMethod]
        public void BuildTypeWithPropertyThatMultiLineNumberOfLinesOnly()
        {
            var propertySpecs = new List<DynamicViewModel.PropertySpec>
                                    {
                                        new DynamicViewModel.PropertySpec
                                            {
                                                Name = "Comments",
                                                Type = typeof (string),
                                                MultiLineNumberOfLines = 10
                                            }
                                    };
            var dvm = DynamicViewModel.Create<MyDynamicViewModel>("MyCompany", "MyCompany.MyDynamicViewModel_5", propertySpecs);

            var propertyInfo = dvm.GetType().GetProperty("Comments");
            propertyInfo.Should().NotBeNull();

            var customAttributes = propertyInfo.GetCustomAttributes(false);

            var multiLineAttribute = GetAttribute<MultiLineAttribute>(customAttributes);
            multiLineAttribute.Should().NotBeNull();
            multiLineAttribute.NumberOfLines.Should().Be(10);
            multiLineAttribute.Width.Should().Be(0);
        }

        [TestMethod]
        public void BuildTypeWithPropertyThatMultiLineWidthOnly()
        {
            var propertySpecs = new List<DynamicViewModel.PropertySpec>
                                    {
                                        new DynamicViewModel.PropertySpec
                                            {
                                                Name = "Comments",
                                                Type = typeof (string),
                                                MultiLineWidth = 20,
                                            }
                                    };
            var dvm = DynamicViewModel.Create<MyDynamicViewModel>("MyCompany", "MyCompany.MyDynamicViewModel_6", propertySpecs);

            var propertyInfo = dvm.GetType().GetProperty("Comments");
            propertyInfo.Should().NotBeNull();

            var customAttributes = propertyInfo.GetCustomAttributes(false);

            var multiLineAttribute = GetAttribute<MultiLineAttribute>(customAttributes);
            multiLineAttribute.Should().NotBeNull();
            multiLineAttribute.Width.Should().Be(20);
            multiLineAttribute.NumberOfLines.Should().Be(6); // default
        }

        [TestMethod]
        public void BuildTypeWithPropertyThatHasChoices()
        {
            var propertySpecs = new List<DynamicViewModel.PropertySpec>
                                    {
                                        new DynamicViewModel.PropertySpec
                                            {
                                                Name = "PaymentMethod",
                                                Type = typeof (string),
                                                Choices = new List<string> {"Visa", "Mastercard", "Amex", "PayPal"}
                                            }
                                    };
            var dvm = DynamicViewModel.Create<MyDynamicViewModel>("MyCompany", "MyCompany.MyDynamicViewModel_3", propertySpecs);

            var propertyInfo = dvm.GetType().GetProperty("PaymentMethod");
            propertyInfo.Should().NotBeNull();

            var methodInfo = dvm.GetType().GetMethod("ChoicesPaymentMethod");
            methodInfo.Should().NotBeNull();

            var retval = methodInfo.Invoke(dvm, new object[] {}) as List<string>;
            retval.Should().NotBeNull();
            retval[0].Should().Be("Visa");
            retval[1].Should().Be("Mastercard");
        }


        private static T GetAttribute<T>(IEnumerable<object> customAttributes)
        {
            foreach (var customAttribute in customAttributes)
            {
                if (customAttribute is T)
                {
                    return (T)customAttribute;
                }
            }
            return default(T);
        }
    }

    public class MyDynamicViewModel : DynamicViewModel {}


}
