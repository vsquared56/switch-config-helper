using System.Management.Automation;
using DiffPlex;
using System;
using System.IO;

namespace SwitchConfigHelper
{
    [Cmdlet(VerbsData.Compare, "EffectiveSwitchConfigs", DefaultParameterSetName = "Context")]
    [OutputType(typeof(string))]
    public class CompareEffectiveSwitchConfigsCmdlet : PSCmdlet
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
            Position = 7,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        [Parameter(ParameterSetName = "Context")]
        [Parameter(ParameterSetName = "Full")]
        public SwitchParameter IgnoreEqualAcls
        {
            get { return ignoreEqualAcls; }
            set { ignoreEqualAcls = value; }
        }
        private bool ignoreEqualAcls;

        [Parameter(
            Mandatory = false,
            Position = 8,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        [Parameter(ParameterSetName = "Context")]
        [Parameter(ParameterSetName = "Full")]
        public SwitchParameter IgnoreRemovedDuplicateAcls
        {
            get { return ignoreRemovedDuplicateAcls; }
            set { ignoreRemovedDuplicateAcls = value; }
        }
        private bool ignoreRemovedDuplicateAcls;

        protected override void BeginProcessing()
        {

        }

        protected override void ProcessRecord()
        {
            var referenceText = File.ReadAllText(referencePath);
            var differenceText = File.ReadAllText(differencePath);
            var diffBuilder = new SemanticInlineDiffBuilder(new Differ());
            SemanticDiffPaneModel diff;
            
            diff = diffBuilder.BuildEffectiveDiffModel(referenceText, differenceText, ignoreRemovedDuplicateAcls);

            string result;
            if (printFullDiff)
            {
                result = DiffFormatter.FormatDiff(diff, true, ignoreEqualAcls);
            }
            else
            {
                var trimmedLineMarker = showTrimmedLines ? "..." : "";
                result = DiffFormatter.FormatDiff(diff, true, Context, !NoSectionHeaders, trimmedLineMarker, ignoreEqualAcls);
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