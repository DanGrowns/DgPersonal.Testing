using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using Mama.XUnitTests.Enums;

namespace DgPersonal.Testing.Classes
{
    public static class DomainBoundaryTester
    {
        private static PropertyInfo PropertyAccessTests<TModel, TProperty>(
            Expression<Func<TModel, TProperty>> expression,
            TModel model, 
            SetExpect expectation)
        {
            var exprBody = (MemberExpression) expression.Body;
            var property = model.GetType().GetProperty(exprBody.Member.Name);
            
            var setMethod = property?.SetMethod;
            if (expectation == SetExpect.None)
            {
                setMethod.Should().BeNull();
                return property;
            }

            setMethod.Should().NotBeNull();
            Debug.Assert(setMethod != null, nameof(setMethod) + " != null");
            
            switch (expectation)
            {
                case SetExpect.Normal:
                case SetExpect.Init:
                    setMethod.IsPublic.Should().BeTrue();
                    
                    var setMethodReturnParameterModifiers = setMethod.ReturnParameter?.GetRequiredCustomModifiers();
                    var isInit = setMethodReturnParameterModifiers?.Contains(typeof(System.Runtime.CompilerServices.IsExternalInit));
                    
                    if (expectation == SetExpect.Init)
                        isInit.Should().BeTrue();
                    else
                        isInit.Should().BeFalse();
                    break;

                case SetExpect.Protected:
                    setMethod.IsFamily.Should().BeTrue();
                    setMethod.IsFamilyOrAssembly.Should().BeFalse();
                    break;
                
                case SetExpect.ProtectedInternal:
                    setMethod.IsFamily.Should().BeFalse();
                    setMethod.IsFamilyOrAssembly.Should().BeTrue();
                    break;
            }

            return property;
        }

        private static void ValueTest<TModel, TProperty>(TModel model, PropertyInfo property, TProperty value)
        {
            var getMethod = property?.GetMethod;
            if (getMethod != null)
            {
                var getResult = property.GetValue(model);
                getResult.Should().BeEquivalentTo(value);
            }
        }
        
        public static void TestProperty<TModel, TProperty>(this TModel model, 
            Expression<Func<TModel, TProperty>> expression, 
            TProperty value,
            SetExpect expectation)
        {
            var property = PropertyAccessTests(expression, model, expectation);
            ValueTest(model, property, value);
        }
        
        public static void TestProperty<TModel, TProperty>(this TModel model, 
            Expression<Func<TModel, TProperty>> expression,
            SetExpect expectation)
            => PropertyAccessTests(expression, model, expectation);
    }
}