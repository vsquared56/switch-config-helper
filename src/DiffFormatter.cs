using System;
using System.Text;
using System.Linq;
using DiffPlex.DiffBuilder.Model;

namespace SwitchConfigHelper
{
    public static class DiffFormatter
    {
        public static string FormatDiff(SemanticDiffPaneModel model, bool includeLineNumbers, bool modifiedLinesAreUnchanged)
        {
            return FormatDiff(model, includeLineNumbers, true, 0, false, "", modifiedLinesAreUnchanged);
        }

        public static string FormatDiff(SemanticDiffPaneModel model, bool includeLineNumbers, int context, bool printSectionHeaders, string trimmedLinesReplacement, bool modifiedLinesAreUnchanged)
        {
            return FormatDiff(model, includeLineNumbers, false, context, printSectionHeaders, trimmedLinesReplacement, modifiedLinesAreUnchanged);
        }

        public static string FormatDiff(SemanticDiffPaneModel model, bool includeLineNumbers, bool fullOutput, int context, bool printSectionHeaders, string trimmedLineMarker, bool modifiedLinesAreUnchanged)
        {
            string currentSection = null;
            int currentSectionStart = 0;
            bool currentSectionContextPrinted = false;
            int lastPrintedLine = 0;
            int lastForwardContext = -1;
            System.Text.StringBuilder result = new System.Text.StringBuilder();

            for (int i = 0; i < model.Lines.Count; i++)
            {
                var currentLine = model.Lines[i];

                //new section
                if (i == 0 || currentLine.SectionStartPosition > model.Lines[i - 1].SectionStartPosition)
                {
                    currentSectionStart = model.Lines.IndexOf(model.Lines.Where(x => x.Position == currentLine.SectionStartPosition).First());
                    currentSection = model.Lines[currentSectionStart].Text;
                    currentSectionContextPrinted = false;
                }

                //Print all lines
                if (fullOutput)
                {
                    AddFormattedOutputLine(ref result, currentLine, includeLineNumbers, modifiedLinesAreUnchanged);
                }
                //print only changed lines with context
                else
                {
                    //new change, and we're not printing forward context from an earlier change
                    if (lastForwardContext < i
                        && ((!modifiedLinesAreUnchanged && currentLine.Type == ChangeType.Modified)
                            || currentLine.Type == ChangeType.Inserted
                            || currentLine.Type == ChangeType.Deleted))
                    {
                        //print section information
                        //note that section headers that are also included in previous context
                        //are printed as section headers, not as previous context
                        if (printSectionHeaders && !currentSectionContextPrinted)
                        {
                            //show trimmed lines before the current section header in the output, e.g. with "..."
                            if (trimmedLineMarker.Length > 0 && lastPrintedLine < currentSectionStart - 1)
                            {
                                AddFormattedOutputLine(ref result, trimmedLineMarker, ChangeType.Unchanged, 0, includeLineNumbers, modifiedLinesAreUnchanged);
                            }

                            //print the section header
                            AddFormattedOutputLine(ref result, model.Lines[currentSectionStart], includeLineNumbers, modifiedLinesAreUnchanged);
                            lastPrintedLine = currentSectionStart;
                            currentSectionContextPrinted = true;
                        }

                        //show trimmed lines before the actual change, or the previous context of this change
                        if (trimmedLineMarker.Length > 0 && lastPrintedLine < Math.Max(i - context, lastForwardContext + 1) - 1)
                        {
                            AddFormattedOutputLine(ref result, trimmedLineMarker, ChangeType.Unchanged, 0, includeLineNumbers, modifiedLinesAreUnchanged);
                        }

                        //print previous context and the current line,
                        //but only as far back as the start of the document,
                        //the already-printed forward context,
                        //or the line after the last printed (in case a section header was printed earlier)
                        if (context > 0)
                        {
                            int contextStart = new[] { 0, i - context, lastForwardContext + 1, lastPrintedLine + 1}.Max();
                            for (var j = contextStart; j <= i; j++)
                            {
                                AddFormattedOutputLine(ref result, model.Lines[j], includeLineNumbers, modifiedLinesAreUnchanged);
                                lastPrintedLine = j;
                            }
                            lastForwardContext = i + context;
                        }
                        //if no context is to be printed, print the current line
                        else
                        {
                            AddFormattedOutputLine(ref result, model.Lines[i], includeLineNumbers, modifiedLinesAreUnchanged);
                            lastPrintedLine = i;
                        }
                    }

                    //finish printing forward context from an earlier change
                    else if (lastForwardContext >= i)
                    {
                        //additional changes need more context
                        if ((!modifiedLinesAreUnchanged && currentLine.Type == ChangeType.Modified)
                            || currentLine.Type == ChangeType.Inserted
                            || currentLine.Type == ChangeType.Deleted)
                        {
                            lastForwardContext = i + context;
                        }
                        AddFormattedOutputLine(ref result, currentLine, includeLineNumbers, modifiedLinesAreUnchanged);
                        lastPrintedLine = i;
                    }
                }
            }

            //show trimmed lines between the last change and the EOF
            if (trimmedLineMarker.Length > 0 && model.HasDifferences && lastPrintedLine < model.Lines.Count - 1)
            {
                AddFormattedOutputLine(ref result, trimmedLineMarker, ChangeType.Unchanged, 0, includeLineNumbers, modifiedLinesAreUnchanged);
            }

            return result.ToString();
        }

        private static void AddFormattedOutputLine(ref StringBuilder result, DiffPiece line, bool includeLineNumbers)
        {
            var position = 0;
            if (line.Position.HasValue)
            {
                position = line.Position.Value;
            }

            AddFormattedOutputLine(ref result, line.Text, line.Type, position, includeLineNumbers, false);
        }

        private static void AddFormattedOutputLine(ref StringBuilder result, DiffPiece line, bool includeLineNumbers, bool modifiedLinesAreUnchanged)
        {
            var position = 0;
            if (line.Position.HasValue)
            {
                position = line.Position.Value;
            }

            AddFormattedOutputLine(ref result, line.Text, line.Type, position, includeLineNumbers, modifiedLinesAreUnchanged);
        }

        private static void AddFormattedOutputLine(ref StringBuilder result, string line, ChangeType changeType, int position, bool includeLineNumbers, bool modifiedLinesAreUnchanged)
        {
            if (includeLineNumbers)
            {
                if (position > 0)
                {
                    result.Append(position);
                }

                result.Append('\t');
            }

            switch (changeType)
            {
                case ChangeType.Inserted:
                    result.Append("+ ");
                    break;
                case ChangeType.Deleted:
                    result.Append("- ");
                    break;
                case ChangeType.Modified:
                    if (modifiedLinesAreUnchanged)
                    {
                        result.Append("  ");
                    }
                    else
                    {
                        result.Append("* ");
                    }
                    break;
                default:
                    result.Append("  ");
                    break;
            }
            result.Append(line);
            result.Append(System.Environment.NewLine);
        }
    }
}