using System.Management.Automation;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.IO;

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
            foreach (var line in diff.Lines)
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
            WriteObject(output);
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {

        }
    }
}
