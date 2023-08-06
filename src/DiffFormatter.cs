using System;
using System.Text;
using System.Linq;
using DiffPlex.DiffBuilder.Model;

namespace SwitchConfigHelper
{
    public static class DiffFormatter
    {
        public static string FormatDiff(SemanticDiffPaneModel model, bool includeLineNumbers)
        {
            return FormatDiff(model, includeLineNumbers, true, 0, false, "");
        }

        public static string FormatDiff(SemanticDiffPaneModel model, bool includeLineNumbers, int context, bool printSectionHeaders, string trimmedLinesReplacement)
        {
            return FormatDiff(model, includeLineNumbers, false, context, printSectionHeaders, trimmedLinesReplacement);
        }

        public static string FormatDiff(SemanticDiffPaneModel model, bool includeLineNumbers, bool fullOutput, int context, bool printSectionHeaders, string trimmedLineMarker)
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
                    AddFormattedOutputLine(ref result, currentLine, includeLineNumbers);
                }
                //print only changed lines with context
                else
                {
                    //new change, and we're not printing forward context from an earlier change
                    if (lastForwardContext < i && (currentLine.Type == ChangeType.Inserted || currentLine.Type == ChangeType.Deleted))
                    {
                        //print section information
                        //note that section headers that are also included in previous context
                        //are printed as section headers, not as previous context
                        if (printSectionHeaders && !currentSectionContextPrinted)
                        {
                            //show trimmed lines before the current section header in the output, e.g. with "..."
                            if (trimmedLineMarker.Length > 0 && lastPrintedLine < currentSectionStart - 1)
                            {
                                AddFormattedOutputLine(ref result, trimmedLineMarker, ChangeType.Unchanged, 0, includeLineNumbers);
                            }

                            //print the section header
                            AddFormattedOutputLine(ref result, model.Lines[currentSectionStart], includeLineNumbers);
                            lastPrintedLine = currentSectionStart;
                            currentSectionContextPrinted = true;
                        }

                        //show trimmed lines before the actual change, or the previous context of this change
                        if (trimmedLineMarker.Length > 0 && lastPrintedLine < Math.Max(i - context, lastForwardContext + 1) - 1)
                        {
                            AddFormattedOutputLine(ref result, trimmedLineMarker, ChangeType.Unchanged, 0, includeLineNumbers);
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
                                AddFormattedOutputLine(ref result, model.Lines[j], includeLineNumbers);
                                lastPrintedLine = j;
                            }
                            lastForwardContext = i + context;
                        }
                        //if no context is to be printed, print the current line
                        else
                        {
                            AddFormattedOutputLine(ref result, model.Lines[i], includeLineNumbers);
                            lastPrintedLine = i;
                        }
                    }

                    //finish printing forward context from an earlier change
                    else if (lastForwardContext >= i)
                    {
                        //additional changes need more context
                        if (currentLine.Type == ChangeType.Inserted || currentLine.Type == ChangeType.Deleted)
                        {
                            lastForwardContext = i + context;
                        }
                        AddFormattedOutputLine(ref result, currentLine, includeLineNumbers);
                        lastPrintedLine = i;
                    }
                }
            }

            //show trimmed lines between the last change and the EOF
            if (trimmedLineMarker.Length > 0 && model.HasDifferences && lastPrintedLine < model.Lines.Count - 1)
            {
                AddFormattedOutputLine(ref result, trimmedLineMarker, ChangeType.Unchanged, 0, includeLineNumbers);
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

            AddFormattedOutputLine(ref result, line.Text, line.Type, position, includeLineNumbers);
        }

        private static void AddFormattedOutputLine(ref StringBuilder result, string line, ChangeType changeType, int position, bool includeLineNumbers)
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
                    result.Append("* ");
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