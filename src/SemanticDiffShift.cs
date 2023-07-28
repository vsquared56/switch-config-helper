using System;

namespace SwitchConfigHelper
{
    internal class SemanticDiffShift : IComparable
    {
        public int ShiftAmount { get; set; }
        public string PrecedingPiece { get; set; }
        public string FirstPiece { get; set; }
        public string LastPiece { get; set; }
        public string FollowingPiece { get; set; }
        public int Score { get; set; }

        public SemanticDiffShift(int shiftAmount, string precedingPiece, string firstPiece, string lastPiece, string followingPiece)
        {
            ShiftAmount = shiftAmount;
            PrecedingPiece = precedingPiece;
            FirstPiece = firstPiece;
            LastPiece = lastPiece;
            FollowingPiece = followingPiece;

            Score = 0;

            if (firstPiece.Contains("!")) { Score--; } //Diffs that start with a section terminator are less optimal
            if (lastPiece.Contains("!")) { Score++; } //Diffs that end with a section terminator are more optimal
            if (firstPiece.Length == 0) { Score++; } //Diffs that start with a newline are more optimal
            if (precedingPiece == null || followingPiece == null) { Score++; } //Diffs at the very start or end of a file are more optimal

        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            SemanticDiffShift next = obj as SemanticDiffShift;
            if (next != null)
            {
                if (next.Score == this.Score)
                {
                    return this.ShiftAmount.CompareTo(next.ShiftAmount);
                }
                else
                {
                    return this.Score.CompareTo(next.Score);
                }
            }
            else
            {
                throw new ArgumentException("Object is not a SemanticDiffShift");
            }  
        }
    }
}
