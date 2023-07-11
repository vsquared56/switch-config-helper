﻿using System;
using System.Management.Automation;
using Scriban;
using System.IO;

namespace SwitchConfigHelper
{
    [Cmdlet(VerbsData.ConvertFrom, "TemplateFile")]
    [OutputType(typeof(string))]
    public class ConvertFromTemplateFileCommand : Cmdlet
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
            var processor = new TemplateProcessor();
            var template = processor.Parse(File.ReadAllText(TemplatePath), TemplatePath);
            var result = processor.Render(template);
            WriteObject(result);
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {

        }
    }
}
