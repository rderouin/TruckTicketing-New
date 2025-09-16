using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.Note;
using SE.Shared.Domain.Entities.Note.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Tests.Note.Rules;

[TestClass]
public class NoteBasicValidationRulesTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void NoteBasicValidationRule_CanBeInstantiated()
    {
        //arrange
        var scope = new DefaultScope();

        //act
        var runOrder = scope.InstanceUnderTest.RunOrder;

        //assert
        runOrder.Should().BePositive();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task NoteBasicValidationRule_ShouldPass_ValidNoteEntry()
    {
        //arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidNoteEntity();
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(null)]
    public async Task NoteBasicValidationRule_ShouldFail_WhenThreadIdIsNullOrEmpty(string threadId)
    {
        //arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidNoteEntity();
        context.Target.ThreadId = threadId;
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert 
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.Note_ThreadId));
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(null)]
    public async Task NoteBasicValidationRule_ShouldFail_WhenCommentIsNullOrEmpty(string comment)
    {
        //arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidNoteEntity();
        context.Target.Comment = comment;
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert 
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.Note_Comment));
    }

    private class DefaultScope : TestScope<NoteBasicValidationRules>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public BusinessContext<NoteEntity> CreateContextWithValidNoteEntity()
        {
            return new(new()
            {
                Comment = "TestComment",
                CreatedBy = "TestUser",
                CreatedById = "TestUser",
                UpdatedById = "TestUser",
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = DateTimeOffset.Now,
                ThreadId = "Test|TestId",
            });
        }
    }
}
