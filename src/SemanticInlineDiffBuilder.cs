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

        public new DiffPaneModel BuildDiffModel(string oldText, string newText)
            => BuildDiffModel(oldText, newText, ignoreWhitespace: true);

        public new DiffPaneModel BuildDiffModel(string oldText, string newText, bool ignoreWhitespace)
        {
            var chunker = new LineChunker();
            return BuildDiffModel(oldText, newText, ignoreWhitespace, false, chunker);
        }

        //This isn't intended to analyze an arbitrary shift.
        //Instead, start at 0 and call with increasing/decreasing shift amounts until an invalid shift
        private bool isValidShift(DiffPaneModel model, int changeStart, int changeEnd, int shiftAmount)
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
        public new DiffPaneModel BuildDiffModel(string oldText, string newText, bool ignoreWhitespace, bool ignoreCase, IChunker chunker)
        {
            DiffPaneModel model = base.BuildDiffModel(oldText, newText, ignoreWhitespace, ignoreCase, chunker);
            if (model.HasDifferences)
            {
                //Detect changed blocks surrounded by unchanged ones
                //The first and last lines do not need to be checked
                for (var changeStart = 1; changeStart < model.Lines.Count - 1; changeStart++)
                {
                    if (model.Lines[changeStart - 1].Type == ChangeType.Unchanged &&
                        (model.Lines[changeStart].Type == ChangeType.Deleted || model.Lines[changeStart].Type == ChangeType.Inserted))
                    {
                        var changeEnd = changeStart;
                        while ((changeEnd < model.Lines.Count - 1) && (model.Lines[changeEnd + 1].Type == model.Lines[changeStart].Type))
                        {
                            changeEnd++;
                        }
                        
                        //A changed block surrounded by unchanged ones is a candidate for shifting left/right
                        if (changeEnd < model.Lines.Count - 1 && model.Lines[changeEnd + 1].Type == ChangeType.Unchanged)
                        {
                            List<SemanticDiffShift> potentialShifts = new List<SemanticDiffShift>();
                            var currentShift = 0;
                            
                            //Add the unshifted change, and try shifting left
                            while (isValidShift(model, changeStart, changeEnd, currentShift))
                            {
                                potentialShifts.Add(new SemanticDiffShift(currentShift,
                                    changeStart + currentShift - 1 > 0 ? model.Lines[changeStart + currentShift - 1].Text : null,
                                    model.Lines[changeStart + currentShift].Text,
                                    model.Lines[changeEnd + currentShift].Text,
                                    changeEnd + currentShift + 1 < model.Lines.Count ? model.Lines[changeEnd + currentShift + 1].Text : null));
                                currentShift--;
                            }

                            //try shifting right
                            currentShift = 1;
                            while (isValidShift(model, changeStart, changeEnd, currentShift))
                            {
                                potentialShifts.Add(new SemanticDiffShift(currentShift,
                                    changeStart + currentShift - 1 > 0 ? model.Lines[changeStart + currentShift - 1].Text : null,
                                    model.Lines[changeStart + currentShift].Text,
                                    model.Lines[changeEnd + currentShift].Text,
                                    changeEnd + currentShift + 1 < model.Lines.Count ? model.Lines[changeEnd + currentShift + 1].Text : null));
                                currentShift++;
                            }

                            potentialShifts.Sort();
                            var optimalShift = potentialShifts.LastOrDefault().ShiftAmount;

                            //perform the shift
                            if (optimalShift != 0)
                            {
                                var currentChange = model.Lines[changeStart].Type;
                                var newStart = changeStart + optimalShift;
                                var newEnd = changeEnd + optimalShift;
                                for (var i = Math.Min(newStart, changeStart); i <= Math.Max(newEnd, changeEnd); i++)
                                {
                                    if (i >= newStart && i <= newEnd)
                                    {
                                        model.Lines[i].Type = currentChange;
                                    }
                                    else
                                    {
                                        model.Lines[i].Type = ChangeType.Unchanged;
                                    }
                                    //keep going from the end of the shifted change
                                    changeStart = Math.Max(newEnd, changeEnd);
                                }
                            }
                        }
                    }
                }
            }
            return model;
        }
    }
}