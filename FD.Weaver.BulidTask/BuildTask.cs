using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using FD.Weaver;

namespace Weaver.BulidTask
{
    public class FdBuildTask : Microsoft.Build.Utilities.Task
    {
        [Microsoft.Build.Framework.Required]
        public string OutputFile
        {
            get;
            set;
        }

        [Microsoft.Build.Framework.Required]
        public string TaskFile
        {
            get;
            set;
        }

        public override bool Execute()
        {
            FuncILInject task = new FuncILInject(@"D:\104 Git\fd.aop\Test\bin\Debug\test.exe");
            task.Run();

            return true;
        }
    }

}
