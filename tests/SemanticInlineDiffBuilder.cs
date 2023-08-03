using System.Net.Sockets;
using Xunit;
using FluentAssertions;
using DiffPlex.DiffBuilder;
using DiffPlex;
using DiffPlex.DiffBuilder.Model;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Net;

namespace SwitchConfigHelper.Tests
{
    public class ConfigDifferTests
    {
        public class TextLevelDiffs
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

                diff.Lines.Where(x => diff.Lines.IndexOf(x) >= 0 && diff.Lines.IndexOf(x) <= 3)
                    .Should().AllSatisfy(x => { x.Type.Should().Be(ChangeType.Unchanged); });
                diff.Lines.Where(x => diff.Lines.IndexOf(x) >= 4 && diff.Lines.IndexOf(x) <= 7)
                    .Should().AllSatisfy(x => { x.Type.Should().Be(ChangeType.Inserted); });

                diff.Lines.Should().BeInAscendingOrder(x => x.SectionStartPosition);
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
                diff.Lines.Should().BeInAscendingOrder(x => x.SectionStartPosition);

                diff.Lines.Should().SatisfyRespectively(
                    line =>
                    {
                        line.Position.Should().Be(1);
                        line.Text.Should().Be("ip access-list extended acl_vlan1");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("  remark Allow DNS");
                        line.Type.Should().Be(ChangeType.Deleted);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(2);
                        line.Text.Should().Be("  remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Inserted);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(4);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(5);
                        line.Text.Should().Be("ip access-list extended acl_vlan2");
                        line.Type.Should().Be(ChangeType.Inserted);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(6);
                        line.Text.Should().Be("  remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Inserted);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(7);
                        line.Text.Should().Be("  permit udp 172.20.2.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Inserted);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(8);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Inserted);
                        line.SectionStartPosition.Should().Be(5);
                    }
                );
            }

            [Fact]
            public void ForgottenSectionTerminatorInserted()
            {
                string referenceText = @"ip access-list extended acl_vlan1
  remark Allow DNS lookups
  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns
ip access-list extended acl_vlan2
  remark Allow DNS lookups
  permit udp 172.20.2.0/24 host 8.8.8.8 eq dns
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
                diff.Lines.Should().BeInAscendingOrder(x => x.SectionStartPosition);

                diff.Lines.Should().SatisfyRespectively(
                    line =>
                    {
                        line.Position.Should().Be(1);
                        line.Text.Should().Be("ip access-list extended acl_vlan1");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(2);
                        line.Text.Should().Be("  remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(4);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Inserted);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(5);
                        line.Text.Should().Be("ip access-list extended acl_vlan2");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(6);
                        line.Text.Should().Be("  remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(7);
                        line.Text.Should().Be("  permit udp 172.20.2.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(8);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(5);
                    }
                );
            }
        }

        public class EffectiveAclDiffs
        {
            [Fact]
            public void ReorderedAcls()
            {
                string referenceText = @"ip access-list extended acl_vlan1
  remark Allow DNS lookups
  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns
ip access-list extended acl_vlan2
  remark Allow DNS lookups from primary DNS
  permit udp 172.20.2.0/24 host 8.8.8.8 eq dns
  remark Allow DNS lookups from secondary DNS
  permit udp 172.20.2.0/24 host 8.8.4.4 eq dns
!
ip access-list extended acl_vlan3
  remark Allow DNS lookups
  permit udp 172.20.3.0/24 host 8.8.8.8 eq dns
!";

                string differenceText = @"ip access-list extended acl_vlan1
  remark Allow DNS lookups
  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns
ip access-list extended acl_vlan2
  remark Allow DNS lookups from secondary DNS
  permit udp 172.20.2.0/24 host 8.8.4.4 eq dns
  remark Allow DNS lookups from primary DNS
  permit udp 172.20.2.0/24 host 8.8.8.8 eq dns
!
ip access-list extended acl_vlan3
  remark Allow DNS lookups
  permit udp 172.20.3.0/24 host 8.8.8.8 eq dns
!";

                var diffBuilder = new SemanticInlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildEffectiveDiffModel(referenceText, differenceText);

                diff.Should().BeOfType<SemanticDiffPaneModel>();
                diff.Lines.Should().AllBeOfType<SemanticDiffPiece>();
                diff.Lines.Should().HaveCount(13);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().OnlyHaveUniqueItems(x => x.Position);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().BeInAscendingOrder(x => x.Position);
                diff.Lines.Should().BeInAscendingOrder(x => x.SectionStartPosition);

                diff.Lines.Should().SatisfyRespectively(
                    line =>
                    {
                        line.Position.Should().Be(1);
                        line.Text.Should().Be("ip access-list extended acl_vlan1");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(2);
                        line.Text.Should().Be("  remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(4);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(5);
                        line.Text.Should().Be("ip access-list extended acl_vlan2");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(6);
                        line.Text.Should().Be("  remark Allow DNS lookups from secondary DNS");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(7);
                        line.Text.Should().Be("  permit udp 172.20.2.0/24 host 8.8.4.4 eq dns");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(8);
                        line.Text.Should().Be("  remark Allow DNS lookups from primary DNS");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(9);
                        line.Text.Should().Be("  permit udp 172.20.2.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(10);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(11);
                        line.Text.Should().Be("ip access-list extended acl_vlan3");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(11);
                    },
                    line =>
                    {
                        line.Position.Should().Be(12);
                        line.Text.Should().Be("  remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(11);
                    },
                    line =>
                    {
                        line.Position.Should().Be(13);
                        line.Text.Should().Be("  permit udp 172.20.3.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(11);
                    }
                );
            }
        }
    }
}