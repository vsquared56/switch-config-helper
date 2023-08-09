using System;

namespace SwitchConfigHelper
{
    internal class EffectiveAclEntry : IEquatable<EffectiveAclEntry>
    {
        public SemanticDiffPiece Piece { get; set; }

        public EffectiveAclEntry(SemanticDiffPiece p)
        {
            Piece = p;
        }

        public bool Equals(EffectiveAclEntry other)
        {
            return Piece.Text.Equals(other.Piece.Text);
        }
    }
}
