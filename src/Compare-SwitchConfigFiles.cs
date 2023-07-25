using System.Management.Automation;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.IO;
using System;

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
        [ValidateContextParameter()]
        public int Context { get; set; } = 0;

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
            string output = "";

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
                if (Context == 0)
                {
                    AddFormattedOutputLine(ref output, line);
                }
                //print only changed lines with context
                else
                {
                    //new change, and we're not printing forward context from an earlier change
                    if (lastForwardContext < i && (line.Type == ChangeType.Inserted || line.Type == ChangeType.Deleted))
                    {
                        //print section information
                        if (currentSectionContextPrinted == false && currentSection != null && (i - currentSectionStart) > Context)
                        {
                            AddFormattedOutputLine(ref output, diff.Lines[currentSectionStart]);
                            if ((i - currentSectionStart) > Context + 1)
                            {
                                output += "\t  ..." + System.Environment.NewLine;
                            }
                            currentSectionContextPrinted = true;
                        }

                        //print previous context, but only as far back as the already-printed forward context or the start of the document
                        for (var j = Math.Max(0, Math.Max(i - Context, lastForwardContext + 1)); j <= i; j++)
                        {
                            AddFormattedOutputLine(ref output, diff.Lines[j]);
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
                        AddFormattedOutputLine(ref output, line);
                    }
                }

            }
            WriteObject(output);
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {

        }

        private void AddFormattedOutputLine(ref string output, DiffPiece line)
        {
            if (line.Position.HasValue)
            {
                output += line.Position.Value;
            }

            output += '\t';
            switch (line.Type)
            {
                case ChangeType.Inserted:
                    output += "+ ";
                    break;
                case ChangeType.Deleted:
                    output += "- ";
                    break;
                default:
                    output += "  ";
                    break;
            }
            output += line.Text;
            output += System.Environment.NewLine;
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