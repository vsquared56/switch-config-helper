using DiffPlex;
using System;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.Collections.Generic;
using DiffPlex.Chunkers;
using System.Linq;
using System.Collections;

namespace SwitchConfigHelper
{
    public class SemanticInlineDiffBuilder : InlineDiffBuilder, IInlineDiffBuilder
    {
        public SemanticInlineDiffBuilder(IDiffer differ = null) : base(differ)
        {
        }

        public new SemanticDiffPaneModel BuildDiffModel(string oldText, string newText)
            => BuildDiffModel(oldText, newText, ignoreWhitespace: true);

        public SemanticDiffPaneModel BuildEffectiveDiffModel(string oldText, string newText)
            => BuildEffectiveDiffModel(oldText, newText, ignoreWhitespace: true);

        public new SemanticDiffPaneModel BuildDiffModel(string oldText, string newText, bool ignoreWhitespace)
        {
            var chunker = new LineChunker();
            return BuildDiffModel(oldText, newText, ignoreWhitespace, false, chunker);
        }

        public SemanticDiffPaneModel BuildEffectiveDiffModel(string oldText, string newText, bool ignoreWhitespace)
        {
            var chunker = new LineChunker();
            return BuildEffectiveDiffModel(oldText, newText, ignoreWhitespace, false, chunker);
        }
        
        public new SemanticDiffPaneModel BuildDiffModel(string oldText, string newText, bool ignoreWhitespace, bool ignoreCase, IChunker chunker)
        {
            var model = new SemanticDiffPaneModel(base.BuildDiffModel(oldText, newText, ignoreWhitespace, ignoreCase, chunker));
            return PerformSemanticShifts(model);
        }

        public SemanticDiffPaneModel BuildEffectiveDiffModel(string oldText, string newText, bool ignoreWhitespace, bool ignoreCase, IChunker chunker)
        {
            var model = new SemanticDiffPaneModel(base.BuildDiffModel(oldText, newText, ignoreWhitespace, ignoreCase, chunker));
            return PerformSemanticShifts(FindEffectiveAclChanges(model));
        }

        public SemanticDiffPaneModel FindEffectiveAclChanges(SemanticDiffPaneModel model)
        {
            if (!model.HasDifferences)
            {
                return model;
            }
            else
            {
                //Ignore remark lines that have been deleted
                model.Lines.RemoveAll(x => x.Text.Trim().StartsWith("remark") && x.Type == ChangeType.Deleted);
                //Remark lines that have been inserted should only be considered changed
                foreach (var line in model.Lines.Where(x => x.Text.Trim().StartsWith("remark") && x.Type == ChangeType.Inserted))
                {
                    line.Type = ChangeType.Modified;
                }

                var currentSectionRemovals = new List<EffectiveAclEntry>();
                var currentSectionAdditions = new List<EffectiveAclEntry>();
                var previousSectionStart = -1;
                for (var i = 0; i < model.Lines.Count; i++)
                {
                    var currentLine = model.Lines[i];
                    var previousLine = i == 0 ? null : model.Lines[i - 1];
                    var currentSectionStart = model.Lines.IndexOf(model.Lines.Where(x => x.Position == currentLine.SectionStartPosition).First());
                    var currentSection = model.Lines[currentSectionStart];

                    if (currentSectionStart != previousSectionStart || i == model.Lines.Count - 1)
                    {
                        foreach (var acl in currentSectionRemovals)
                        {
                            if (currentSectionAdditions.Contains(acl))
                            {
                                model.Lines.Remove(acl.Piece);
                                var additionAcl = currentSectionAdditions.Find(a => a.Equals(acl));
                                model.Lines[(model.Lines.IndexOf(additionAcl.Piece))].Type = ChangeType.Modified;
                            }
                        }

                        currentSectionRemovals.Clear();
                        currentSectionAdditions.Clear();
                    }
                    else
                    {
                        if (currentLine.Type == ChangeType.Inserted && isAclSection(currentSection.Text))
                        {
                            if (currentLine.Text.Trim().StartsWith("permit"))
                            {
                                currentSectionAdditions.Add(new EffectiveAclEntry(currentLine));
                            }
                        }
                        else if (currentLine.Type == ChangeType.Deleted)
                        {
                            if (currentLine.Text.Trim().StartsWith("permit"))
                            {
                                currentSectionRemovals.Add(new EffectiveAclEntry(currentLine));
                            }
                        }
                    }
                    previousSectionStart = currentSectionStart;
                }
                return model;
            }
        }

        //Take the base diff model, and analyze if blocks of changes can be shifted left or right
        //for better semantic alignment.  See 3.2.2 in https://neil.fraser.name/writing/diff/ for inspiration
        public SemanticDiffPaneModel PerformSemanticShifts(SemanticDiffPaneModel model)
        {
            if (!model.HasDifferences)
            {
                return model;
            }
            else
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
                return model;
            }
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
                        && model.Lines[changeStart + shiftAmount].Text == model.Lines[changeEnd + shiftAmount + 1].Text);
            }
            else if (shiftAmount > 0)
            {
                return (changeEnd + shiftAmount < model.Lines.Count
                        && model.Lines[changeEnd + shiftAmount].Type == ChangeType.Unchanged
                        && model.Lines[changeStart + shiftAmount - 1].Text == model.Lines[changeEnd + shiftAmount].Text);
            }
            return false;
        }

        private bool isAclSection(string text)
        {
            return text.Trim().StartsWith("ip access-list");
        }
    }
}