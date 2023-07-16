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

        protected override void BeginProcessing()
        {

        }

        protected override void ProcessRecord()
        {
            var referenceText = File.ReadAllText(ReferencePath);
            var differenceText = File.ReadAllText(DifferencePath);
            var diffBuilder = new InlineDiffBuilder(new Differ());
            var diff = diffBuilder.BuildDiffModel(referenceText, differenceText);

            string output = "";
            string currentSection = null;
            for (int i = 0; i < diff.Lines.Count; i++)
            {
                var line = diff.Lines[i];
                if (line.Position.HasValue)
                {
                    output += line.Position.Value;
                }

                if (line.Text == "!")
                {
                    currentSection = null;
                    Console.WriteLine($"Found ! at line {i}");

                    //Better diffs for entire inserted sections
                    //If the first line of a new set of changes is a section terminator, find the next unchanged line
                    //If the next unchanged line is also a section terminator, flip the line type:
                    //The first section terminator is not an actual change, the second section terminator is.
                    if ((line.Type == ChangeType.Inserted || line.Type == ChangeType.Deleted) && i > 0 && diff.Lines[i-1].Type == ChangeType.Unchanged)
                    {
                        int j = i + 1;
                        while (j < diff.Lines.Count && diff.Lines[j].Type == diff.Lines[i].Type)
                        {
                            j++;
                        }

                        if (diff.Lines[j].Type == ChangeType.Unchanged)
                        {
                            diff.Lines[j].Type = diff.Lines[i].Type;
                            diff.Lines[i].Type = ChangeType.Unchanged;
                        }
                    }
                }
                else if (currentSection == null)
                {
                    currentSection = line.Text;
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
            WriteObject(output);
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {

        }
    }
}
