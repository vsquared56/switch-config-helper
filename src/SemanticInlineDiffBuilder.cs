using DiffPlex;
using System;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.Collections.Generic;
using DiffPlex.Chunkers;
using System.Linq;

namespace SwitchConfigHelper
{
    public class SemanticInlineDiffBuilder : InlineDiffBuilder, IInlineDiffBuilder
    {
        public SemanticInlineDiffBuilder(IDiffer differ = null) : base(differ)
        {
        }

        public new SemanticDiffPaneModel BuildDiffModel(string oldText, string newText)
            => BuildDiffModel(oldText, newText, ignoreWhitespace: true);

        public new SemanticDiffPaneModel BuildDiffModel(string oldText, string newText, bool ignoreWhitespace)
        {
            var chunker = new LineChunker();
            return BuildDiffModel(oldText, newText, ignoreWhitespace, false, chunker);
        }

        //This isn't intended to analyze an arbitrary shift.
        //Instead, start at 0 and call with increasing/decreasing shift amounts until an invalid shift
        private bool isValidShift(SemanticDiffPaneModel model, int changeStart, int changeEnd, int shiftAmount)
        {
            if (shiftAmount == 0)
            {
                return true;
            }
            else if (shiftAmount < 0)
            {
                //shifts left are valid if they are still within the document
                //if we're shifting left into still-unchanged lines
                //and if the text to the left is identical to the last-changed line
                return (changeStart + shiftAmount >= 0
                        && model.Lines[changeStart + shiftAmount].Type == ChangeType.Unchanged
                        && model.Lines[changeStart + shiftAmount].Text == model.Lines[changeEnd + shiftAmount].Text);
            }
            else if (shiftAmount > 0)
            {
                return (changeEnd + shiftAmount < model.Lines.Count
                        && model.Lines[changeEnd + shiftAmount].Type == ChangeType.Unchanged
                        && model.Lines[changeStart + shiftAmount - 1].Text == model.Lines[changeEnd + shiftAmount].Text);
            }
            return false;
        }

        //Take the base diff model, and analyze if blocks of changes can be shifted left or right
        //for better semantic alignment.  See 3.2.2 in https://neil.fraser.name/writing/diff/ for inspiration
        public new SemanticDiffPaneModel BuildDiffModel(string oldText, string newText, bool ignoreWhitespace, bool ignoreCase, IChunker chunker)
        {
            DiffPaneModel model = base.BuildDiffModel(oldText, newText, ignoreWhitespace, ignoreCase, chunker);
            SemanticDiffPaneModel semanticModel = new SemanticDiffPaneModel();
            if (model.HasDifferences)
            {
                int currentSectionStart = -1;
                for (int i = 0; i < model.Lines.Count; i++)
                {
                    var currentLine = model.Lines[i];
                    if (currentLine.Text == "!" && (currentLine.Type == ChangeType.Unchanged || currentLine.Type == ChangeType.Inserted))
                    {
                        currentSectionStart = -1;
                        semanticModel.Lines.Add(new SemanticDiffPiece(
                            currentLine.Text,
                            currentLine.Type,
                            currentLine.Position,
                            null));
                    }
                    else if (currentSectionStart == -1 && currentLine.Position != null && (currentLine.Type == ChangeType.Unchanged || currentLine.Type == ChangeType.Inserted))
                    {
                        currentSectionStart = (int)currentLine.Position;
                        semanticModel.Lines.Add(new SemanticDiffPiece(
                            currentLine.Text,
                            currentLine.Type,
                            currentLine.Position,
                            currentSectionStart));
                    }
                    else
                    {
                        semanticModel.Lines.Add(new SemanticDiffPiece(
                            currentLine.Text,
                            currentLine.Type,
                            currentLine.Position,
                            currentSectionStart));
                    }
                }

                //Detect changed blocks surrounded by unchanged ones
                //The first and last lines do not need to be checked
                for (var changeStart = 1; changeStart < model.Lines.Count - 1; changeStart++)
                {
                    if (semanticModel.Lines[changeStart - 1].Type == ChangeType.Unchanged &&
                        (semanticModel.Lines[changeStart].Type == ChangeType.Deleted || semanticModel.Lines[changeStart].Type == ChangeType.Inserted))
                    {
                        var changeEnd = changeStart;
                        while ((changeEnd < semanticModel.Lines.Count - 1) && (semanticModel.Lines[changeEnd + 1].Type == semanticModel.Lines[changeStart].Type))
                        {
                            changeEnd++;
                        }

                        //A changed block surrounded by unchanged ones is a candidate for shifting left/right
                        if (changeEnd < semanticModel.Lines.Count - 1 && semanticModel.Lines[changeEnd + 1].Type == ChangeType.Unchanged)
                        {
                            List<SemanticDiffShift> potentialShifts = new List<SemanticDiffShift>();
                            var currentShift = 0;

                            //Add the unshifted change, and try shifting left
                            while (isValidShift(semanticModel, changeStart, changeEnd, currentShift))
                            {
                                potentialShifts.Add(new SemanticDiffShift(currentShift,
                                    changeStart + currentShift - 1 > 0 ? semanticModel.Lines[changeStart + currentShift - 1].Text : null,
                                    semanticModel.Lines[changeStart + currentShift].Text,
                                    semanticModel.Lines[changeEnd + currentShift].Text,
                                    changeEnd + currentShift + 1 < semanticModel.Lines.Count ? semanticModel.Lines[changeEnd + currentShift + 1].Text : null));
                                currentShift--;
                            }

                            //try shifting right
                            currentShift = 1;
                            while (isValidShift(semanticModel, changeStart, changeEnd, currentShift))
                            {
                                potentialShifts.Add(new SemanticDiffShift(currentShift,
                                    changeStart + currentShift - 1 > 0 ? semanticModel.Lines[changeStart + currentShift - 1].Text : null,
                                    semanticModel.Lines[changeStart + currentShift].Text,
                                    semanticModel.Lines[changeEnd + currentShift].Text,
                                    changeEnd + currentShift + 1 < semanticModel.Lines.Count ? semanticModel.Lines[changeEnd + currentShift + 1].Text : null));
                                currentShift++;
                            }

                            potentialShifts.Sort();
                            var optimalShift = potentialShifts.LastOrDefault().ShiftAmount;

                            //perform the shift
                            if (optimalShift != 0)
                            {
                                var currentChange = semanticModel.Lines[changeStart].Type;
                                var newStart = changeStart + optimalShift;
                                var newEnd = changeEnd + optimalShift;
                                for (var i = Math.Min(newStart, changeStart); i <= Math.Max(newEnd, changeEnd); i++)
                                {
                                    if (i >= newStart && i <= newEnd)
                                    {
                                        semanticModel.Lines[i].Type = currentChange;
                                    }
                                    else
                                    {
                                        semanticModel.Lines[i].Type = ChangeType.Unchanged;
                                    }
                                    //keep going from the end of the shifted change
                                    changeStart = Math.Max(newEnd, changeEnd);
                                }
                            }
                        }
                    }
                }
            }
            return semanticModel;
        }
    }
}