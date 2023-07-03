using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cottle;
using System.Collections.Generic;
using System.IO;

namespace Switch_Config_Helper
{
    [Cmdlet(VerbsDiagnostic.Test, "SampleCmdlet")]
    [OutputType(typeof(string))]
    public class TestSampleCmdletCommand : PSCmdlet
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
            using (StreamReader templateReader = File.OpenText(TemplatePath))
            {
                var documentResult = Document.CreateDefault(templateReader); // Create from template string
                var document = documentResult.DocumentOrThrow; // Throws ParseException on error

                var context = Context.CreateBuiltin(new Dictionary<Value, Value>
                {
                    ["who"] = "my friend" // Declare new variable "who" with value "my friend"
                });

                var output = document.Render(context);
                WriteObject(output);
            }
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {

        }
    }
}
