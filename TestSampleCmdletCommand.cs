using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cottle;
using System.Collections.Generic;

namespace Switch_Config_Helper
{
    [Cmdlet(VerbsDiagnostic.Test,"SampleCmdlet")]
    [OutputType(typeof(FavoriteStuff))]
    public class TestSampleCmdletCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public int FavoriteNumber { get; set; }

        [Parameter(
            Position = 1,
            ValueFromPipelineByPropertyName = true)]
        [ValidateSet("Cat", "Dog", "Horse")]
        public string FavoritePet { get; set; } = "Dog";

        // This method gets called once for each cmdlet in the pipeline when the pipeline starts executing
        protected override void BeginProcessing()
        {
            WriteVerbose("Begin!");
        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            var template = "Hello {who}, stay awhile and listen!";

            var documentResult = Document.CreateDefault(template); // Create from template string
            var document = documentResult.DocumentOrThrow; // Throws ParseException on error

            var context = Context.CreateBuiltin(new Dictionary<Value, Value>
            {
                ["who"] = "my friend" // Declare new variable "who" with value "my friend"
            });

            FavoritePet = document.Render(context);
            WriteObject(new FavoriteStuff { 
                FavoriteNumber = FavoriteNumber,
                FavoritePet = FavoritePet
            });
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {
            WriteVerbose("End!");
        }
    }

    public class FavoriteStuff
    {
        public int FavoriteNumber { get; set; }
        public string FavoritePet { get; set; }
    }
}
