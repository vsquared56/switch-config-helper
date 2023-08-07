using DiffPlex.DiffBuilder.Model;
using FluentAssertions;
using Xunit;
using Microsoft.CodeAnalysis;

namespace SwitchConfigHelper.Tests
{
    public class SemanticDiffPaneModelTests
    {
        public class ConstructorFromDiffPaneModel
        {

            [Fact]
            public void SimpleSection()
            {
                var originalModel = new DiffPaneModel();

                originalModel.Lines.Add(new DiffPiece("ip access-list extended acl_vlan1", ChangeType.Unchanged, 1));
                originalModel.Lines.Add(new DiffPiece("remark Allow DNS lookups", ChangeType.Unchanged, 2));
                originalModel.Lines.Add(new DiffPiece("permit udp 172.20.1.0 / 24 host 8.8.8.8 eq dns", ChangeType.Unchanged, 3));
                originalModel.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 4));

                var semanticModel = new SemanticDiffPaneModel(originalModel);

                semanticModel.Should().BeOfType<SemanticDiffPaneModel>();
                semanticModel.Lines.Should().AllBeOfType<SemanticDiffPiece>();
                semanticModel.Lines.Should().HaveCount(originalModel.Lines.Count);

                semanticModel.Lines.Should().AllSatisfy(x => { x.Type.Should().Be(ChangeType.Unchanged); });
                semanticModel.Lines.Where(x => x.Position <= 4)
                    .Should().AllSatisfy(x => { x.SectionStartPosition.Should().Be(1); });

                semanticModel.Lines.Where(x => x.Type != ChangeType.Deleted).Should().BeInAscendingOrder(x => x.SectionStartPosition);
            }

            [Fact]
            public void RepeatedSectionTerminators()
            {
                var originalModel = new DiffPaneModel();

                originalModel.Lines.Add(new DiffPiece("ip access-list extended acl_vlan1", ChangeType.Unchanged, 1));
                originalModel.Lines.Add(new DiffPiece("remark Allow DNS lookups", ChangeType.Unchanged, 2));
                originalModel.Lines.Add(new DiffPiece("permit udp 172.20.1.0 / 24 host 8.8.8.8 eq dns", ChangeType.Unchanged, 3));
                originalModel.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 4));
                originalModel.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 5));
                originalModel.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 6));
                originalModel.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 7));

                var semanticModel = new SemanticDiffPaneModel(originalModel);

                semanticModel.Should().BeOfType<SemanticDiffPaneModel>();
                semanticModel.Lines.Should().AllBeOfType<SemanticDiffPiece>();
                semanticModel.Lines.Should().HaveCount(originalModel.Lines.Count);

                semanticModel.Lines.Should().AllSatisfy(x => { x.Type.Should().Be(ChangeType.Unchanged); });
                semanticModel.Lines.Where(x => x.Position <= 4)
                    .Should().AllSatisfy(x => { x.SectionStartPosition.Should().Be(1); });
                semanticModel.Lines.Where(x => x.Position >= 5)
                    .Should().AllSatisfy(x => { x.SectionStartPosition.Should().Be(x.Position); });

                semanticModel.Lines.Where(x => x.Type != ChangeType.Deleted).Should().BeInAscendingOrder(x => x.SectionStartPosition);
            }

            [Fact]
            public void DeletedSection()
            {
                var originalModel = new DiffPaneModel();

                originalModel.Lines.Add(new DiffPiece("ip access-list extended acl_vlan1", ChangeType.Unchanged, 1));
                originalModel.Lines.Add(new DiffPiece("remark Allow DNS lookups", ChangeType.Unchanged, 2));
                originalModel.Lines.Add(new DiffPiece("permit udp 172.20.1.0 / 24 host 8.8.8.8 eq dns", ChangeType.Unchanged, 3));
                originalModel.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 4));
                originalModel.Lines.Add(new DiffPiece("ip access-list extended acl_vlan2", ChangeType.Deleted, null));
                originalModel.Lines.Add(new DiffPiece("remark Allow DNS lookups", ChangeType.Deleted, null));
                originalModel.Lines.Add(new DiffPiece("permit udp 172.20.1.0 / 24 host 8.8.8.8 eq dns", ChangeType.Deleted, null));
                originalModel.Lines.Add(new DiffPiece("!", ChangeType.Deleted, null));

                var semanticModel = new SemanticDiffPaneModel(originalModel);

                semanticModel.Should().BeOfType<SemanticDiffPaneModel>();
                semanticModel.Lines.Should().AllBeOfType<SemanticDiffPiece>();
                semanticModel.Lines.Should().HaveCount(originalModel.Lines.Count);

                semanticModel.Lines.Where(x => x.Type == ChangeType.Unchanged)
                    .Should().AllSatisfy(x => { x.SectionStartPosition.Should().Be(1); });
                semanticModel.Lines.Where(x => x.Type == ChangeType.Deleted)
                    .Should().AllSatisfy(x => { x.SectionStartPosition.Should().BeNull(); });

                semanticModel.Lines.Where(x => x.Type != ChangeType.Deleted).Should().BeInAscendingOrder(x => x.SectionStartPosition);
            }
        }
    }
}
