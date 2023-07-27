using System.Management.Automation;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.IO;
using System;
using System.Text;

namespace SwitchConfigHelper
{
    [Cmdlet(VerbsData.Compare, "SwitchConfigFiles")]
    [OutputType(typeof(string))]
    public class CompareSwitchConfigFilesCmdlet : Cmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        public string ReferencePath { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        public string DifferencePath { get; set; }

        [Parameter(
            Mandatory = false,
            Position = 2,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        [Parameter(ParameterSetName = "Context")]
        [ValidateContextParameter()]
        public int Context { get; set; } = 0;

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

            string currentSection = null;
            int currentSectionStart = 0;
            bool currentSectionContextPrinted = false;
            int lastForwardContext = 0;
            System.Text.StringBuilder result = new System.Text.StringBuilder(Math.Max(referenceText.Length,differenceText.Length));

            for (int i = 0; i < diff.Lines.Count; i++)
            {
                var line = diff.Lines[i];

                if (line.Text == "!")
                {
                    currentSection = null;
                    currentSectionStart = i;
                    currentSectionContextPrinted = false;
                }
                else if (currentSection == null)
                {
                    currentSection = line.Text;
                    currentSectionStart = i;
                    currentSectionContextPrinted = false;
                }

                //Print all lines
                if (printFullDiff)
                {
                    AddFormattedOutputLine(ref result, line);
                }
                //print only changed lines with context
                else
                {
                    //new change, and we're not printing forward context from an earlier change
                    if (lastForwardContext < i && (line.Type == ChangeType.Inserted || line.Type == ChangeType.Deleted))
                    {
                        //print section information
                        if (!noSectionHeaders && !currentSectionContextPrinted && currentSection != null && (i - currentSectionStart) > Context)
                        {
                            AddFormattedOutputLine(ref result, diff.Lines[currentSectionStart]);
                            if ((i - currentSectionStart) > Context + 1)
                            {
                                result.AppendLine("\t  ...");
                            }
                            currentSectionContextPrinted = true;
                        }

                        //print previous context, but only as far back as the already-printed forward context or the start of the document
                        for (var j = Math.Max(0, Math.Max(i - Context, lastForwardContext + 1)); j <= i; j++)
                        {
                            AddFormattedOutputLine(ref result, diff.Lines[j]);
                        }
                        lastForwardContext = i + Context;
                    }
                    //finish printing forward context from an earlier change
                    else if (lastForwardContext >= i)
                    {
                        //additional changes need more context
                        if (line.Type == ChangeType.Inserted || line.Type == ChangeType.Deleted)
                        {
                            lastForwardContext = i + Context;
                        }
                        AddFormattedOutputLine(ref result, line);
                    }
                }

            }
            WriteObject(result.ToString());
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {

        }

        private void AddFormattedOutputLine(ref StringBuilder result, DiffPiece line)
        {
            if (line.Position.HasValue)
            {
                result.Append(line.Position.Value);
            }

            result.Append('\t');
            switch (line.Type)
            {
                case ChangeType.Inserted:
                    result.Append("+ ");
                    break;
                case ChangeType.Deleted:
                    result.Append("- ");
                    break;
                default:
                    result.Append("  ");
                    break;
            }
            result.Append(line.Text);
            result.Append(System.Environment.NewLine);
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