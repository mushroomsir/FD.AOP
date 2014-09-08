using FD.Weaver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Weaver.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            FuncILInject task = new FuncILInject(@"D:\104 Git\fd.aop\Test\bin\Debug\test.exe");
            task.Run();
        }
    }

}
