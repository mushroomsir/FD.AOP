using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FD.Weaver
{
    public class ILProcessorExsions
    {
        public static void InsertBefore(ILProcessor iLProcessor, Instruction target, Instruction[] ins)
        {
            if (ins != null && ins.Length > 0)
            {
                Array.ForEach(ins, t => iLProcessor.InsertBefore(target, t));
            }
        }

        public static void InsertAfter(ILProcessor iLProcessor, Instruction target, Instruction[] ins)
        {
            if (ins != null && ins.Length > 0)
            {
                Array.ForEach(ins, t => { iLProcessor.InsertAfter(target, t); target = t; });
            }
        }

        public static void Append(ILProcessor iLProcessor, Instruction[] ins)
        {
            if (ins != null && ins.Length > 0)
            {
                Array.ForEach(ins, t => { iLProcessor.Append(t); });
            }
        }
    }

}
