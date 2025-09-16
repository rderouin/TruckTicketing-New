using SE.TruckTicketing.Contracts;

using Trident.Business;
using Trident.Validation;

namespace SE.Shared.Domain.Entities.Note.Rules;

public class NoteBasicValidationRules : PropertyExpressionValidationRule<BusinessContext<NoteEntity>, NoteEntity, TTErrorCodes>
{
    public override int RunOrder => 100;

    protected override void ConfigureRules(BusinessContext<NoteEntity> context)
    {
        AddRule(nameof(NoteEntity.Comment), x => !string.IsNullOrWhiteSpace(x.Comment),
                errorCode: TTErrorCodes.Note_Comment);

        AddRule(nameof(NoteEntity.ThreadId), x => !string.IsNullOrWhiteSpace(x.ThreadId),
                errorCode: TTErrorCodes.Note_ThreadId);
    }
}
