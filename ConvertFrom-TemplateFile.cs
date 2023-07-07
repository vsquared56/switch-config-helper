using System;
using System.Management.Automation;
using Scriban;
using System.IO;

namespace SwitchConfigHelper
{
    [Cmdlet(VerbsData.ConvertFrom, "TemplateFile")]
    [OutputType(typeof(string))]
    public class ConvertFromTemplateFileCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public string TemplatePath { get; set; }

        protected override void BeginProcessing()
        {

        }

        protected override void ProcessRecord()
        {
            var dnsFunctions = new DnsLookups();

            var context = new TemplateContext();
            context.PushGlobal(dnsFunctions);

            var template = Template.Parse(File.ReadAllText(TemplatePath), TemplatePath);
            if (template.HasErrors)
            {
                foreach (var error in template.Messages)
                {
                    Console.WriteLine(error);
                }
                return;
            }

            var result = template.Render(context);

            WriteObject(result);
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {

        }
    }
}
