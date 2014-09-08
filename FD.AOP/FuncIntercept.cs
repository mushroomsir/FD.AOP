using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FD.AOP
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class FuncIntercept : Attribute
    {
        public int Order
        {
            get;
            set;
        }
    }
}
