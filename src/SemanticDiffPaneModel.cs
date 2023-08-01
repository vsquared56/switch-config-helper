using System.Collections.Generic;
using System.Linq;
using DiffPlex.DiffBuilder.Model;

namespace SwitchConfigHelper
{
    public class SemanticDiffPaneModel : DiffPaneModel
    {
        public new List<SemanticDiffPiece> Lines { get; }

        public SemanticDiffPaneModel()
        {
            Lines = new List<SemanticDiffPiece>();
        }
    }
}