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
            int? currentSectionStart = null;
            for (int i = 0; i < model.Lines.Count; i++)
            {
                var currentLine = model.Lines[i];
                //End of the current section
                if (currentLine.Text == "!" && (currentLine.Type == ChangeType.Unchanged || currentLine.Type == ChangeType.Inserted))
                {
                    Lines.Add(new SemanticDiffPiece(
                        currentLine.Text,
                        currentLine.Type,
                        currentLine.Position,
                        currentSectionStart == null ? currentLine.Position : currentSectionStart));
                    currentSectionStart = null;
                }
                //Start of a new section contained in the output (i.e. a section that isn't deleted)
                else if (currentSectionStart == null && currentLine.Position != null && (currentLine.Type == ChangeType.Unchanged || currentLine.Type == ChangeType.Inserted))
                {
                    currentSectionStart = (int)currentLine.Position;
                    Lines.Add(new SemanticDiffPiece(
                        currentLine.Text,
                        currentLine.Type,
                        currentLine.Position,
                        currentSectionStart));
                }
                //Lines within a section, or lines in a deleted section
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