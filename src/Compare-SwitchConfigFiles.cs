using System.Management.Automation;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.IO;
using System;
using System.Text;

namespace SwitchConfigHelper
{
    [Cmdlet(VerbsData.Compare, "SwitchConfigFiles", DefaultParameterSetName = "Context")]
    [OutputType(typeof(string))]
    public class CompareSwitchConfigFilesCmdlet : Cmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        [Parameter(ParameterSetName = "Context")]
        [Parameter(ParameterSetName = "Full")]
        public string ReferencePath { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        [Parameter(ParameterSetName = "Context")]
        [Parameter(ParameterSetName = "Full")]
        public string DifferencePath { get; set; }

        [Parameter(
            Mandatory = false,
            Position = 2,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        [Parameter(ParameterSetName = "Context")]
        [ValidateContextParameter()]
        public int Context { get; set; } = 3;

        [Parameter(
            Mandatory = false,
            Position = 3,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        [Parameter(ParameterSetName = "Context")]
        public SwitchParameter NoSectionHeaders
        {
            get { return noSectionHeaders; }
            set { noSectionHeaders = value; }
        }
        private bool noSectionHeaders;

        [Parameter(
            Mandatory = false,
            Position = 4,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        [Parameter(ParameterSetName = "Context")]
        public SwitchParameter ShowTrimmedLines
        {
            get { return showTrimmedLines; }
            set { showTrimmedLines = value; }
        }
        private bool showTrimmedLines;

        [Parameter(
            Mandatory = false,
            Position = 5,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        [Parameter(ParameterSetName = "Full")]
        public SwitchParameter Full
        {
            get { return printFullDiff; }
            set { printFullDiff = value; }
        }
        private bool printFullDiff;

        protected override void BeginProcessing()
        {

        }

        protected override void ProcessRecord()
        {
            var referenceText = File.ReadAllText(ReferencePath);
            var differenceText = File.ReadAllText(DifferencePath);
            var diffBuilder = new SemanticInlineDiffBuilder(new Differ());
            var diff = diffBuilder.BuildDiffModel(referenceText, differenceText);

            if (printFullDiff)
            {
                WriteObject(DiffFormatter.FormatDiff(diff, true));
            }
            else
            {
                var trimmedLineMarker = showTrimmedLines ? "..." : "";
                WriteObject(DiffFormatter.FormatDiff(diff, true, Context, !NoSectionHeaders, trimmedLineMarker));
            }
            
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {

        }
    }
}

class ValidateContextParameter : ValidateArgumentsAttribute
{
    protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
    {
        var context = (int)arguments;
        if (context < 0)
        {
            throw new ArgumentOutOfRangeException();
        }
    }
}