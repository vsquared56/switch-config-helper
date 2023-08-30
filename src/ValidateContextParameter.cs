using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace SwitchConfigHelper
{
    class ValidateContextParameter : ValidateArgumentsAttribute
    {
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            var context = (int)arguments;
            if (context < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
        }
    }
}
