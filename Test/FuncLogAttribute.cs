using FD.AOP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
    public class FuncLogAttribute : FuncIntercept
    {
        public static bool OnFuncBefore(FuncEventArgs args)
        {
            Console.WriteLine(":" + "Executeing");
            return true;
        }

        public static ExceptionCategory OnException(FuncEventArgs args)
        {
            Console.WriteLine(":" + "Exceptioned");
            return ExceptionCategory.Handle;
        }

        public static void OnFuncAfter(FuncEventArgs args)
        {
            Console.WriteLine( ":" + "ExecuteSuccess");
        }
    }
}
