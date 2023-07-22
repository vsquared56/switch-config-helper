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

                Assert.Equal(8, diff.Lines.Count);
                Assert.Equal(ChangeType.Unchanged, diff.Lines[3].Type);
                Assert.Equal(ChangeType.Inserted, diff.Lines[4].Type);
                Assert.Equal(ChangeType.Inserted, diff.Lines[5].Type);
                Assert.Equal(ChangeType.Inserted, diff.Lines[6].Type);
                Assert.Equal(ChangeType.Inserted, diff.Lines[7].Type);
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

            Assert.Equal(9, diff.Lines.Count);
            Assert.Equal(ChangeType.Unchanged, diff.Lines[4].Type); //First ! unchanged
            Assert.Equal(ChangeType.Inserted, diff.Lines[5].Type);
            Assert.Equal(ChangeType.Inserted, diff.Lines[6].Type);
            Assert.Equal(ChangeType.Inserted, diff.Lines[7].Type);
            Assert.Equal(ChangeType.Inserted, diff.Lines[8].Type); //Final ! inserted
        }
    }
}