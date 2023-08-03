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
            Lines = new List<SemanticDiffPiece>();
            int currentSectionStart = -1;
            for (int i = 0; i < model.Lines.Count; i++)
            {
                var currentLine = model.Lines[i];
                if (currentLine.Text == "!" && (currentLine.Type == ChangeType.Unchanged || currentLine.Type == ChangeType.Inserted))
                {
                    Lines.Add(new SemanticDiffPiece(
                        currentLine.Text,
                        currentLine.Type,
                        currentLine.Position,
                        currentSectionStart == -1 ? i : currentSectionStart));
                    currentSectionStart = -1;
                }
                else if (currentSectionStart == -1 && currentLine.Position != null && (currentLine.Type == ChangeType.Unchanged || currentLine.Type == ChangeType.Inserted))
                {
                    currentSectionStart = (int)currentLine.Position;
                    Lines.Add(new SemanticDiffPiece(
                        currentLine.Text,
                        currentLine.Type,
                        currentLine.Position,
                        currentSectionStart));
                }
                else
                {
                    Lines.Add(new SemanticDiffPiece(
                        currentLine.Text,
                        currentLine.Type,
                        currentLine.Position,
                        currentSectionStart));
                }
            }
        }
    }
}