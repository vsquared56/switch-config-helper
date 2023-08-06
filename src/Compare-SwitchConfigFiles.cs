using System.Management.Automation;
using DiffPlex;
using System;
using System.IO;

namespace SwitchConfigHelper
{
    [Cmdlet(VerbsData.Compare, "SwitchConfigFiles", DefaultParameterSetName = "Context")]
    [OutputType(typeof(string))]
    public class CompareSwitchConfigFilesCmdlet : PSCmdlet
    {
        private string referencePath;
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        [Parameter(ParameterSetName = "Context")]
        [Parameter(ParameterSetName = "Full")]
        public string ReferencePath { 
            get { return ReferencePath; }
            set { referencePath = PathProcessor.ProcessPath(value); }
        }

        private string differencePath;
        [Parameter(
            Mandatory = true,
            Position = 1,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        [Parameter(ParameterSetName = "Context")]
        [Parameter(ParameterSetName = "Full")]
        public string DifferencePath { 
            get { return DifferencePath; }
            set { differencePath = PathProcessor.ProcessPath(value); }
         }

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

        [Parameter(
            Mandatory = false,
            Position = 6,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        [Parameter(ParameterSetName = "Full")]
        [Parameter(ParameterSetName = "Context")]
        public SwitchParameter EffectiveChangesOnly
        {
            get { return effectiveChangesOnly; }
            set { effectiveChangesOnly = value; }
        }
        private bool effectiveChangesOnly;

        protected override void BeginProcessing()
        {

        }

        protected override void ProcessRecord()
        {
            var referenceText = File.ReadAllText(referencePath);
            var differenceText = File.ReadAllText(differencePath);
            var diffBuilder = new SemanticInlineDiffBuilder(new Differ());
            SemanticDiffPaneModel diff;
            if (effectiveChangesOnly)
            {
                diff = diffBuilder.BuildEffectiveDiffModel(referenceText, differenceText);
            }
            else
            {
                diff = diffBuilder.BuildDiffModel(referenceText, differenceText);
            }

            string result;
            if (printFullDiff)
            {
                result = DiffFormatter.FormatDiff(diff, true);
            }
            else
            {
                var trimmedLineMarker = showTrimmedLines ? "..." : "";
                result = DiffFormatter.FormatDiff(diff, true, Context, !NoSectionHeaders, trimmedLineMarker);
            }

            if (result.Length == 0)
            {
                return;
            }
            else
            {
                WriteObject(result);
            }
        }

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