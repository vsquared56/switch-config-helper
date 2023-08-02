using System.Net.Sockets;
using Xunit;
using FluentAssertions;
using DiffPlex.DiffBuilder;
using DiffPlex;
using DiffPlex.DiffBuilder.Model;

namespace SwitchConfigHelper.Tests
{
    public class ConfigDifferTests
    {
        public class FindDifferences
        {
            //When a config section is inserted, verify that the change begins with the inserted section
            //i.e. ip access-list extended acl_vlan2
            [Fact]
            public void InsertedSectionDiff()
            {
                string referenceText = @"ip access-list extended acl_vlan1
  remark Allow DNS lookups
  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns
!";

                string differenceText = @"ip access-list extended acl_vlan1
  remark Allow DNS lookups
  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns
!
ip access-list extended acl_vlan2
  remark Allow DNS lookups
  permit udp 172.20.2.0/24 host 8.8.8.8 eq dns
!";

                var diffBuilder = new SemanticInlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildDiffModel(referenceText, differenceText);

                diff.Should().BeOfType<SemanticDiffPaneModel>();
                diff.Lines.Should().AllBeOfType<SemanticDiffPiece>();
                diff.Lines.Should().HaveCount(8);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().OnlyHaveUniqueItems(x => x.Position);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().BeInAscendingOrder(x => x.Position);

                diff.Lines.Should().SatisfyRespectively(
                    line00 => { line00.Type.Should().Be(ChangeType.Unchanged); },
                    line01 => { line01.Type.Should().Be(ChangeType.Unchanged); },
                    line02 => { line02.Type.Should().Be(ChangeType.Unchanged); },
                    line03 => { line03.Type.Should().Be(ChangeType.Unchanged); },
                    line04 => { line04.Type.Should().Be(ChangeType.Inserted); },
                    line05 => { line05.Type.Should().Be(ChangeType.Inserted); },
                    line06 => { line06.Type.Should().Be(ChangeType.Inserted); },
                    line07 => { line07.Type.Should().Be(ChangeType.Inserted); }
                    );
            }
        }

        //When a config section is inserted and there is a change in the previous section,
        //verify that the change begins with the inserted section
        //i.e. ip access-list extended acl_vlan2
        //not the default behavior of Diffplex, which identifies a change at line 3 (i.e. '!')
        [Fact]
        public void InsertedSectionWithPreviousChangeDiff()
        {
            string referenceText = @"ip access-list extended acl_vlan1
  remark Allow DNS
  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns
!";

            string differenceText = @"ip access-list extended acl_vlan1
  remark Allow DNS lookups
  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns
!
ip access-list extended acl_vlan2
  remark Allow DNS lookups
  permit udp 172.20.2.0/24 host 8.8.8.8 eq dns
!";

            var diffBuilder = new SemanticInlineDiffBuilder(new Differ());
            var diff = diffBuilder.BuildDiffModel(referenceText, differenceText);

            diff.Should().BeOfType<SemanticDiffPaneModel>();
            diff.Lines.Should().AllBeOfType<SemanticDiffPiece>();
            diff.Lines.Should().HaveCount(9);
            diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().OnlyHaveUniqueItems(x => x.Position);
            diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().BeInAscendingOrder(x => x.Position);

            diff.Lines.Should().SatisfyRespectively(
                    line00 => { line00.Type.Should().Be(ChangeType.Unchanged); },
                    line01 => { line01.Type.Should().Be(ChangeType.Deleted); },
                    line02 => { line02.Type.Should().Be(ChangeType.Inserted); },
                    line03 => { line03.Type.Should().Be(ChangeType.Unchanged); },
                    line04 => { line04.Type.Should().Be(ChangeType.Unchanged); },
                    line05 => { line05.Type.Should().Be(ChangeType.Inserted); },
                    line06 => { line06.Type.Should().Be(ChangeType.Inserted); },
                    line07 => { line07.Type.Should().Be(ChangeType.Inserted); },
                    line08 => { line08.Type.Should().Be(ChangeType.Inserted); }
                    );
        }
    }
}