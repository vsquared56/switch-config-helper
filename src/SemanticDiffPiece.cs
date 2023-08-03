using System;
using DiffPlex;
using DiffPlex.DiffBuilder.Model;

namespace SwitchConfigHelper
{
    public class SemanticDiffPiece : DiffPiece, IEquatable<DiffPiece>
    {
        public int? SectionStartPosition { get; set; }
        public SemanticDiffPiece(string text, ChangeType type, int? position = null)
        {
            Text = text;
            Position = position;
            Type = type;
            SectionStartPosition = null;
        }

        public SemanticDiffPiece(string text, ChangeType type, int? position = null, int? sectionPosition = null)
        {
            Text = text;
            Position = position;
            Type = type;
            SectionStartPosition = sectionPosition;
        }

        public SemanticDiffPiece(DiffPiece piece)
        {
            Text = piece.Text;
            Position = piece.Position;
            Type = piece.Type;
            SectionStartPosition = null;
        }
    }
}