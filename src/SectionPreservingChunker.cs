using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder.Model;
using System;
using System.Reflection;

namespace SwitchConfigHelper
{
    internal static class SectionPreservingLineModifier
    {
        public static string InsertSectionInformation(string line, string section)
        {
            return "\uFFF9" + section + "\uFFFB" + line;
        }

        public static string RemoveSectionInformation(string line)
        {
            int start = line.LastIndexOf('\uFFF9');
            int end = line.IndexOf('\uFFFB', start);
            return line.Remove(start, end - start + 1);
        }
    }

    internal class SectionPreservingChunker:IChunker
    {
        private readonly string[] lineSeparators = new[] { "\r\n", "\r", "\n" };

        /// <summary>
        /// Gets the default singleton instance of the chunker.
        /// </summary>
        public static SectionPreservingChunker Instance { get; } = new SectionPreservingChunker();

        public string[] Chunk(string text)
        {
            var lineChunks = text.Split(lineSeparators, StringSplitOptions.None);

            string currentSectionStart = "";
            for (int i = 0; i < lineChunks.Length; i++)
            {
                var currentLine = lineChunks[i];
                //End of the current section
                if (currentLine == "!")
                {
                    currentSectionStart = "";
                }
                //Start of a new section contained in the output (i.e. a section that isn't deleted)
                else if (currentSectionStart == "")
                {
                    currentSectionStart = lineChunks[i];
                }

                lineChunks[i] = SectionPreservingLineModifier.InsertSectionInformation(lineChunks[i], currentSectionStart);
            }
            return lineChunks;
        }
    }
}
