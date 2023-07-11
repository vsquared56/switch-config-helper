using System;
using System.Management.Automation;
using Scriban;
using System.IO;

namespace SwitchConfigHelper
{
    public class TemplateProcessor
    {
        static TemplateContext context = new TemplateContext();
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
}