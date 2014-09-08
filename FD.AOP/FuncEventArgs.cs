using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace FD.AOP
{
    public class FuncEventArgs
    {
        public object Instance { get; private set; }

        public Exception Exception { get; set; }

        public MethodBase Method { get; private set; }

        public object ReturnValue { get; set; }

        public object[] Arguments
        {
            get;
            private set;
        }
        public FuncEventArgs(MethodBase method, object instance, object[] arguments)
        {
            Method = method;
            Arguments = arguments;
            Instance = instance;
        }
        public FuncEventArgs(MethodBase method, object instance, object[] arguments, string returnType)
        {
            Method = method;
            Arguments = arguments;
            Instance = instance;
            if (returnType != null)
            {
                var type = Type.GetType(returnType);
                if (type != null)
                {
                    this.ReturnValue = DefaultForType(type);
                }
            }
        }
        protected static object DefaultForType(Type targetType)
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }
    }
}
