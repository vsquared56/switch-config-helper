using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cottle;
using System.Collections.Generic;
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

        protected string DefaultTrimmer(string text)
        {
            Console.WriteLine("<Input>" + "Length: " + text.Length);
            Console.WriteLine(text.Replace("\r", "\\r").Replace("\n", "\\n").Replace(" ", "."));
            Console.WriteLine("</Input>");
            int index;
            int start;
            int stop;
            // Skip first line if any
            for (index = 0; index < text.Length && text[index] <= ' ' && text[index] != '\n' && text[index] != '\r';)
                ++index;
            if (index >= text.Length || text[index] != '\n' && text[index] != '\r')
                start = 0;
            else if (index + 1 >= text.Length || text[index] == text[index + 1] ||
                     text[index + 1] != '\n' && text[index + 1] != '\r')
                start = index + 1;
            else
                start = index + 2;
            // Skip last line if any
            for (index = text.Length - 1;
                index >= 0 && text[index] <= ' ' && text[index] != '\n' && text[index] != '\r';)
            {
                Console.WriteLine("Decrement");
                index--;
            }
            Console.WriteLine("Index: " + index);
            if (index < 0 || text[index] != '\n' && text[index] != '\r')
            {
                Console.WriteLine("Last A");
                stop = text.Length - 1;
            }
            else if (index < 1 || text[index] == text[index - 1] || text[index - 1] != '\n' && text[index - 1] != '\r')
            {
                Console.WriteLine("Last B");
                stop = index - 1;
            }
            else
            {
                Console.WriteLine("Last C");
                stop = index;
            }
            // Select inner content if any, whole text else
            if (start < stop)
                text = text.Substring(start, stop - start + 1);
            
            Console.WriteLine("<Output> Start: " + start + " Stop: " + stop + " substrLen: " + (stop - start));
            Console.WriteLine(text.Replace("\r", "\\r").Replace("\n", "\\n").Replace(" ", "."));
            Console.WriteLine("</Output>");
            return text;
        }

        protected string AclTrimmer(string text)
        {
            Console.WriteLine("<Input>" + "Length: " + text.Length);
            Console.WriteLine(text.Replace("\r", "\\r").Replace("\n", "\\n").Replace(" ", "_"));
            Console.WriteLine("</Input>");
            
            Console.WriteLine("<Output>");
            Console.WriteLine(text.Replace("\r", "\\r").Replace("\n", "\\n").Replace(" ", "."));
            Console.WriteLine("</Output>");
            return text;
        }


        protected override void BeginProcessing()
        {

        }

        protected override void ProcessRecord()
        {
            using (StreamReader templateReader = File.OpenText(TemplatePath))
            {
                var configuration = new DocumentConfiguration
                {
                    Trimmer = AclTrimmer
                    //Trimmer = DefaultTrimmer
                    //Trimmer = DocumentConfiguration.TrimNothing
                };

                var documentResult = Document.CreateDefault(templateReader, configuration);
                var document = documentResult.DocumentOrThrow; // Throws ParseException on error

                var context = Context.CreateBuiltin(new Dictionary<Value, Value>
                {

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
