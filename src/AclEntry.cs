using System;
using System.Collections.Generic;
using System.Text;

namespace SwitchConfigHelper
{
    internal class AclEntry : IEquatable<AclEntry>
    {
        public SemanticDiffPiece Acl { get; set; }
        public SemanticDiffPiece Remark { get; set; }

        public AclEntry(SemanticDiffPiece acl)
        {
            Acl = acl;
            Remark = null;
        }

        public AclEntry(SemanticDiffPiece acl, SemanticDiffPiece remark)
        {
            Acl = acl;
            Remark = remark;
        }

        public bool Equals(AclEntry other)
        {
            return Acl.Text.Equals(other.Acl.Text) && Remark.Text.Equals(other.Remark.Text);
        }
    }
}
