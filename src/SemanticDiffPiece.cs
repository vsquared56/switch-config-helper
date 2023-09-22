using System;
using System.Collections.Generic;
using DiffPlex.DiffBuilder.Model;

namespace SwitchConfigHelper
{
    public class SemanticDiffPiece : DiffPiece, IEquatable<SemanticDiffPiece>
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

        public bool Equals(SemanticDiffPiece other)
        {
            return base.Equals(other) && EqualityComparer<int?>.Default.Equals(SectionStartPosition, other.SectionStartPosition);
        }
    }
}