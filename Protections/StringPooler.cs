﻿using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;
using System;

public class StringPooler
{
    private static ProtoRandom.ProtoRandom _random = new ProtoRandom.ProtoRandom(5);

    public static void Process(ModuleDefMD module)
    {
        List<MethodDef> toObfuscate = new List<MethodDef>();

        foreach (TypeDef type in module.Types)
        {
            if (type.IsGlobalModuleType)
            {
                continue;
            }

            foreach (MethodDef method in type.Methods)
            {
                if (method.FullName.Contains("InitializeComponent") || method.IsConstructor || method.IsFamily || method.IsRuntimeSpecialName || method.DeclaringType.IsForwarder || method.HasOverrides || method.IsVirtual)
                {
                    continue;
                }

                if (!method.HasBody)
                {
                    continue;
                }

                for (int i = 0; i < method.Body.Instructions.Count; i++)
                {
                    if (method.Body.Instructions[i].OpCode == OpCodes.Ldstr)
                    {
                        for (int j = 1; j < _random.GetRandomInt32(10, 20); j++)
                        {
                            if (j != 1)
                            {
                                j += 1;
                            }

                            Local newLocal1 = new Local(module.CorLibTypes.String);
                            Local newLocal2 = new Local(module.CorLibTypes.String);

                            method.Body.Variables.Add(newLocal1);
                            method.Body.Variables.Add(newLocal2);

                            method.Body.Instructions.Insert(i + j, Instruction.Create(OpCodes.Stloc_S, newLocal1));
                            method.Body.Instructions.Insert(i + (j + 1), Instruction.Create(OpCodes.Ldloc_S, newLocal1));
                        }
                    }
                }

                toObfuscate.Add(method);
            }
        }

        int x = 1;

        foreach (MethodDef method in toObfuscate)
        {
            foreach (Instruction instruction in method.Body.Instructions)
            {
                if (instruction.OpCode == OpCodes.Ldstr)
                {
                    x++;
                    MethodDef newMethod = new MethodDefUser("StringPoolingObfuscation_" + x.ToString(), MethodSig.CreateStatic(method.DeclaringType.Module.CorLibTypes.String), MethodImplAttributes.IL | MethodImplAttributes.Managed, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig) { Body = new CilBody() };

                    newMethod.Body.Instructions.Add(new Instruction(OpCodes.Ldstr, instruction.Operand.ToString()));
                    newMethod.Body.Instructions.Add(new Instruction(OpCodes.Ret));

                    Instruction jumpTo = newMethod.Body.Instructions[0];

                    newMethod.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, module.Import(typeof(System.Reflection.Assembly).GetMethod("GetExecutingAssembly", new Type[] { }))));
                    newMethod.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Call, module.Import(typeof(System.Reflection.Assembly).GetMethod("GetCallingAssembly", new Type[] { }))));
                    newMethod.Body.Instructions.Insert(2, Instruction.Create(OpCodes.Call, module.Import(typeof(System.Reflection.Assembly).GetMethod("op_Inequality", new Type[] { typeof(System.Reflection.Assembly), typeof(System.Reflection.Assembly) }))));
                    newMethod.Body.Instructions.Insert(3, new Instruction(OpCodes.Brfalse_S, jumpTo));
                    newMethod.Body.Instructions.Insert(4, Instruction.Create(OpCodes.Call, module.Import(typeof(System.Diagnostics.Process).GetMethod("GetCurrentProcess", new Type[] { }))));
                    newMethod.Body.Instructions.Insert(5, Instruction.Create(OpCodes.Callvirt, module.Import(typeof(System.Diagnostics.Process).GetMethod("Kill", new Type[] { }))));
                    newMethod.Body.Instructions.Insert(6, Instruction.Create(OpCodes.Ldstr, ""));
                    newMethod.Body.Instructions.Insert(7, Instruction.Create(OpCodes.Ret));

                    method.DeclaringType.Methods.Add(newMethod);
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = newMethod;
                }
            }
        }

        toObfuscate.Clear();
        GC.Collect();
    }
}