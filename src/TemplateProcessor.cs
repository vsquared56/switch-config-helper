using System;
using System.IO;
using System.Threading.Tasks;
using Scriban;
using Scriban.Runtime;
using Scriban.Parsing;


namespace SwitchConfigHelper
{
    public class TemplateProcessor
    {
        static TemplateContext context = new TemplateContext();

        public TemplateProcessor()
        {
            context.TemplateLoader = new TemplateLoader();
        }

        public Template Parse(string templateText)
        {
            return Parse(templateText, "");
        }

        public Template Parse(string templateText, string templatePath)
        {
            var dnsFunctions = new DnsLookups();

            context.PushGlobal(dnsFunctions);

            var template = Template.Parse(templateText, templatePath);
            if (template.HasErrors)
            {
                foreach (var error in template.Messages)
                {
                    Console.WriteLine(error);
                }
                return null;
            }
            return template;
        }

        public string Render(Template template)
        {
            var result = template.Render(context);
            return result;
        }
    }

    public class TemplateLoader : ITemplateLoader
    {
        public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
        {
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(callerSpan.FileName), templateName));
        }

        public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            return File.ReadAllText(templatePath);
        }

        public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            throw new NotImplementedException();
        }
    }
}