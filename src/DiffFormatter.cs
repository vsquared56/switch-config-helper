using System;
using DiffPlex;
using System.Text;
using DiffPlex.DiffBuilder.Model;

namespace SwitchConfigHelper
{
    public static class DiffFormatter
    {
        public static string FormatDiff(DiffPaneModel model)
        {
            return FormatDiff(model, true, 0, false);
        }

        public static string FormatDiff(DiffPaneModel model, int context, bool printSectionHeaders)
        {
            return FormatDiff(model, false, context, printSectionHeaders);
        }

        public static string FormatDiff(DiffPaneModel model, bool fullOutput, int context, bool printSectionHeaders)
        {
            string currentSection = null;
            int currentSectionStart = 0;
            bool currentSectionContextPrinted = false;
            int lastForwardContext = 0;
            System.Text.StringBuilder result = new System.Text.StringBuilder();

            for (int i = 0; i < model.Lines.Count; i++)
            {
                var line = model.Lines[i];

                if (line.Text == "!")
                {
                    currentSection = null;
                    currentSectionStart = i;
                    currentSectionContextPrinted = false;
                }
                else if (currentSection == null)
                {
                    currentSection = line.Text;
                    currentSectionStart = i;
                    currentSectionContextPrinted = false;
                }

                //Print all lines
                if (fullOutput)
                {
                    AddFormattedOutputLine(ref result, line);
                }
                //print only changed lines with context
                else
                {
                    //new change, and we're not printing forward context from an earlier change
                    if (lastForwardContext < i && (line.Type == ChangeType.Inserted || line.Type == ChangeType.Deleted))
                    {
                        //print section information
                        if (!printSectionHeaders && !currentSectionContextPrinted && currentSection != null && (i - currentSectionStart) > context)
                        {
                            AddFormattedOutputLine(ref result, model.Lines[currentSectionStart]);
                            if ((i - currentSectionStart) > context + 1)
                            {
                                result.AppendLine("\t  ...");
                            }
                            currentSectionContextPrinted = true;
                        }

                        //print previous context, but only as far back as the already-printed forward context or the start of the document
                        for (var j = Math.Max(0, Math.Max(i - context, lastForwardContext + 1)); j <= i; j++)
                        {
                            AddFormattedOutputLine(ref result, model.Lines[j]);
                        }
                        lastForwardContext = i + context;
                    }

                    //finish printing forward context from an earlier change
                    else if (lastForwardContext >= i)
                    {
                        //additional changes need more context
                        if (line.Type == ChangeType.Inserted || line.Type == ChangeType.Deleted)
                        {
                            lastForwardContext = i + context;
                        }
                        AddFormattedOutputLine(ref result, line);
                    }
                }
            }
            return result.ToString();
        }

        private static void AddFormattedOutputLine(ref StringBuilder result, DiffPiece line)
        {
            if (line.Position.HasValue)
            {
                result.Append(line.Position.Value);
            }

            result.Append('\t');
            switch (line.Type)
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
            result.Append(line.Text);
            result.Append(System.Environment.NewLine);
        }
    }
}