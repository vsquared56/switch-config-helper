using DiffPlex;
using DiffPlex.DiffBuilder.Model;
using Xunit;
using FluentAssertions;

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
                diff.Lines.Should().HaveCount(11);
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
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("ip access-list extended acl_vlan2");
                        line.Type.Should().Be(ChangeType.Deleted);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("  remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Deleted);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("  permit udp 172.20.2.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Deleted);
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
  remark Allow DNS lookups from secondary DNS
  permit udp 172.20.1.0/24 host 8.8.4.4 eq dns
  remark Allow DNS lookups from primary DNS
  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns
!
ip access-list extended acl_vlan2
  permit udp 172.20.2.0/24 host 8.8.4.4 eq dns
  permit udp 172.20.2.0/24 host 8.8.8.8 eq dns
!
ip access-list extended acl_vlan3
  remark Allow DNS lookups
  permit udp 172.20.3.0/24 host 8.8.8.8 eq dns
!";

                string differenceText = @"ip access-list extended acl_vlan1
  remark Allow DNS lookups from primary DNS
  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns
  remark Allow DNS lookups from secondary DNS
  permit udp 172.20.1.0/24 host 8.8.4.4 eq dns
!
ip access-list extended acl_vlan2
  permit udp 172.20.2.0/24 host 8.8.8.8 eq dns
  permit udp 172.20.2.0/24 host 8.8.4.4 eq dns
!
ip access-list extended acl_vlan3
  remark Allow DNS lookups
  permit udp 172.20.3.0/24 host 8.8.8.8 eq dns
!";

                var diffBuilder = new SemanticInlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildEffectiveDiffModel(referenceText, differenceText, false);

                diff.Should().BeOfType<SemanticDiffPaneModel>();
                diff.Lines.Should().AllBeOfType<SemanticDiffPiece>();
                diff.Lines.Should().HaveCount(14);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().OnlyHaveUniqueItems(x => x.Position);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().BeInAscendingOrder(x => x.Position);
                diff.Lines.Should().BeInAscendingOrder(x => x.SectionStartPosition);

                diff.Lines.Where(x => x.Text.Trim().StartsWith("remark"))
                    .Should().AllSatisfy(x => x.Type.Should().BeOneOf(ChangeType.Unchanged, ChangeType.Modified));

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
                        line.Text.Should().Be("  remark Allow DNS lookups from primary DNS");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(4);
                        line.Text.Should().Be("  remark Allow DNS lookups from secondary DNS");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(5);
                        line.Text.Should().Be("  permit udp 172.20.1.0/24 host 8.8.4.4 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(6);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(7);
                        line.Text.Should().Be("ip access-list extended acl_vlan2");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(7);
                    },
                    line =>
                    { // Test that the first line in a section is also considered
                      // when evaluating effective ACL changes.
                      // Other tests include remarks as the first line.
                        line.Position.Should().Be(8);
                        line.Text.Should().Be("  permit udp 172.20.2.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(7);
                    },
                    line =>
                    {
                        line.Position.Should().Be(9);
                        line.Text.Should().Be("  permit udp 172.20.2.0/24 host 8.8.4.4 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(7);
                    },
                    line =>
                    {
                        line.Position.Should().Be(10);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(7);
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
                    },
                    line =>
                    {
                        line.Position.Should().Be(14);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(11);
                    }
                );
            }

            [Fact]
            public void ReorderedAclsWithChangedRemarks()
            {
                string referenceText = @"ip access-list extended acl_vlan1
  remark Allow DNS lookups only from 8.8.8.8
  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns
  permit udp 172.20.1.0/24 host 8.8.4.4 eq dns
!
ip access-list extended acl_vlan2
  remark Allow DNS lookups from secondary DNS (UDP)
  permit udp 172.20.2.0/24 host 8.8.4.4 eq dns
  remark Allow DNS lookups from secondary DNS (TCP)
  permit tcp 172.20.2.0/24 host 8.8.4.4 eq dns
  remark Allow DNS lookups from primary DNS (UDP)
  permit udp 172.20.2.0/24 host 8.8.8.8 eq dns
  remark Allow DNS lookups from primary DNS (TCP)
  permit tcp 172.20.2.0/24 host 8.8.8.8 eq dns
!
ip access-list extended acl_vlan3
  remark Allow DNS lookups
  permit udp 172.20.3.0/24 host 8.8.8.8 eq dns
!";

                string differenceText = @"ip access-list extended acl_vlan1
  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns
  remark Allow DNS lookups only from 8.8.4.4
  permit udp 172.20.1.0/24 host 8.8.4.4 eq dns
!
ip access-list extended acl_vlan2
  remark Allow UDP DNS lookups from primary
  permit udp 172.20.2.0/24 host 8.8.8.8 eq dns
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.2.0/24 host 8.8.8.8 eq dns
  remark Allow UDP DNS lookups from secondary
  permit udp 172.20.2.0/24 host 8.8.4.4 eq dns
  remark Allow TCP DNS lookups from secondary
  permit tcp 172.20.2.0/24 host 8.8.4.4 eq dns
!
ip access-list extended acl_vlan3
  remark Allow DNS lookups only from 8.8.8.8
  permit udp 172.20.3.0/24 host 8.8.8.8 eq dns
!";

                var diffBuilder = new SemanticInlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildEffectiveDiffModel(referenceText, differenceText, false);

                diff.Should().BeOfType<SemanticDiffPaneModel>();
                diff.Lines.Should().AllBeOfType<SemanticDiffPiece>();
                diff.Lines.Should().HaveCount(19);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().OnlyHaveUniqueItems(x => x.Position);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().BeInAscendingOrder(x => x.Position);
                diff.Lines.Should().BeInAscendingOrder(x => x.SectionStartPosition);

                diff.Lines.Where(x => x.Text.Trim().StartsWith("remark"))
                    .Should().AllSatisfy(x => x.Type.Should().BeOneOf(ChangeType.Unchanged, ChangeType.Modified));

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
                        line.Text.Should().Be("  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("  remark Allow DNS lookups only from 8.8.4.4");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(4);
                        line.Text.Should().Be("  permit udp 172.20.1.0/24 host 8.8.4.4 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(5);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(6);
                        line.Text.Should().Be("ip access-list extended acl_vlan2");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(6);
                    },
                    line =>
                    {
                        line.Position.Should().Be(7);
                        line.Text.Should().Be("  remark Allow UDP DNS lookups from primary");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(6);
                    },
                    line =>
                    {
                        line.Position.Should().Be(8);
                        line.Text.Should().Be("  permit udp 172.20.2.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(6);
                    },
                    line =>
                    {
                        line.Position.Should().Be(9);
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from primary");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(6);
                    },
                    line =>
                    {
                        line.Position.Should().Be(10);
                        line.Text.Should().Be("  permit tcp 172.20.2.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(6);
                    },
                    line =>
                    {
                        line.Position.Should().Be(11);
                        line.Text.Should().Be("  remark Allow UDP DNS lookups from secondary");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(6);
                    },
                    line =>
                    {
                        line.Position.Should().Be(12);
                        line.Text.Should().Be("  permit udp 172.20.2.0/24 host 8.8.4.4 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(6);
                    },
                    line =>
                    {
                        line.Position.Should().Be(13);
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from secondary");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(6);
                    },
                    line =>
                    {
                        line.Position.Should().Be(14);
                        line.Text.Should().Be("  permit tcp 172.20.2.0/24 host 8.8.4.4 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(6);
                    },
                    line =>
                    {
                        line.Position.Should().Be(15);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(6);
                    },
                    line =>
                    {
                        line.Position.Should().Be(16);
                        line.Text.Should().Be("ip access-list extended acl_vlan3");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(16);
                    },
                    line =>
                    {
                        line.Position.Should().Be(17);
                        line.Text.Should().Be("  remark Allow DNS lookups only from 8.8.8.8");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(16);
                    },
                    line =>
                    {
                        line.Position.Should().Be(18);
                        line.Text.Should().Be("  permit udp 172.20.3.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(16);
                    },
                    line =>
                    {
                        line.Position.Should().Be(19);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(16);
                    }
                );
            }


            [Fact]
            public void ReorderedAclsPermitDeny()
            {
                string referenceText = @"ip access-list extended acl_vlan1
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
  remark Deny all other TCP connections
  deny tcp any any
  remark Allow TCP DNS lookups from secondary
  permit tcp 172.20.1.0/24 host 8.8.4.4 eq dns
  remark Allow UDP DNS lookups from secondary
  permit udp 172.20.1.0/24 host 8.8.4.4 eq dns
  remark Allow UDP DNS lookups from primary
  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns
!";

                string differenceText = @"ip access-list extended acl_vlan1
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
  remark Allow TCP DNS lookups from secondary
  permit tcp 172.20.1.0/24 host 8.8.4.4 eq dns
  remark Deny all other TCP connections
  deny tcp any any
  remark Allow UDP DNS lookups from primary
  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns
  remark Allow UDP DNS lookups from secondary
  permit udp 172.20.1.0/24 host 8.8.4.4 eq dns
!";

                var diffBuilder = new SemanticInlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildEffectiveDiffModel(referenceText, differenceText, false);

                diff.Should().BeOfType<SemanticDiffPaneModel>();
                diff.Lines.Should().AllBeOfType<SemanticDiffPiece>();
                diff.Lines.Should().HaveCount(13);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().OnlyHaveUniqueItems(x => x.Position);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().BeInAscendingOrder(x => x.Position);
                diff.Lines.Should().BeInAscendingOrder(x => x.SectionStartPosition);

                diff.Lines.Where(x => x.Text.Trim().StartsWith("remark"))
                    .Should().AllSatisfy(x => x.Type.Should().BeOneOf(ChangeType.Unchanged, ChangeType.Modified));

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
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from primary");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(4);
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from secondary");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(5);
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.4.4 eq dns");
                        line.Type.Should().Be(ChangeType.Inserted);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(6);
                        line.Text.Should().Be("  remark Deny all other TCP connections");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(7);
                        line.Text.Should().Be("  deny tcp any any");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.4.4 eq dns");
                        line.Type.Should().Be(ChangeType.Deleted);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(8);
                        line.Text.Should().Be("  remark Allow UDP DNS lookups from primary");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(9);
                        line.Text.Should().Be("  permit udp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(10);
                        line.Text.Should().Be("  remark Allow UDP DNS lookups from secondary");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(11);
                        line.Text.Should().Be("  permit udp 172.20.1.0/24 host 8.8.4.4 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(12);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    }
                );
            }

            [Fact]
            public void DuplicateAclsRemovedButIgnored()
            {
                string referenceText = @"ip access-list extended acl_vlan1
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
  remark Allow TCP DNS lookups from secondary
  permit tcp 172.20.1.0/24 host 8.8.4.4 eq dns
!";

                string differenceText = @"ip access-list extended acl_vlan1
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
  remark Allow TCP DNS lookups from secondary
  permit tcp 172.20.1.0/24 host 8.8.4.4 eq dns
!";

                var diffBuilder = new SemanticInlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildEffectiveDiffModel(referenceText, differenceText, true);

                diff.Should().BeOfType<SemanticDiffPaneModel>();
                diff.Lines.Should().AllBeOfType<SemanticDiffPiece>();
                diff.Lines.Should().HaveCount(7);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().OnlyHaveUniqueItems(x => x.Position);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted && x.Type != ChangeType.Modified).Should().BeInAscendingOrder(x => x.Position);
                diff.Lines.Should().BeInAscendingOrder(x => x.SectionStartPosition);

                diff.Lines.Where(x => x.Text.Trim().StartsWith("remark"))
                    .Should().AllSatisfy(x => x.Type.Should().BeOneOf(ChangeType.Unchanged, ChangeType.Modified));

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
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from primary");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(4);
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from secondary");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(5);
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.4.4 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(6);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    }
                );
            }

            [Fact]
            public void AllDuplicateAclsRemovedMarkedRemoved()
            {
                string referenceText = @"ip access-list extended acl_vlan1
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
  remark Allow TCP DNS lookups from secondary
  permit tcp 172.20.1.0/24 host 8.8.4.4 eq dns
!";

                string differenceText = @"ip access-list extended acl_vlan1
  remark Allow TCP DNS lookups from secondary
  permit tcp 172.20.1.0/24 host 8.8.4.4 eq dns
!";

                var diffBuilder = new SemanticInlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildEffectiveDiffModel(referenceText, differenceText, true);

                diff.Should().BeOfType<SemanticDiffPaneModel>();
                diff.Lines.Should().AllBeOfType<SemanticDiffPiece>();
                diff.Lines.Should().HaveCount(6);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().OnlyHaveUniqueItems(x => x.Position);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted && x.Type != ChangeType.Modified).Should().BeInAscendingOrder(x => x.Position);
                diff.Lines.Should().BeInAscendingOrder(x => x.SectionStartPosition);

                diff.Lines.Where(x => x.Text.Trim().StartsWith("remark"))
                    .Should().AllSatisfy(x => x.Type.Should().BeOneOf(ChangeType.Unchanged, ChangeType.Modified));

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
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Deleted);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Deleted);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(2);
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from secondary");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.4.4 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(4);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    }
                );
            }

            [Fact]
            public void DuplicateAclsRemovedNotIgnored()
            {
                string referenceText = @"ip access-list extended acl_vlan1
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
  remark Allow TCP DNS lookups from secondary
  permit tcp 172.20.1.0/24 host 8.8.4.4 eq dns
!";

                string differenceText = @"ip access-list extended acl_vlan1
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
  remark Allow TCP DNS lookups from secondary
  permit tcp 172.20.1.0/24 host 8.8.4.4 eq dns
!";

                var diffBuilder = new SemanticInlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildEffectiveDiffModel(referenceText, differenceText, false);

                diff.Should().BeOfType<SemanticDiffPaneModel>();
                diff.Lines.Should().AllBeOfType<SemanticDiffPiece>();
                diff.Lines.Should().HaveCount(7);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().OnlyHaveUniqueItems(x => x.Position);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().BeInAscendingOrder(x => x.Position);
                diff.Lines.Should().BeInAscendingOrder(x => x.SectionStartPosition);

                diff.Lines.Where(x => x.Text.Trim().StartsWith("remark"))
                    .Should().AllSatisfy(x => x.Type.Should().BeOneOf(ChangeType.Unchanged, ChangeType.Modified));

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
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from primary");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Deleted);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(4);
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from secondary");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(5);
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.4.4 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(6);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    }
                );
            }

            [Fact]
            public void DuplicateAclsAdded()
            {
                string referenceText = @"ip access-list extended acl_vlan1
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
  remark Allow TCP DNS lookups from secondary
  permit tcp 172.20.1.0/24 host 8.8.4.4 eq dns
!";

                string differenceText = @"ip access-list extended acl_vlan1
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
  remark Allow TCP DNS lookups from secondary
  permit tcp 172.20.1.0/24 host 8.8.4.4 eq dns
!";

                var diffBuilder = new SemanticInlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildEffectiveDiffModel(referenceText, differenceText, true);

                diff.Should().BeOfType<SemanticDiffPaneModel>();
                diff.Lines.Should().AllBeOfType<SemanticDiffPiece>();
                diff.Lines.Should().HaveCount(8);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().OnlyHaveUniqueItems(x => x.Position);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().BeInAscendingOrder(x => x.Position);
                diff.Lines.Should().BeInAscendingOrder(x => x.SectionStartPosition);

                diff.Lines.Where(x => x.Text.Trim().StartsWith("remark"))
                    .Should().AllSatisfy(x => x.Type.Should().BeOneOf(ChangeType.Unchanged, ChangeType.Modified));

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
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from primary");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(4);
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from primary");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(5);
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Inserted);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(6);
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from secondary");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(7);
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.4.4 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(8);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    }
                );
            }

            [Fact]
            public void SemanticShiftAfterModifiedLine()
            {
                string referenceText = @"ip access-list extended acl_vlan1
  remark Allow TCP DNS lookups from primarie
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
!
ip access-list extended acl_vlan3
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
!";

                string differenceText = @"ip access-list extended acl_vlan1
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
!
ip access-list extended acl_vlan2
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
!
ip access-list extended acl_vlan3
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
!";

                var diffBuilder = new SemanticInlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildEffectiveDiffModel(referenceText, differenceText, true);

                diff.Should().BeOfType<SemanticDiffPaneModel>();
                diff.Lines.Should().AllBeOfType<SemanticDiffPiece>();
                diff.Lines.Should().HaveCount(12);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().OnlyHaveUniqueItems(x => x.Position);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().BeInAscendingOrder(x => x.Position);
                diff.Lines.Should().BeInAscendingOrder(x => x.SectionStartPosition);

                diff.Lines.Where(x => x.Text.Trim().StartsWith("remark"))
                    .Should().AllSatisfy(x => x.Type.Should().BeOneOf(ChangeType.Unchanged, ChangeType.Modified));

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
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from primary");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns");
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
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from primary");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(7);
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Inserted);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(8);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Inserted);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(9);
                        line.Text.Should().Be("ip access-list extended acl_vlan3");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(9);
                    },
                    line =>
                    {
                        line.Position.Should().Be(10);
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from primary");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(9);
                    },
                    line =>
                    {
                        line.Position.Should().Be(11);
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(9);
                    },
                    line =>
                    {
                        line.Position.Should().Be(12);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(9);
                    }
                );
            }

            [Fact]
            public void SemanticShiftAcrossSections()
            {
                string referenceText = @"ip access-list extended acl_vlan1
  remark Allow TCP DNS lookups from primarie
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
!
ip access-list extended acl_vlan2
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
!
ip access-list extended acl_vlan3
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.3.0/24 host 8.8.8.8 eq dns
!";

                string differenceText = @"ip access-list extended acl_vlan1
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns
!
ip access-list extended acl_vlan3
  remark Allow TCP DNS lookups from primary
  permit tcp 172.20.3.0/24 host 8.8.8.8 eq dns
!";

                var diffBuilder = new SemanticInlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildEffectiveDiffModel(referenceText, differenceText, true);

                diff.Should().BeOfType<SemanticDiffPaneModel>();
                diff.Lines.Should().AllBeOfType<SemanticDiffPiece>();
                diff.Lines.Should().HaveCount(11);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().OnlyHaveUniqueItems(x => x.Position);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().BeInAscendingOrder(x => x.Position);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().BeInAscendingOrder(x => x.SectionStartPosition);

                diff.Lines.Where(x => x.Text.Trim().StartsWith("remark"))
                    .Should().AllSatisfy(x => x.Type.Should().BeOneOf(ChangeType.Unchanged, ChangeType.Modified));

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
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from primary");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns");
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
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("ip access-list extended acl_vlan2");
                        line.Type.Should().Be(ChangeType.Deleted);
                        line.SectionStartPosition.Should().BeNull();
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("  permit tcp 172.20.1.0/24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Deleted);
                        line.SectionStartPosition.Should().BeNull();
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Deleted);
                        line.SectionStartPosition.Should().BeNull();
                    },
                    line =>
                    {
                        line.Position.Should().Be(5);
                        line.Text.Should().Be("ip access-list extended acl_vlan3");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(6);
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from primary");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(7);
                        line.Text.Should().Be("  permit tcp 172.20.3.0/24 host 8.8.8.8 eq dns");
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

            [Fact]
            public void RemovedAclMovedInAnotherSection()
            {
                string referenceText = @"ip access-list extended acl_vlan1
  remark Allow TCP DNS lookups from primary
  permit tcp any host 8.8.8.8 eq dns
!
ip access-list extended acl_vlan2
  remark Allow TCP DNS lookups from primary
  permit tcp any host 8.8.8.8 eq dns
!
ip access-list extended acl_vlan3
  remark Allow TCP DNS lookups from secondary
  permit tcp any host 8.8.4.4 eq dns
  remark Allow TCP DNS lookups from primary
  permit tcp any host 8.8.8.8 eq dns
!";

                string differenceText = @"ip access-list extended acl_vlan1
  remark Allow TCP DNS lookups from primary
  permit tcp any host 8.8.8.8 eq dns
!
ip access-list extended acl_vlan3
  remark Allow TCP DNS lookups from primary
  permit tcp any host 8.8.8.8 eq dns
  remark Allow TCP DNS lookups from secondary
  permit tcp any host 8.8.4.4 eq dns
!";

                var diffBuilder = new SemanticInlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildEffectiveDiffModel(referenceText, differenceText, true);

                diff.Should().BeOfType<SemanticDiffPaneModel>();
                diff.Lines.Should().AllBeOfType<SemanticDiffPiece>();
                diff.Lines.Should().HaveCount(13);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().OnlyHaveUniqueItems(x => x.Position);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().BeInAscendingOrder(x => x.Position);
                diff.Lines.Where(x => x.Type != ChangeType.Deleted).Should().BeInAscendingOrder(x => x.SectionStartPosition);

                diff.Lines.Where(x => x.Text.Trim().StartsWith("remark"))
                    .Should().AllSatisfy(x => x.Type.Should().BeOneOf(ChangeType.Unchanged, ChangeType.Modified));

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
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from primary");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(1);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("  permit tcp any host 8.8.8.8 eq dns");
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
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("ip access-list extended acl_vlan2");
                        line.Type.Should().Be(ChangeType.Deleted);
                        line.SectionStartPosition.Should().BeNull();
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("  permit tcp any host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Deleted);
                        line.SectionStartPosition.Should().BeNull();
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Deleted);
                        line.SectionStartPosition.Should().BeNull();
                    },
                    line =>
                    {
                        line.Position.Should().Be(5);
                        line.Text.Should().Be("ip access-list extended acl_vlan3");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(6);
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from primary");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(7);
                        line.Text.Should().Be("  permit tcp any host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Modified);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(8);
                        line.Text.Should().Be("  remark Allow TCP DNS lookups from secondary");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(9);
                        line.Text.Should().Be("  permit tcp any host 8.8.4.4 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(5);
                    },
                    line =>
                    {
                        line.Position.Should().Be(10);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                        line.SectionStartPosition.Should().Be(5);
                    }
                );
            }
        }
    }

    public class SemanticShiftTests
    {
        [Fact]
        public void ShiftLeftDeletion()
        {
            var originalModel = new DiffPaneModel();

            originalModel.Lines.Add(new DiffPiece("ip access-list extended acl_vlan1", ChangeType.Unchanged, 1));
            originalModel.Lines.Add(new DiffPiece("remark Allow DNS lookups", ChangeType.Unchanged, 2));
            originalModel.Lines.Add(new DiffPiece("permit udp 172.20.1.0 / 24 host 8.8.8.8 eq dns", ChangeType.Unchanged, 3));
            originalModel.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 4));
            originalModel.Lines.Add(new DiffPiece("ip access-list extended acl_vlan2", ChangeType.Unchanged, 5));
            originalModel.Lines.Add(new DiffPiece("remark Allow DNS lookups", ChangeType.Unchanged, 6));
            originalModel.Lines.Add(new DiffPiece("permit udp 172.20.1.0 / 24 host 8.8.8.8 eq dns", ChangeType.Deleted, null));
            originalModel.Lines.Add(new DiffPiece("!", ChangeType.Deleted, null));
            originalModel.Lines.Add(new DiffPiece("ip access-list extended acl_vlan2", ChangeType.Deleted, null));
            originalModel.Lines.Add(new DiffPiece("remark Allow DNS lookups", ChangeType.Deleted, null));
            originalModel.Lines.Add(new DiffPiece("permit udp 172.20.2.0 / 24 host 8.8.8.8 eq dns", ChangeType.Unchanged, 7));
            originalModel.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 8));

            var diffBuilder = new SemanticInlineDiffBuilder();
            var shiftedModel = diffBuilder.PerformSemanticShifts(originalModel);

            shiftedModel.Should().BeOfType<DiffPaneModel>();
            shiftedModel.Lines.Should().AllBeOfType<DiffPiece>();
            shiftedModel.Lines.Should().HaveCount(originalModel.Lines.Count);

            shiftedModel.Lines.Should().SatisfyRespectively(
                    line =>
                    {
                        line.Position.Should().Be(1);
                        line.Text.Should().Be("ip access-list extended acl_vlan1");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(2);
                        line.Text.Should().Be("remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("permit udp 172.20.1.0 / 24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(4);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("ip access-list extended acl_vlan2");
                        line.Type.Should().Be(ChangeType.Deleted);
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Deleted);
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("permit udp 172.20.1.0 / 24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Deleted);
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Deleted);
                    },
                    line =>
                    {
                        line.Position.Should().Be(5);
                        line.Text.Should().Be("ip access-list extended acl_vlan2");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(6);
                        line.Text.Should().Be("remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(7);
                        line.Text.Should().Be("permit udp 172.20.2.0 / 24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(8);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    });
        }

        [Fact]
        public void ShiftLeftInsertion()
        {
            var originalModel = new DiffPaneModel();

            originalModel.Lines.Add(new DiffPiece("ip access-list extended acl_vlan1", ChangeType.Unchanged, 1));
            originalModel.Lines.Add(new DiffPiece("remark Allow DNS lookups", ChangeType.Unchanged, 2));
            originalModel.Lines.Add(new DiffPiece("permit udp 172.20.1.0 / 24 host 8.8.8.8 eq dns", ChangeType.Unchanged, 3));
            originalModel.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 4));
            originalModel.Lines.Add(new DiffPiece("ip access-list extended acl_vlan2", ChangeType.Unchanged, 5));
            originalModel.Lines.Add(new DiffPiece("remark Allow DNS lookups", ChangeType.Unchanged, 6));
            originalModel.Lines.Add(new DiffPiece("permit udp 172.20.1.0 / 24 host 8.8.8.8 eq dns", ChangeType.Inserted, 7));
            originalModel.Lines.Add(new DiffPiece("!", ChangeType.Inserted, 8));
            originalModel.Lines.Add(new DiffPiece("ip access-list extended acl_vlan2", ChangeType.Inserted, 9));
            originalModel.Lines.Add(new DiffPiece("remark Allow DNS lookups", ChangeType.Inserted, 10));
            originalModel.Lines.Add(new DiffPiece("permit udp 172.20.2.0 / 24 host 8.8.8.8 eq dns", ChangeType.Unchanged, 11));
            originalModel.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 12));

            var diffBuilder = new SemanticInlineDiffBuilder();
            var shiftedModel = diffBuilder.PerformSemanticShifts(originalModel);

            shiftedModel.Should().BeOfType<DiffPaneModel>();
            shiftedModel.Lines.Should().AllBeOfType<DiffPiece>();
            shiftedModel.Lines.Should().HaveCount(originalModel.Lines.Count);

            shiftedModel.Lines.Should().SatisfyRespectively(
                    line =>
                    {
                        line.Position.Should().Be(1);
                        line.Text.Should().Be("ip access-list extended acl_vlan1");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(2);
                        line.Text.Should().Be("remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("permit udp 172.20.1.0 / 24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(4);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(5);
                        line.Text.Should().Be("ip access-list extended acl_vlan2");
                        line.Type.Should().Be(ChangeType.Inserted);
                    },
                    line =>
                    {
                        line.Position.Should().Be(6);
                        line.Text.Should().Be("remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Inserted);
                    },
                    line =>
                    {
                        line.Position.Should().Be(7);
                        line.Text.Should().Be("permit udp 172.20.1.0 / 24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Inserted);
                    },
                    line =>
                    {
                        line.Position.Should().Be(8);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Inserted);
                    },
                    line =>
                    {
                        line.Position.Should().Be(9);
                        line.Text.Should().Be("ip access-list extended acl_vlan2");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(10);
                        line.Text.Should().Be("remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(11);
                        line.Text.Should().Be("permit udp 172.20.2.0 / 24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(12);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    });
        }

        [Fact]
        public void ShiftRightDeletion()
        {
            var originalModel = new DiffPaneModel();

            originalModel.Lines.Add(new DiffPiece("ip access-list extended acl_vlan1", ChangeType.Unchanged, 1));
            originalModel.Lines.Add(new DiffPiece("remark Allow DNS lookups", ChangeType.Unchanged, 2));
            originalModel.Lines.Add(new DiffPiece("permit udp 172.20.1.0 / 24 host 8.8.8.8 eq dns", ChangeType.Unchanged, 3));
            originalModel.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 4));
            originalModel.Lines.Add(new DiffPiece("ip access-list extended acl_vlan2", ChangeType.Unchanged, 5));
            originalModel.Lines.Add(new DiffPiece("remark Allow DNS lookups", ChangeType.Unchanged, 6));
            originalModel.Lines.Add(new DiffPiece("permit udp 172.20.2.0 / 24 host 8.8.8.8 eq dns", ChangeType.Deleted, null));
            originalModel.Lines.Add(new DiffPiece("!", ChangeType.Deleted, null));
            originalModel.Lines.Add(new DiffPiece("ip access-list extended acl_vlan3", ChangeType.Deleted, null));
            originalModel.Lines.Add(new DiffPiece("remark Allow DNS lookups", ChangeType.Deleted, null));
            originalModel.Lines.Add(new DiffPiece("permit udp 172.20.2.0 / 24 host 8.8.8.8 eq dns", ChangeType.Unchanged, 7));
            originalModel.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 8));

            var diffBuilder = new SemanticInlineDiffBuilder();
            var shiftedModel = diffBuilder.PerformSemanticShifts(originalModel);

            shiftedModel.Should().BeOfType<DiffPaneModel>();
            shiftedModel.Lines.Should().AllBeOfType<DiffPiece>();
            shiftedModel.Lines.Should().HaveCount(originalModel.Lines.Count);

            shiftedModel.Lines.Should().SatisfyRespectively(
                    line =>
                    {
                        line.Position.Should().Be(1);
                        line.Text.Should().Be("ip access-list extended acl_vlan1");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(2);
                        line.Text.Should().Be("remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("permit udp 172.20.1.0 / 24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(4);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(5);
                        line.Text.Should().Be("ip access-list extended acl_vlan2");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(6);
                        line.Text.Should().Be("remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(7);
                        line.Text.Should().Be("permit udp 172.20.2.0 / 24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(8);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("ip access-list extended acl_vlan3");
                        line.Type.Should().Be(ChangeType.Deleted);
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Deleted);
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("permit udp 172.20.2.0 / 24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Deleted);
                    },
                    line =>
                    {
                        line.Position.Should().BeNull();
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Deleted);
                    });
        }

        [Fact]
        public void ShiftRightInsertion()
        {
            var originalModel = new DiffPaneModel();

            originalModel.Lines.Add(new DiffPiece("ip access-list extended acl_vlan1", ChangeType.Unchanged, 1));
            originalModel.Lines.Add(new DiffPiece("remark Allow DNS lookups", ChangeType.Unchanged, 2));
            originalModel.Lines.Add(new DiffPiece("permit udp 172.20.1.0 / 24 host 8.8.8.8 eq dns", ChangeType.Unchanged, 3));
            originalModel.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 4));
            originalModel.Lines.Add(new DiffPiece("ip access-list extended acl_vlan2", ChangeType.Unchanged, 5));
            originalModel.Lines.Add(new DiffPiece("remark Allow DNS lookups", ChangeType.Unchanged, 6));
            originalModel.Lines.Add(new DiffPiece("permit udp 172.20.2.0 / 24 host 8.8.8.8 eq dns", ChangeType.Inserted, 7));
            originalModel.Lines.Add(new DiffPiece("!", ChangeType.Inserted, 8));
            originalModel.Lines.Add(new DiffPiece("ip access-list extended acl_vlan3", ChangeType.Inserted, 9));
            originalModel.Lines.Add(new DiffPiece("remark Allow DNS lookups", ChangeType.Inserted, 10));
            originalModel.Lines.Add(new DiffPiece("permit udp 172.20.2.0 / 24 host 8.8.8.8 eq dns", ChangeType.Unchanged, 11));
            originalModel.Lines.Add(new DiffPiece("!", ChangeType.Unchanged, 12));

            var diffBuilder = new SemanticInlineDiffBuilder();
            var shiftedModel = diffBuilder.PerformSemanticShifts(originalModel);

            shiftedModel.Should().BeOfType<DiffPaneModel>();
            shiftedModel.Lines.Should().AllBeOfType<DiffPiece>();
            shiftedModel.Lines.Should().HaveCount(originalModel.Lines.Count);

            shiftedModel.Lines.Should().SatisfyRespectively(
                    line =>
                    {
                        line.Position.Should().Be(1);
                        line.Text.Should().Be("ip access-list extended acl_vlan1");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(2);
                        line.Text.Should().Be("remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(3);
                        line.Text.Should().Be("permit udp 172.20.1.0 / 24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(4);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(5);
                        line.Text.Should().Be("ip access-list extended acl_vlan2");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(6);
                        line.Text.Should().Be("remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(7);
                        line.Text.Should().Be("permit udp 172.20.2.0 / 24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(8);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Unchanged);
                    },
                    line =>
                    {
                        line.Position.Should().Be(9);
                        line.Text.Should().Be("ip access-list extended acl_vlan3");
                        line.Type.Should().Be(ChangeType.Inserted);
                    },
                    line =>
                    {
                        line.Position.Should().Be(10);
                        line.Text.Should().Be("remark Allow DNS lookups");
                        line.Type.Should().Be(ChangeType.Inserted);
                    },
                    line =>
                    {
                        line.Position.Should().Be(11);
                        line.Text.Should().Be("permit udp 172.20.2.0 / 24 host 8.8.8.8 eq dns");
                        line.Type.Should().Be(ChangeType.Inserted);
                    },
                    line =>
                    {
                        line.Position.Should().Be(12);
                        line.Text.Should().Be("!");
                        line.Type.Should().Be(ChangeType.Inserted);
                    });
        }
    }
}