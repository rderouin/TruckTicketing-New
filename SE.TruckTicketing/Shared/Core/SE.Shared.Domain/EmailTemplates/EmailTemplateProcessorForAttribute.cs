using System;

namespace SE.Shared.Domain.EmailTemplates;

public class EmailTemplateProcessorForAttribute : Attribute
{
    public EmailTemplateProcessorForAttribute(string templateKey)
    {
        TemplateKey = templateKey;
    }

    public string TemplateKey { get; }
}
