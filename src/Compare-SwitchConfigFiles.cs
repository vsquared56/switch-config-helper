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

            int remainingForwardContext = 0;
            string output = "";

            for (int i = 0; i < diff.Lines.Count; i++)
            {
                var line = diff.Lines[i];

                if (Context == 0)
                {
                    AddFormattedOutputLine(ref output, line);
                }
                else
                {
                    if (remainingForwardContext == 0 && (line.Type == ChangeType.Inserted || line.Type == ChangeType.Deleted))
                    {
                        for (var j = Math.Max(0, i - Context); j <= i; j++)
                        {
                            AddFormattedOutputLine(ref output, diff.Lines[j]);
                        }
                        remainingForwardContext = Context;
                    }
                    else if (remainingForwardContext > 0)
                    {
                        if (line.Type == ChangeType.Inserted || line.Type == ChangeType.Deleted)
                        {
                            remainingForwardContext = Context;
                        }
                        else
                        {
                            remainingForwardContext--;
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
