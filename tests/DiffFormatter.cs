using Xunit;
using DiffPlex.DiffBuilder.Model;

namespace SwitchConfigHelper.Tests
{
    public class DiffFormatterTests
    {
        public class FormatWithoutLineNumbers
        {
            [Fact]
            public void FullOutputNoChanges()
            {
                var model = new SemanticDiffPaneModel();
                model.Lines.Add(new SemanticDiffPiece("section 1", ChangeType.Unchanged, 1, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.1", ChangeType.Unchanged, 2, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.2", ChangeType.Unchanged, 3, 1));
                model.Lines.Add(new SemanticDiffPiece("!", ChangeType.Unchanged, 4, 1));
                model.Lines.Add(new SemanticDiffPiece("section 2", ChangeType.Unchanged, 5, 5));
                model.Lines.Add(new SemanticDiffPiece("statement 2.1", ChangeType.Unchanged, 6, 5));
                model.Lines.Add(new SemanticDiffPiece("statement 2.2", ChangeType.Unchanged, 7, 5));
                model.Lines.Add(new SemanticDiffPiece("!", ChangeType.Unchanged, 8, 5));

                string expectedOutput = @"  section 1
  statement 1.1
  statement 1.2
  !
  section 2
  statement 2.1
  statement 2.2
  !
";

                var diffOutput = DiffFormatter.FormatDiff(model, false, false);
                Assert.Equal(expectedOutput, diffOutput);
            }

            [Fact]
            public void FullOutputSimpleInsertAndDelete()
            {
                var model = new SemanticDiffPaneModel();
                model.Lines.Add(new SemanticDiffPiece("section 1", ChangeType.Unchanged, 1, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.1", ChangeType.Unchanged, 2, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.2", ChangeType.Inserted, 3, 1));
                model.Lines.Add(new SemanticDiffPiece("!", ChangeType.Unchanged, 4, 1));
                model.Lines.Add(new SemanticDiffPiece("section 2", ChangeType.Unchanged, 5, 5));
                model.Lines.Add(new SemanticDiffPiece("statement 2.1", ChangeType.Unchanged, 6, 5));
                model.Lines.Add(new SemanticDiffPiece("statement 2.2", ChangeType.Deleted, 7, 5));
                model.Lines.Add(new SemanticDiffPiece("!", ChangeType.Unchanged, 8, 5));

                string expectedOutput = @"  section 1
  statement 1.1
+ statement 1.2
  !
  section 2
  statement 2.1
- statement 2.2
  !
";

                var diffOutput = DiffFormatter.FormatDiff(model, false, false);
                Assert.Equal(expectedOutput, diffOutput);
            }

            [Fact]
            public void ContextOutputSectionHeaders()
            {
                var model = new SemanticDiffPaneModel();
                model.Lines.Add(new SemanticDiffPiece("section 1", ChangeType.Unchanged, 1, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.1", ChangeType.Unchanged, 2, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.2", ChangeType.Inserted, 3, 1));
                model.Lines.Add(new SemanticDiffPiece("!", ChangeType.Unchanged, 4, 1));
                model.Lines.Add(new SemanticDiffPiece("section 2", ChangeType.Unchanged, 5, 5));
                model.Lines.Add(new SemanticDiffPiece("statement 2.1", ChangeType.Unchanged, 6, 5));
                model.Lines.Add(new SemanticDiffPiece("statement 2.2", ChangeType.Deleted, null, 5));
                model.Lines.Add(new SemanticDiffPiece("!", ChangeType.Unchanged, 7, 5));

                string expectedOutput = @"  section 1
+ statement 1.2
  section 2
- statement 2.2
";

                var diffOutput = DiffFormatter.FormatDiff(model, false, 0, true, "", false);
                Assert.Equal(expectedOutput, diffOutput);
            }

            [Fact]
            public void ContextOutputSectionHeadersWithModifiedLines()
            {
                var model = new SemanticDiffPaneModel();
                model.Lines.Add(new SemanticDiffPiece("section 1", ChangeType.Unchanged, 1, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.1", ChangeType.Unchanged, 2, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.2", ChangeType.Modified, 3, 1));
                model.Lines.Add(new SemanticDiffPiece("!", ChangeType.Unchanged, 4, 1));
                model.Lines.Add(new SemanticDiffPiece("section 2", ChangeType.Unchanged, 5, 5));
                model.Lines.Add(new SemanticDiffPiece("statement 2.1", ChangeType.Unchanged, 6, 5));
                model.Lines.Add(new SemanticDiffPiece("statement 2.2", ChangeType.Modified, 7, 5));
                model.Lines.Add(new SemanticDiffPiece("!", ChangeType.Unchanged, 8, 5));

                string expectedOutput = @"  section 1
* statement 1.2
  section 2
* statement 2.2
";

                var diffOutput = DiffFormatter.FormatDiff(model, false, 0, true, "", false);
                Assert.Equal(expectedOutput, diffOutput);
            }

            [Fact]
            public void ContextOutputSectionHeadersWithContextAndTrimmedLines()
            {
                var model = new SemanticDiffPaneModel();
                model.Lines.Add(new SemanticDiffPiece("section 1", ChangeType.Unchanged, 1, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.1", ChangeType.Unchanged, 2, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.2", ChangeType.Unchanged, 3, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.3", ChangeType.Inserted, 4, 1));
                model.Lines.Add(new SemanticDiffPiece("!", ChangeType.Unchanged, 5, 1));
                model.Lines.Add(new SemanticDiffPiece("section 2", ChangeType.Unchanged, 6, 6));
                model.Lines.Add(new SemanticDiffPiece("statement 2.1", ChangeType.Deleted, null, 6));
                model.Lines.Add(new SemanticDiffPiece("statement 2.2", ChangeType.Unchanged, 7, 6));
                model.Lines.Add(new SemanticDiffPiece("statement 2.3", ChangeType.Unchanged, 8, 6));
                model.Lines.Add(new SemanticDiffPiece("statement 2.4", ChangeType.Unchanged, 9, 6));
                model.Lines.Add(new SemanticDiffPiece("!", ChangeType.Inserted, 10, 6));

                string expectedOutput = @"  section 1
  ...
  statement 1.2
+ statement 1.3
  !
  section 2
- statement 2.1
  statement 2.2
  ...
  statement 2.4
+ !
";

                var diffOutput = DiffFormatter.FormatDiff(model, false, 1, true, "...", false);
                Assert.Equal(expectedOutput, diffOutput);
            }

            [Fact]
            public void OnlyModificationsPrintsNothing()
            {
                var model = new SemanticDiffPaneModel();
                model.Lines.Add(new SemanticDiffPiece("section 1", ChangeType.Unchanged, 1, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.1", ChangeType.Unchanged, 2, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.2", ChangeType.Unchanged, 3, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.3", ChangeType.Unchanged, 4, 1));
                model.Lines.Add(new SemanticDiffPiece("!", ChangeType.Unchanged, 5, 1));

                string expectedOutput = "";

                var diffOutput = DiffFormatter.FormatDiff(model, false, 1, true, "...", true);
                Assert.Equal(expectedOutput, diffOutput);
            }

            [Fact]
            public void NoChangesPrintsNothing()
            {
                var model = new SemanticDiffPaneModel();
                model.Lines.Add(new SemanticDiffPiece("section 1", ChangeType.Unchanged, 1, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.1", ChangeType.Unchanged, 2, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.2", ChangeType.Unchanged, 3, 1));
                model.Lines.Add(new SemanticDiffPiece("statement 1.3", ChangeType.Unchanged, 4, 1));
                model.Lines.Add(new SemanticDiffPiece("!", ChangeType.Unchanged, 5, 1));

                string expectedOutput = "";

                var diffOutput = DiffFormatter.FormatDiff(model, false, 1, true, "...", false);
                Assert.Equal(expectedOutput, diffOutput);
            }
        }
    }
}