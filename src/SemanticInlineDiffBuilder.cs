using DiffPlex;
using System;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.Collections.Generic;
using DiffPlex.Chunkers;
using System.Linq;
using System.Linq.Expressions;

namespace SwitchConfigHelper
{
    public class SemanticInlineDiffBuilder : InlineDiffBuilder, IInlineDiffBuilder
    {
        public SemanticInlineDiffBuilder(IDiffer differ = null) : base(differ)
        {
        }

        public new SemanticDiffPaneModel BuildDiffModel(string oldText, string newText)
            => BuildDiffModel(oldText, newText, ignoreWhitespace: true);

        public SemanticDiffPaneModel BuildEffectiveDiffModel(string oldText, string newText, bool ignoreRemovedDuplicateAcls)
            => BuildEffectiveDiffModel(oldText, newText, ignoreRemovedDuplicateAcls, ignoreWhitespace: true);

        public new SemanticDiffPaneModel BuildDiffModel(string oldText, string newText, bool ignoreWhitespace)
        {
            var chunker = new SectionPreservingChunker();
            return BuildDiffModel(oldText, newText, ignoreWhitespace, false, chunker);
        }

        public SemanticDiffPaneModel BuildEffectiveDiffModel(string oldText, string newText, bool ignoreRemovedDuplicateAcls, bool ignoreWhitespace)
        {
            var chunker = new SectionPreservingChunker();
            return BuildEffectiveDiffModel(oldText, newText, ignoreRemovedDuplicateAcls, ignoreWhitespace, false, chunker);
        }
        
        public new SemanticDiffPaneModel BuildDiffModel(string oldText, string newText, bool ignoreWhitespace, bool ignoreCase, IChunker chunker)
        {
            var model = base.BuildDiffModel(oldText, newText, ignoreWhitespace, ignoreCase, chunker);
            model = RemoveSectionInformation(model);
            model = PerformSemanticShifts(model);
            return RemoveSectionInformation(new SemanticDiffPaneModel(model));
        }

        public SemanticDiffPaneModel BuildEffectiveDiffModel(string oldText, string newText, bool ignoreRemovedDuplicateAcls, bool ignoreWhitespace, bool ignoreCase, IChunker chunker)
        {
            var model = base.BuildDiffModel(oldText, newText, ignoreWhitespace, ignoreCase, chunker);
            model = RemoveSectionInformation(model);
            model = FindModifiedRemarkLines(model);
            model = PerformSemanticShifts(model);
            var semanticModel = new SemanticDiffPaneModel(model);
            return PerformSemanticShifts(FindEffectiveAclChanges(semanticModel, ignoreRemovedDuplicateAcls));
        }

        private enum AclType
        {
            None,
            Permit,
            Deny
        }

        public T FindModifiedRemarkLines<T>(T model) where T : DiffPaneModel
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
                return model;
            }
        }

        public SemanticDiffPaneModel FindEffectiveAclChanges(SemanticDiffPaneModel model, bool ignoreRemovedDuplicateAcls)
        {
            if (!model.HasDifferences)
            {
                return model;
            }
            else
            {
                var currentSectionRemovals = new List<EffectiveAclEntry>();
                var currentSectionAdditions = new List<EffectiveAclEntry>();
                var currentSectionUnchangeds = new List<EffectiveAclEntry>();
                var previousSectionStart = -1;
                var previousAclType = AclType.None;

                for (var i = 0; i < model.Lines.Count; i++)
                {
                    var currentLine = model.Lines[i];
                    var previousLine = i == 0 ? null : model.Lines[i - 1];
                    var currentSectionStart = model.Lines.IndexOf(model.Lines.Where(x => x.Position == currentLine.SectionStartPosition).First());
                    var currentSection = model.Lines[currentSectionStart];
                    var currentLineAclType = AclType.None;
                    if (currentLine.Text.Trim().StartsWith("permit"))
                    {
                        currentLineAclType = AclType.Permit;
                    }
                    else if (currentLine.Text.Trim().StartsWith("deny"))
                    {
                        currentLineAclType = AclType.Deny;
                    }

                    //Remove equivalent added/removed ACLs in a given section
                    //Sections are delimited by the actual section header changing
                    //or the end of the document
                    //or ACL types changing from permit to deny, or vice-versa
                    if (currentSectionStart != previousSectionStart
                        || i == model.Lines.Count - 1
                        || (currentLineAclType != AclType.None && previousAclType != AclType.None && currentLineAclType != previousAclType))
                    {
                        foreach (var acl in currentSectionRemovals)
                        {
                            //If an equivalent ACL was both removed and inserted
                            if (currentSectionAdditions.Contains(acl))
                            {
                                //Get rid of the removed ACL
                                model.Lines.Remove(acl.Piece);
                                i--; //Every line removed changes the index into model.Lines
                                var additionAcl = currentSectionAdditions.Find(a => a.Equals(acl));
                                //Consider the inserted ACL only a changed line
                                model.Lines[(model.Lines.IndexOf(additionAcl.Piece))].Type = ChangeType.Modified;
                            }
                            //If a duplicate ACL was removed and should be ignored
                            else if (ignoreRemovedDuplicateAcls
                                && acl.Piece.Type == ChangeType.Deleted
                                && currentSectionUnchangeds.Contains(acl))
                            {
                                var removalAcl = currentSectionRemovals.Find(a => a.Equals(acl));
                                //Consider the removed ACL only a changed line
                                model.Lines[(model.Lines.IndexOf(removalAcl.Piece))].Type = ChangeType.Modified;
                            }
                        }

                        currentSectionRemovals.Clear();
                        currentSectionAdditions.Clear();
                        currentSectionUnchangeds.Clear();
                    }

                    //Add inserted, deleted, or unchanged ACLs for checking above
                    if (isAclSection(currentSection.Text) && currentLineAclType != AclType.None)
                    {
                        switch (currentLine.Type)
                        {
                            case ChangeType.Inserted:
                                currentSectionAdditions.Add(new EffectiveAclEntry(currentLine));
                                break;
                            case ChangeType.Deleted:
                                currentSectionRemovals.Add(new EffectiveAclEntry(currentLine));
                                break;
                            case ChangeType.Unchanged:
                                currentSectionUnchangeds.Add(new EffectiveAclEntry(currentLine));
                                break;
                        }
                    }

                    previousSectionStart = currentSectionStart;
                    //Remarks or other entries that aren't permit/deny don't count as new sections
                    if (currentLineAclType != AclType.None)
                    {
                        previousAclType = currentLineAclType;
                    }    
                }
                return model;
            }
        }

        public T RemoveSectionInformation<T>(T model) where T : DiffPaneModel
        {
            for (var i = 0; i < model.Lines.Count; i++)
            {
                model.Lines[i].Text = SectionPreservingLineModifier.RemoveSectionInformation(model.Lines[i].Text);
            }
            return model;
        }

        //Take the base diff model, and analyze if blocks of changes can be shifted left or right
        //for better semantic alignment.  See 3.2.2 in https://neil.fraser.name/writing/diff/ for inspiration
        public T PerformSemanticShifts<T>(T model) where T: DiffPaneModel
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
                    if ((model.Lines[changeStart - 1].Type == ChangeType.Unchanged || model.Lines[changeStart - 1].Type == ChangeType.Modified)
                        && (model.Lines[changeStart].Type == ChangeType.Deleted || model.Lines[changeStart].Type == ChangeType.Inserted))
                    {
                        var changeEnd = changeStart;
                        while ((changeEnd < model.Lines.Count - 1)
                               && (model.Lines[changeEnd + 1].Type == model.Lines[changeStart].Type || model.Lines[changeEnd + 1].Type == ChangeType.Modified))
                        {
                            changeEnd++;
                        }

                        //A changed block surrounded by unchanged ones is a candidate for shifting left/right
                        if (changeEnd < model.Lines.Count - 1
                            && (model.Lines[changeEnd + 1].Type == ChangeType.Unchanged || model.Lines[changeEnd + 1].Type == ChangeType.Modified))
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
                                var modificationStart = Math.Min(newStart, changeStart);
                                var modificationEnd = Math.Max(newEnd, changeEnd);
                                var shiftAmount = Math.Abs(optimalShift);

                                //Save positions before they become modified
                                List<int> savedPositions = new List<int>();
                                var j = 0; //index into savedPositions
                                for (var i = modificationStart; i <= modificationEnd; i++)
                                {
                                    if (model.Lines[i].Position != null)
                                    {
                                        savedPositions.Add(model.Lines[i].Position.GetValueOrDefault());
                                    }
                                }

                                //Shift pieces
                                for (var i = modificationStart; i <= modificationEnd; i++)
                                {
                                    //shift left
                                    if (optimalShift < 0)
                                    {
                                        if (i < changeStart && model.Lines[i].Type != ChangeType.Modified)
                                        {
                                            model.Lines[i].Type = currentChange;
                                            if (currentChange == ChangeType.Deleted)
                                            {
                                                model.Lines[i].Position = null;
                                            }
                                        }
                                        else if (i > newEnd && model.Lines[i].Type != ChangeType.Modified)
                                        {
                                            model.Lines[i].Type = ChangeType.Unchanged;
                                            if (currentChange == ChangeType.Deleted)
                                            {
                                                model.Lines[i].Position = savedPositions[j];
                                                j++;
                                            }
                                        }
                                        else if (currentChange == ChangeType.Deleted && model.Lines[i].Type == ChangeType.Modified)
                                        {
                                            model.Lines[i].Position = savedPositions[j];
                                            j++;
                                        }
                                    }
                                    else
                                    {
                                        if (i < newStart && model.Lines[i].Type != ChangeType.Modified)
                                        {
                                            model.Lines[i].Type = ChangeType.Unchanged;
                                            if (currentChange == ChangeType.Deleted)
                                            {
                                                model.Lines[i].Position = savedPositions[j];
                                                j++;
                                            }
                                        }
                                        else if (i > changeEnd && model.Lines[i].Type != ChangeType.Modified)
                                        {
                                            model.Lines[i].Type = currentChange;
                                            if (currentChange == ChangeType.Deleted)
                                            {
                                                model.Lines[i].Position = null;
                                            }
                                        }
                                        else if (currentChange == ChangeType.Deleted && model.Lines[i].Type == ChangeType.Modified)
                                        {
                                            model.Lines[i].Position = savedPositions[j];
                                            j++;
                                        }
                                    }
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
        private bool isValidShift<T>(T model, int changeStart, int changeEnd, int shiftAmount) where T: DiffPaneModel
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
                        && (model.Lines[changeStart + shiftAmount].Type == ChangeType.Unchanged
                            || model.Lines[changeStart + shiftAmount].Type == ChangeType.Modified)
                        && model.Lines[changeStart + shiftAmount].Text == model.Lines[changeEnd + shiftAmount + 1].Text);
            }
            else if (shiftAmount > 0)
            {
                return (changeEnd + shiftAmount < model.Lines.Count
                        && (model.Lines[changeEnd + shiftAmount].Type == ChangeType.Unchanged
                            || model.Lines[changeEnd + shiftAmount].Type == ChangeType.Modified)
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