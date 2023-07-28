using Xunit;
using DiffPlex;
using DiffPlex.DiffBuilder.Model;
using SwitchConfigHelper;

namespace SwitchConfigHelper.Tests
{
    public class DiffFormatterTests
    {
        public class FormatWithoutLineNumbers
        {
            [Fact]
            public void NoChanges()
            {
                var model = new DiffPaneModel();
                model.Lines.Add(new DiffPiece("section 1", ChangeType.Unchanged, 1));
                model.Lines.Add(new DiffPiece("statement 1.1", ChangeType.Unchanged, 2));
                model.Lines.Add(new DiffPiece("statement 1.2", ChangeType.Unchanged, 3));
                model.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 4));
                model.Lines.Add(new DiffPiece("section 2", ChangeType.Unchanged, 5));
                model.Lines.Add(new DiffPiece("statement 2.1", ChangeType.Unchanged, 6));
                model.Lines.Add(new DiffPiece("statement 2.2", ChangeType.Unchanged, 7));
                model.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 8));

                string expectedOutput = @"  section 1
  statement 1.1
  statement 1.2
  !
  section 2
  statement 2.1
  statement 2.2
  !
";

                var diffOutput = DiffFormatter.FormatDiff(model, false);
                Assert.Equal(expectedOutput, diffOutput);
            }

            [Fact]
            public void SimpleInsertAndDelete()
            {
                var model = new DiffPaneModel();
                model.Lines.Add(new DiffPiece("section 1", ChangeType.Unchanged, 1));
                model.Lines.Add(new DiffPiece("statement 1.1", ChangeType.Unchanged, 2));
                model.Lines.Add(new DiffPiece("statement 1.2", ChangeType.Inserted, 3));
                model.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 4));
                model.Lines.Add(new DiffPiece("section 2", ChangeType.Unchanged, 5));
                model.Lines.Add(new DiffPiece("statement 2.1", ChangeType.Unchanged, 6));
                model.Lines.Add(new DiffPiece("statement 2.2", ChangeType.Deleted, 7));
                model.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 8));

                string expectedOutput = @"  section 1
  statement 1.1
+ statement 1.2
  !
  section 2
  statement 2.1
- statement 2.2
  !
";

                var diffOutput = DiffFormatter.FormatDiff(model, false);
                Assert.Equal(expectedOutput, diffOutput);
            }

            [Fact]
            public void SectionHeaders()
            {
                var model = new DiffPaneModel();
                model.Lines.Add(new DiffPiece("section 1", ChangeType.Unchanged, 1));
                model.Lines.Add(new DiffPiece("statement 1.1", ChangeType.Unchanged, 2));
                model.Lines.Add(new DiffPiece("statement 1.2", ChangeType.Inserted, 3));
                model.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 4));
                model.Lines.Add(new DiffPiece("section 2", ChangeType.Unchanged, 5));
                model.Lines.Add(new DiffPiece("statement 2.1", ChangeType.Unchanged, 6));
                model.Lines.Add(new DiffPiece("statement 2.2", ChangeType.Deleted));
                model.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 8));

                string expectedOutput = @"  section 1
+ statement 1.2
  section 2
- statement 2.2
";

                var diffOutput = DiffFormatter.FormatDiff(model, false, 0, true, "");
                Assert.Equal(expectedOutput, diffOutput);
            }

            [Fact]
            public void SectionHeadersWithContextAndTrimmedLines()
            {
                var model = new DiffPaneModel();
                model.Lines.Add(new DiffPiece("section 1", ChangeType.Unchanged, 1));
                model.Lines.Add(new DiffPiece("statement 1.1", ChangeType.Unchanged, 2));
                model.Lines.Add(new DiffPiece("statement 1.2", ChangeType.Unchanged, 3));
                model.Lines.Add(new DiffPiece("statement 1.3", ChangeType.Inserted, 4));
                model.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 5));
                model.Lines.Add(new DiffPiece("section 2", ChangeType.Unchanged, 6));
                model.Lines.Add(new DiffPiece("statement 2.1", ChangeType.Deleted));
                model.Lines.Add(new DiffPiece("statement 2.2", ChangeType.Unchanged, 7));
                model.Lines.Add(new DiffPiece("statement 2.3", ChangeType.Unchanged, 8));
                model.Lines.Add(new DiffPiece("statement 2.4", ChangeType.Unchanged, 9));
                model.Lines.Add(new DiffPiece("!", ChangeType.Inserted, 10));

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

                var diffOutput = DiffFormatter.FormatDiff(model, false, 1, true, "...");
                Assert.Equal(expectedOutput, diffOutput);
            }

            [Fact]
            public void NoChangesPrintsNothing()
            {
                var model = new DiffPaneModel();
                model.Lines.Add(new DiffPiece("section 1", ChangeType.Unchanged, 1));
                model.Lines.Add(new DiffPiece("statement 1.1", ChangeType.Unchanged, 2));
                model.Lines.Add(new DiffPiece("statement 1.2", ChangeType.Unchanged, 3));
                model.Lines.Add(new DiffPiece("statement 1.3", ChangeType.Unchanged, 4));
                model.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 5));

                string expectedOutput = "";

                var diffOutput = DiffFormatter.FormatDiff(model, false, 1, true, "...");
                Assert.Equal(expectedOutput, diffOutput);
            }
        }
    }
}