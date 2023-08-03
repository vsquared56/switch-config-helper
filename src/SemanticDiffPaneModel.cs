using System.Collections.Generic;
using System.Linq;
using DiffPlex.DiffBuilder.Model;

namespace SwitchConfigHelper
{
    public class SemanticDiffPaneModel : DiffPaneModel
    {
        public new List<SemanticDiffPiece> Lines { get; }

        public new bool HasDifferences
        {
            get { return Lines.Any(x => x.Type != ChangeType.Unchanged); }
        }

        public SemanticDiffPaneModel()
        {
            Lines = new List<SemanticDiffPiece>();
        }

        public SemanticDiffPaneModel(DiffPaneModel model)
        {
            Lines = model.Lines.ConvertAll(x => new SemanticDiffPiece(x));
        }
    }
}