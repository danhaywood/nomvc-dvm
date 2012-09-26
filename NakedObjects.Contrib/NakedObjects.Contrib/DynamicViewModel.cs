using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NakedObjects;

namespace NakedObjects.Contrib
{
    [NotPersisted]
    public abstract class DynamicViewModel
    {
        public class PropertySpec
        {
            public string Name { get; set; }
            public Type Type { get; set; }
            public string DisplayName { get; set; }
            public double MemberOrder { get; set; }
            public string DescribedAs { get; set; }

            public bool Mandatory { get; set; }
            public int? TypicalLength { get; set; }
            public int? MaxLength { get; set; }
            public int? MultiLineNumberOfLines { get; set; }
            public int? MultiLineWidth { get; set; }

            public object Value { get; set; }

            public List<string> Choices { get; set; }
        }

        private static readonly ConstructorInfo DisplayNameAttribute = typeof(DisplayNameAttribute).GetConstructor(new[] { typeof(string) });
        private static readonly ConstructorInfo MemberOrderAttribute = typeof(MemberOrderAttribute).GetConstructor(new[] { typeof(double) });
        private static readonly ConstructorInfo OptionallyAttribute = typeof(OptionallyAttribute).GetConstructor(new Type[0]);
        private static readonly ConstructorInfo DescribedAsAttribute = typeof(DescribedAsAttribute).GetConstructor(new[] { typeof(string) });
        private static readonly ConstructorInfo TypicalLengthAttribute = typeof(TypicalLengthAttribute).GetConstructor(new[] { typeof(int) });
        private static readonly ConstructorInfo MaxLengthAttribute = typeof(MaxLengthAttribute).GetConstructor(new[] { typeof(int) });
        private static readonly ConstructorInfo MultiLineAttributeCi = typeof(MultiLineAttribute).GetConstructor(new Type[0]);
        private static readonly PropertyInfo MultiLineAttributePiNumberOfLines = typeof(MultiLineAttribute).GetProperty("NumberOfLines");
        private static readonly PropertyInfo MultiLineAttributePiWidth = typeof(MultiLineAttribute).GetProperty("Width");

        private static ModuleBuilder ModuleBuilder { get; set; }

        public static T Create<T>(string assemblyName, string typeName, List<PropertySpec> propertySpecs) where T : DynamicViewModel
        {
            EnsureModuleBuilderExists(assemblyName);

            var dynamicType = ObtainType<T>(typeName, propertySpecs);
            var newRep = InstantiateAndInitializeInstance<T>(dynamicType, propertySpecs);

            return (T)newRep;
        }

        private static object InstantiateAndInitializeInstance<T>(Type dynamicType, IEnumerable<PropertySpec> propertySpecs)
            where T : DynamicViewModel
        {
            var instance = Activator.CreateInstance(dynamicType);
            foreach (var propertySpec in propertySpecs)
            {
                if (propertySpec.Value == null)
                {
                    continue;
                }

                PropertyInfo propertyInfo = null;
                while (propertyInfo == null)
                {
                    // very odd; seems that the first time we ask, get back a null.
                    propertyInfo = dynamicType.GetProperty(propertySpec.Name);
                }
                propertyInfo.SetValue(instance, propertySpec.Value, null);
            }
            return instance;
        }

        private static Type ObtainType<T>(string typeName, List<PropertySpec> propertySpecs) where T : DynamicViewModel
        {
            var proxyType = ModuleBuilder.GetType(typeName);

            if (proxyType != null)
            {
                return proxyType;
            }
            var typeBuilder = ModuleBuilder.DefineType(typeName,
                                                       TypeAttributes.Public | TypeAttributes.Class |
                                                       TypeAttributes.Sealed,
                                                       typeof(T));
            foreach (var propertySpec in propertySpecs)
            {
                AppendProperty(typeBuilder, propertySpec);
            }

            var dynamicType = typeBuilder.CreateType();

            foreach (var propertySpec in propertySpecs)
            {
                PropertyInfo propertyInfo = null;
                while (propertyInfo == null)
                {
                    // very odd; seems that the first time we ask, get back a null.
                    propertyInfo = dynamicType.GetProperty(propertySpec.Name);
                }
            }
            return dynamicType;
        }

        private static void EnsureModuleBuilderExists(string assemblyName)
        {
            if (ModuleBuilder != null)
            {
                return;
            }
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
            ModuleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName);
        }

        private static void AppendProperty(TypeBuilder typeBuilder, PropertySpec propertySpec)
        {
            var propertyType = propertySpec.Type;
            var name = propertySpec.Name;

            var fieldBuilder = typeBuilder.DefineField("_" + name, propertyType, FieldAttributes.Private);

            var propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.None, propertyType, Type.EmptyTypes);

            var getMethodBuilder = typeBuilder.DefineMethod("get_" + name, MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Public, propertyType, Type.EmptyTypes);
            var iLGenerator = getMethodBuilder.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
            iLGenerator.Emit(OpCodes.Ret);

            var setMethodBuilder = typeBuilder.DefineMethod("set_" + name, MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Public, null, new[] { propertyType });

            iLGenerator = setMethodBuilder.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Ldarg_1);
            iLGenerator.Emit(OpCodes.Stfld, fieldBuilder);
            iLGenerator.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getMethodBuilder);
            propertyBuilder.SetSetMethod(setMethodBuilder);

            propertyBuilder.SetCustomAttribute(new CustomAttributeBuilder(DisplayNameAttribute, new object[] { propertySpec.DisplayName }));
            propertyBuilder.SetCustomAttribute(new CustomAttributeBuilder(MemberOrderAttribute, new object[] { propertySpec.MemberOrder }));

            if (propertySpec.DescribedAs != null)
            {
                propertyBuilder.SetCustomAttribute(new CustomAttributeBuilder(DescribedAsAttribute, new object[] { propertySpec.DescribedAs }));    
            }

            if (!propertySpec.Mandatory)
            {
                propertyBuilder.SetCustomAttribute(new CustomAttributeBuilder(OptionallyAttribute, new object[0]));
            }

            if (propertySpec.Type == typeof(string))
            {
                if (propertySpec.TypicalLength != null)
                {
                    propertyBuilder.SetCustomAttribute(new CustomAttributeBuilder(TypicalLengthAttribute, new object[] { propertySpec.TypicalLength }));
                }

                if (propertySpec.MaxLength != null)
                {
                    propertyBuilder.SetCustomAttribute(new CustomAttributeBuilder(MaxLengthAttribute, new object[] { propertySpec.MaxLength }));
                }

                if (propertySpec.MultiLineNumberOfLines != null && propertySpec.MultiLineWidth != null)
                {
                    propertyBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                                                           MultiLineAttributeCi, new object[0], 
                                                           new []{MultiLineAttributePiNumberOfLines, MultiLineAttributePiWidth}, 
                                                           new object[]{propertySpec.MultiLineNumberOfLines, propertySpec.MultiLineWidth}));
                }

                if (propertySpec.MultiLineNumberOfLines != null && propertySpec.MultiLineWidth == null)
                {
                    propertyBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                                                           MultiLineAttributeCi, new object[0],
                                                           new[] {MultiLineAttributePiNumberOfLines},
                                                           new object[] {propertySpec.MultiLineNumberOfLines}));
                }

                if (propertySpec.MultiLineNumberOfLines == null && propertySpec.MultiLineWidth != null)
                {
                    propertyBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                                                           MultiLineAttributeCi, new object[0],
                                                           new [] { MultiLineAttributePiWidth },
                                                           new object[] { propertySpec.MultiLineWidth }));
                }

                if (propertySpec.Choices != null)
                {
                    var choicesMethodBuilder = typeBuilder.DefineMethod(
                        "Choices" + name, 
                        MethodAttributes.Public,
                        typeof (string[]), Type.EmptyTypes);

                    iLGenerator = choicesMethodBuilder.GetILGenerator();

                    iLGenerator.DeclareLocal(typeof (List<string>));
                    iLGenerator.DeclareLocal(typeof (string[]));

                    iLGenerator.Emit(OpCodes.Nop);
                    iLGenerator.Emit(OpCodes.Ldc_I4, propertySpec.Choices.Count);
                    iLGenerator.Emit(OpCodes.Newarr, typeof (string));
                    iLGenerator.Emit(OpCodes.Stloc_1);

                    var i = 0;
                    foreach (var choice in propertySpec.Choices)
                    {
                        iLGenerator.Emit(OpCodes.Ldloc_1);
                        iLGenerator.Emit(OpCodes.Ldc_I4, i++);
                        iLGenerator.Emit(OpCodes.Ldstr, choice);
                        iLGenerator.Emit(OpCodes.Stelem_Ref);
                    }

                    iLGenerator.Emit(OpCodes.Ldloc_1);

                    var dvmToListMethod = typeof(DynamicViewModel).GetMethod("ToList", new Type[] { typeof(string[]) });
                    iLGenerator.Emit(OpCodes.Call, dvmToListMethod);

                    iLGenerator.Emit(OpCodes.Stloc_0);
                    iLGenerator.Emit(OpCodes.Ldloc_0);
                    iLGenerator.Emit(OpCodes.Ret);
                }
            }
        }

        public static List<string> ToList(params string[] strings)
        {
            return strings.ToList();
        }
    }
}