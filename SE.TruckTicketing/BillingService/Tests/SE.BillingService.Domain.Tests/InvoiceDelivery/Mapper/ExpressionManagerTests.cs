using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.BillingService.Domain.InvoiceDelivery.Mapper;

using Trident.Testing.TestScopes;

namespace SE.BillingService.Domain.Tests.InvoiceDelivery.Mapper;

[TestClass]
public class ExpressionManagerTests
{
    [TestMethod("ExpressionManager should be able to compile expressions.")]
    public void ExpressionManager_CompileExpressions_Basic()
    {
        // arrange
        var scope = new DefaultScope();
        var (moduleId, expressions, _) = scope.GetSampleModule();

        // act
        var assembly = scope.InstanceUnderTest.CompileExpressions(moduleId, expressions);

        // assert
        assembly.Should().NotBeNull();
        assembly.GetType("MapperExpressions").Should().NotBeNull();
        assembly.GetType("MapperExpressions")!.GetProperty($"exp_{expressions.First().Key:N}", BindingFlags.Public | BindingFlags.Static).Should().NotBeNull();
    }

    [TestMethod("ExpressionManager should throw an exception with details when compilation fails.")]
    public void ExpressionManager_CompileExpressions_FailedToCompile()
    {
        // arrange
        var scope = new DefaultScope();
        var (moduleId, expressions, _) = scope.GetSampleModule();
        expressions[expressions.First().Key] = "!";

        // act
        try
        {
            scope.InstanceUnderTest.CompileExpressions(moduleId, expressions);
        }
        catch (Exception x)
        {
            // assert
            x.Should().BeOfType<InvalidOperationException>();
            x.Message.Should().Contain("CS1525:");
            return;
        }

        Assert.Fail("ExpressionManager should throw an exception.");
    }

    [TestMethod("ExpressionManager should be able to cache the module.")]
    public void ExpressionManager_CompileExpressions_CanCache()
    {
        // arrange
        var scope = new DefaultScope();
        var (moduleId, expressions, _) = scope.GetSampleModule();
        var assembly = scope.InstanceUnderTest.CompileExpressions(moduleId, expressions);

        // act
        var anotherAssembly = scope.InstanceUnderTest.CompileExpressions(moduleId, expressions);

        // assert
        anotherAssembly.Should().BeSameAs(assembly);
    }

    [TestMethod("ExpressionManager should be able to fetch the existing expression.")]
    public void ExpressionManager_GetExpression_Basic()
    {
        // arrange
        var scope = new DefaultScope();
        var (moduleId, expressions, _) = scope.GetSampleModule();
        var assembly = scope.InstanceUnderTest.CompileExpressions(moduleId, expressions);

        // act
        var expression = scope.InstanceUnderTest.GetExpression(assembly, expressions.First().Key);

        // assert
        expression.Should().NotBeNull();
    }

    [TestMethod("ExpressionManager should be able to compile and execute expressions.")]
    public void ExpressionManager_EndToEnd_Basic()
    {
        // arrange
        var scope = new DefaultScope();
        var (moduleId, expressions, expectedValue) = scope.GetSampleModule();

        // act
        var assembly = scope.InstanceUnderTest.CompileExpressions(moduleId, expressions);
        var expression = scope.InstanceUnderTest.GetExpression(assembly, expressions.First().Key);
        var output = expression(new(), new());

        // assert
        output.Should().BeOfType<string>();
        var stringOutput = output as string;
        stringOutput.Should().Be(expectedValue);
    }

    private class DefaultScope : TestScope<ExpressionManager>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(new DotNetCompiler());
        }

        public (string moduleName, Dictionary<Guid, string> expressions, string expectedValue) GetSampleModule()
        {
            var moduleName = $"{Guid.NewGuid():N}_{DateTimeOffset.Now.Ticks}";
            var expressionId = Guid.NewGuid();
            var expression = @"(a,b) => $""{a.GetType().Name} & {b.GetType().Name}""";
            var expressions = new Dictionary<Guid, string>
            {
                [expressionId] = expression,
            };

            return (moduleName, expressions, "Dictionary`2 & Dictionary`2");
        }
    }
}
