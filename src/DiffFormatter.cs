using System;
using System.Text;
using DiffPlex.DiffBuilder.Model;

namespace SwitchConfigHelper
{
    public static class DiffFormatter
    {
        public static string FormatDiff(DiffPaneModel model, bool includeLineNumbers)
        {
            return FormatDiff(model, includeLineNumbers, true, 0, false, "");
        }

        public static string FormatDiff(DiffPaneModel model, bool includeLineNumbers, int context, bool printSectionHeaders, string trimmedLinesReplacement)
        {
            return FormatDiff(model, includeLineNumbers, false, context, printSectionHeaders, trimmedLinesReplacement);
        }

        public static string FormatDiff(DiffPaneModel model, bool includeLineNumbers, bool fullOutput, int context, bool printSectionHeaders, string trimmedLinesReplacement)
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

                if (currentLine.Text == "!")
                {
                    currentSection = null;
                    currentSectionStart = i;
                    currentSectionContextPrinted = false;
                }
                else if (currentSection == null)
                {
                    currentSection = currentLine.Text;
                    currentSectionStart = i;
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
                        //note that section headers that fit into the previous context won't be printed as a section header, but as context
                        if (printSectionHeaders && !currentSectionContextPrinted && currentSection != null && (i - currentSectionStart) > context)
                        {
                            //show trimmed lines before the current section header in the output, e.g. with "..."
                            if (trimmedLinesReplacement.Length > 0 && lastPrintedLine < currentSectionStart)
                            {
                                AddFormattedOutputLine(ref result, trimmedLinesReplacement, ChangeType.Unchanged, 0, includeLineNumbers);
                            }

                            //print the section header
                            AddFormattedOutputLine(ref result, model.Lines[currentSectionStart], includeLineNumbers);
                            lastPrintedLine = currentSectionStart;
                            currentSectionContextPrinted = true;
                        }

                        //show trimmed lines before the actual change, or the previous context of this change
                        if (trimmedLinesReplacement.Length > 0 && lastPrintedLine < Math.Max(i - context, lastForwardContext + 1) - 1)
                        {
                            AddFormattedOutputLine(ref result, trimmedLinesReplacement, ChangeType.Unchanged, 0, includeLineNumbers);
                        }

                        //print previous context, but only as far back as the already-printed forward context or the start of the document
                        for (var j = Math.Max(0, Math.Max(i - context, lastForwardContext + 1)); j <= i; j++)
                        {
                            AddFormattedOutputLine(ref result, model.Lines[j], includeLineNumbers);
                            lastPrintedLine = j;
                        }
                        lastForwardContext = i + context;
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
            if (trimmedLinesReplacement.Length > 0 && model.HasDifferences && lastPrintedLine < model.Lines.Count - 1)
            {
                AddFormattedOutputLine(ref result, trimmedLinesReplacement, ChangeType.Unchanged, 0, includeLineNumbers);
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
                default:
                    result.Append("  ");
                    break;
            }
            result.Append(line);
            result.Append(System.Environment.NewLine);
        }
    }
}