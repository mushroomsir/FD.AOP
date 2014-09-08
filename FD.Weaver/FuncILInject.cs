using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Runtime.CompilerServices;
using FD.AOP;
using System.Reflection;
using Mono.Cecil.Rocks;

namespace FD.Weaver
{
    public class FuncILInject
    {
        private string binPath = string.Empty;
        private AssemblyDefinition assembly = null;

        public FuncILInject(string binpath)
        {
            if (string.IsNullOrEmpty(binpath))
            {
                throw new Exception("The Path is empty!");
            }
            binPath = binpath;
            assembly = AssemblyDefinition.ReadAssembly(binpath);
        }
        public void Run()
        {
            CheckModules(assembly);
            assembly.Write(binPath);
        }

        private void CheckModules(AssemblyDefinition assembly)
        {
            foreach (var modeul in assembly.Modules)
            {
                CheckTypes(modeul);
            }
        }
        private void CheckTypes(ModuleDefinition modeul)
        {
            var mtype = modeul.Types.Where(t => t.IsSpecialName == false).Where(t => t.CustomAttributes.Any(k => k.AttributeType.FullName == typeof(CompilerGeneratedAttribute).FullName) == false).ToList();
            mtype.ForEach(item =>
            {
                CheckMethods(item);
            });
        }
        private void CheckMethods(TypeDefinition mtype)
        {
            mtype.Methods.Where(t => !t.IsSpecialName && !t.IsSetter && !t.IsGetter).ToList().ForEach(method =>
             {
                 if (!method.HasCustomAttributes)
                     return;

                 method.CustomAttributes
                       .Where(t => IsSubclassOf(t.AttributeType.Resolve(), mtype.Module.Import(typeof(FuncIntercept)).Resolve()))
                       .Select(t =>
                           new
                           {
                               Attribute = t,
                               Order = t.Properties.Any(p => p.Name == "Order") ? (int)t.Properties.SingleOrDefault(p => p.Name == "Order").Argument.Value : int.MaxValue,
                           })
                       .OrderBy(t => t.Order)
                       .Select(t => t.Attribute)
                       .ToList()
                       .ForEach(
                            t => DealMethodInject(t, mtype, method, InjectTye.Method)
                        );
             });
        }

        public static bool IsSubclassOf(TypeDefinition type, TypeDefinition baseType)
        {
            if (type == null || baseType == null)
                return false;
            if (type.FullName == typeof(object).FullName)
                return false;
            else if (type.FullName == baseType.FullName)
                return true;
            return IsSubclassOf(type.BaseType.Resolve(), baseType);
        }

        internal virtual void DealMethodInject(CustomAttribute methodInject, TypeDefinition mtype, MethodDefinition method, InjectTye usage)
        {
            var il = method.Body.GetILProcessor();
            var module = method.Module;

            var newmethod = CompilerGeneratedNewMethod(method, module);

            if (!newmethod.CustomAttributes.Any(t => t.AttributeType.FullName == typeof(CompilerGeneratedAttribute).FullName))
            {
                newmethod.CustomAttributes.Add(new CustomAttribute(module.Import(typeof(CompilerGeneratedAttribute).GetConstructor(new Type[0]))));
            }

            mtype.Methods.Add(newmethod);

            method.Body.Instructions.Clear();
            method.Body.ExceptionHandlers.Clear();
            method.Body.Variables.Clear();
            method.Body.Instructions.Add(il.Create(OpCodes.Nop));


            var varMethodBase = new VariableDefinition(module.Import(typeof(System.Reflection.MethodBase)));
            method.Body.Variables.Add(varMethodBase);
            var varthis = new VariableDefinition(module.Import(typeof(System.Object)));
            method.Body.Variables.Add(varthis);
            var varparams = new VariableDefinition(module.Import(typeof(object[])));
            method.Body.Variables.Add(varparams);
            var varparams2 = new VariableDefinition(module.Import(typeof(object[])));
            method.Body.Variables.Add(varparams2);
            var varexception = new VariableDefinition(module.Import(typeof(System.Exception)));
            method.Body.Variables.Add(varexception);
            var varMethodExecutionEventArgs = new VariableDefinition(module.Import(typeof(FuncEventArgs)));
            method.Body.Variables.Add(varMethodExecutionEventArgs);
            var varflag = new VariableDefinition(module.Import(typeof(bool)));
            method.Body.Variables.Add(varflag);
            var varExceptionCategory = new VariableDefinition(module.Import(typeof(ExceptionCategory)));
            method.Body.Variables.Add(varExceptionCategory);
            var vartypeArray = new VariableDefinition(module.Import(typeof(Type[])));
            method.Body.Variables.Add(vartypeArray);

            method.Body.InitLocals = false;

            var lastNop = new[]{  il.Create(OpCodes.Nop)            ,
                il.Create(OpCodes.Nop)            ,
                il.Create(OpCodes.Nop)            ,
                (method.ReturnType.FullName != "System.Void")?il.Create(OpCodes.Ldloc_S,varMethodExecutionEventArgs):il.Create(OpCodes.Nop),
             };

            var lastLeaves = il.Create(OpCodes.Leave_S, lastNop[1]);

            var case1 = il.Create(OpCodes.Br_S, lastNop[0]);
            var case2 = il.Create(OpCodes.Rethrow);
            var case3 = il.Create(OpCodes.Ldloc_S, varMethodExecutionEventArgs);

            ILProcessorExsions.Append(il, new[] 
             {                
                 il.Create(OpCodes.Nop),                    
             });
            ILProcessorExsions.Append(il, new[] 
                 {  
                  il.Create(OpCodes.Call,module.Import(typeof(System.Reflection.MethodBase).GetMethod("GetCurrentMethod"))),
                 il.Create(OpCodes.Stloc_S,varMethodBase),
                 });
            var FuncAttr = methodInject.AttributeType.Resolve();

            if (!method.IsStatic)
            {
                ILProcessorExsions.Append(il, new[] {             
                 il.Create(OpCodes.Ldarg_S,method.Body.ThisParameter),                                 
              });
            }
            else
            {
                ILProcessorExsions.Append(il, new[] {                                                          
                  il.Create(OpCodes.Ldnull),                    
              });
            }

            ILProcessorExsions.Append(il, new[] { 
                il.Create(OpCodes.Stloc_S,varthis),
                il.Create(OpCodes.Ldc_I4,method.Parameters.Count), 
                il.Create(OpCodes.Newarr,module.Import(typeof(object))),
                il.Create(OpCodes.Stloc_S,varparams2),
                il.Create(OpCodes.Ldloc_S,varparams2),
            });

            var j = 0;
            method.Parameters.ToList().ForEach(t =>
            {
                ILProcessorExsions.Append(il, new[] { 
                    il.Create(OpCodes.Ldc_I4,j++),
                    il.Create(OpCodes.Ldarg_S, t),
                    il.Create(OpCodes.Box,t.ParameterType),
                    il.Create(OpCodes.Stelem_Ref),
                    il.Create(OpCodes.Ldloc_S,varparams2)
                });
            });


            ILProcessorExsions.Append(il, new[] { 
                
                 il.Create(OpCodes.Stloc_S,varparams),
                 il.Create(OpCodes.Ldloc_S,varMethodBase),
                 il.Create(OpCodes.Ldloc_S,varthis),
                 il.Create(OpCodes.Ldloc_S,varparams),
            });

            if (method.ReturnType.FullName != "System.Void")
            {
                if (method.ReturnType.IsValueType)
                {

                    ILProcessorExsions.Append(il, new[] {                     
                        il.Create(OpCodes.Ldstr, method.ReturnType.FullName),                           
                    });

                }
                else
                {
                    ILProcessorExsions.Append(il, new[] {                     
                        il.Create(OpCodes.Ldnull ),                            
                    });

                }
            }

            ILProcessorExsions.Append(il, new[] { 
                il.Create(OpCodes.Nop), 
                 il.Create(OpCodes.Newobj,module.Import(typeof(FuncEventArgs).GetConstructor(
                      new Type[] { typeof(MethodBase), typeof(object), typeof(object[]),typeof(string) }))),
                il.Create(OpCodes.Stloc_S,varMethodExecutionEventArgs),
                il.Create(OpCodes.Nop), 
            });
            var onFuncBefore = FuncAttr.GetMethods().Single(n => n.Name == "OnFuncBefore");
            var onFuncAfter = FuncAttr.GetMethods().Single(n => n.Name == "OnFuncAfter");
            var onException = FuncAttr.GetMethods().Single(n => n.Name == "OnException");

            ILProcessorExsions.Append(il, new[] { 
                 il.Create(OpCodes.Nop),
              
                il.Create(OpCodes.Ldloc_S,varMethodExecutionEventArgs),
                il.Create(OpCodes.Call, onFuncBefore),
                il.Create(OpCodes.Ldc_I4_0),
                il.Create(OpCodes.Ceq),
               il.Create(OpCodes.Stloc_S,varflag),
                il.Create(OpCodes.Ldloc_S,varflag),
                il.Create(OpCodes.Brtrue_S,lastNop[3]),
                il.Create(OpCodes.Nop),
              });


            var trySatrt = il.Create(OpCodes.Nop);
            ILProcessorExsions.Append(il, new[] { 
                 trySatrt,                                     
                 il.Create(OpCodes.Ldloc_S,varMethodExecutionEventArgs),
            });

            if (!method.IsStatic)
            {
                method.Body.Instructions.Add(il.Create(OpCodes.Ldarg_0));//Load this;
            }
            method.Parameters.ToList().ForEach(t =>
            {
                method.Body.Instructions.Add(il.Create(OpCodes.Ldarg_S, t));
            });

            method.Body.Instructions.Add(il.Create(OpCodes.Call, newmethod));
            if (method.ReturnType.FullName != "System.Void")
            {
                ILProcessorExsions.Append(il, new[] 
                  { 
                      il.Create(OpCodes.Box,method.ReturnType),
                      il.Create(OpCodes.Callvirt,module.Import(typeof(FuncEventArgs).GetMethod("set_ReturnValue",new Type[]{typeof(System.Object)}))),
                      il.Create(OpCodes.Nop),
                  });

            }
            else
            {
                method.Body.Instructions.Add(il.Create(OpCodes.Nop));
            }

            var tryEnd = il.Create(OpCodes.Stloc_S, varexception);
            ILProcessorExsions.Append(il, new[] 
              {  
                  il.Create(OpCodes.Ldloc_S,varMethodExecutionEventArgs),
                 il.Create(OpCodes.Call, onFuncAfter),
                  il.Create(OpCodes.Nop),
                  il.Create(OpCodes.Nop),
                  il.Create(OpCodes.Leave_S,lastNop[1]),
                  tryEnd,
                  il.Create(OpCodes.Nop),
                  il.Create(OpCodes.Ldloc_S,varMethodExecutionEventArgs),
                  il.Create(OpCodes.Ldloc_S,varexception),
                  il.Create(OpCodes.Callvirt,module.Import(typeof(FuncEventArgs).GetMethod("set_Exception",new Type[]{typeof(System.Exception)}))),
                  il.Create(OpCodes.Nop),
                  il.Create(OpCodes.Ldloc_S,varMethodExecutionEventArgs),
                   il.Create(OpCodes.Call, onException),
                  il.Create(OpCodes.Stloc_S,varExceptionCategory),
                  il.Create(OpCodes.Ldloc_S,varExceptionCategory),
                  il.Create(OpCodes.Switch,new []{case1,case2,case3}),
                  il.Create(OpCodes.Br_S,lastNop[0]),
                 case1,
                 case2,
                 case3,
                 il.Create(OpCodes.Callvirt,module.Import(typeof(FuncEventArgs).GetMethod("get_Exception",new Type[]{}))),
                 il.Create(OpCodes.Throw),                 
              });

            ILProcessorExsions.Append(il, new[] { 
                 lastNop[0],
                 lastLeaves,
                 lastNop[1],
                 lastNop[2],
                 lastNop[3],                 
             });
            if (method.ReturnType.FullName != "System.Void")
            {
                var varreturnValue = new VariableDefinition(method.ReturnType);
                method.Body.Variables.Add(varreturnValue);
                var lastreturn = il.Create(OpCodes.Ldloc_S, varreturnValue);
                ILProcessorExsions.Append(il, new[] { 
                     il.Create(OpCodes.Callvirt,module.Import(typeof(FuncEventArgs).GetMethod("get_ReturnValue",new Type[]{}))),
                     il.Create(OpCodes.Unbox_Any,method.ReturnType),
                     il.Create(OpCodes.Stloc_S,varreturnValue),
                     il.Create(OpCodes.Br_S,lastreturn),
                     lastreturn,
                });
            }

            method.Body.Instructions.Add(il.Create(OpCodes.Ret));
            method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                HandlerEnd = lastNop[1],
                HandlerStart = tryEnd,
                TryEnd = tryEnd,
                TryStart = trySatrt,
                CatchType = module.Import(typeof(System.Exception))
            });

        }

        private MethodDefinition CompilerGeneratedNewMethod(MethodDefinition method, ModuleDefinition module)
        {
            var newmethod = new MethodDefinition(method.Name + (Guid.NewGuid().ToString().Replace("-", "_")), method.Attributes, method.ReturnType)
            {
                IsPrivate = true,
                IsStatic = method.IsStatic,
            };
            method.CustomAttributes.ToList().ForEach(t => { newmethod.CustomAttributes.Add(t); });
            method.Body.Instructions.ToList().ForEach(t => { newmethod.Body.Instructions.Add(t); });
            method.Body.Variables.ToList().ForEach(t => { newmethod.Body.Variables.Add(t); });
            method.Body.ExceptionHandlers.ToList().ForEach(t => { newmethod.Body.ExceptionHandlers.Add(t); });
            method.Parameters.ToList().ForEach(t => { newmethod.Parameters.Add(t); });
            method.GenericParameters.ToList().ForEach(t => { newmethod.GenericParameters.Add(t); });

            newmethod.Body.LocalVarToken = method.Body.LocalVarToken;
            newmethod.Body.InitLocals = method.Body.InitLocals;
            return newmethod;
        }

    }
}
