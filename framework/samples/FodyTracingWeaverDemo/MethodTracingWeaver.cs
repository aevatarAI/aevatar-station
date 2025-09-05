using System;
using System.Collections.Generic;
using System.Linq;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Aevatar.Core.Interception.Context;
using Aevatar.Core.Interception.Attributes;

namespace FodyTracingWeaverDemo;

/// <summary>
/// Fody weaver that injects method-level tracing code at compile time.
/// Automatically adds tracing to methods decorated with FodyTraceAttribute.
/// </summary>
public class MethodTracingWeaver : BaseModuleWeaver
{
    private TypeReference? _activitySourceType;
    private TypeReference? _activityType;
    private TypeReference? _activityStatusCodeType;
    private TypeReference? _traceContextType;
    private TypeReference? _fodyTraceAttributeType;
    private TypeReference? _stringType;
    private TypeReference? _objectType;
    private TypeReference? _boolType;
    private TypeReference? _intType;
    private TypeReference? _voidType;
    private TypeReference? _taskType;
    private TypeReference? _taskGenericType;
    private TypeReference? _exceptionType;

    public override void Execute()
    {
        // Find and cache type references
        FindTypeReferences();

        // Process all types in the module
        foreach (var type in ModuleDefinition.Types)
        {
            ProcessType(type);
        }
    }

    private void FindTypeReferences()
    {
        _activitySourceType = FindType("System.Diagnostics.ActivitySource");
        _activityType = FindType("System.Diagnostics.Activity");
        _activityStatusCodeType = FindType("System.Diagnostics.ActivityStatusCode");
        _traceContextType = FindType("Aevatar.Core.Interception.Context.TraceContext");
        _fodyTraceAttributeType = FindType("Aevatar.Core.Interception.Attributes.FodyTraceAttribute");
        _stringType = FindType("System.String");
        _objectType = FindType("System.Object");
        _boolType = FindType("System.Boolean");
        _intType = FindType("System.Int32");
        _voidType = FindType("System.Void");
        _taskType = FindType("System.Threading.Tasks.Task");
        _taskGenericType = FindType("System.Threading.Tasks.Task`1");
        _exceptionType = FindType("System.Exception");
    }

    private void ProcessType(TypeDefinition type)
    {
        // Skip compiler-generated types (check for special names)
        if (type.Name.Contains("<") || type.Name.Contains(">") || type.Name.StartsWith("<>"))
            return;

        // Check if this type has any methods with FodyTrace attributes
        bool hasTracingMethods = type.Methods.Any(m => GetFodyTraceAttribute(m) != null) || 
                                GetFodyTraceAttribute(type) != null;

        // Process methods in the type
        foreach (var method in type.Methods)
        {
            ProcessMethod(method);
        }

        // Process nested types
        foreach (var nestedType in type.NestedTypes)
        {
            ProcessType(nestedType);
        }
    }
    
    private void ProcessMethod(MethodDefinition method)
    {
        // Skip if method has no body (abstract, extern, etc.)
        if (!method.HasBody)
            return;

        // Check if method has FodyTrace attribute
        // Method-level attributes take precedence over class-level attributes
        var traceAttribute = GetFodyTraceAttribute(method);

        // If no method-level attribute, check class-level attributes
        if (traceAttribute == null)
        {
            traceAttribute = GetFodyTraceAttribute(method.DeclaringType);
        }

        if (traceAttribute == null)
            return;

        // Inject tracing code
        InjectTracingCode(method, traceAttribute);
    }

    private CustomAttribute? GetFodyTraceAttribute(MethodDefinition method)
    {
        if (!method.HasCustomAttributes)
            return null;

        return method.CustomAttributes.FirstOrDefault(attr => 
            attr.AttributeType.FullName == "Aevatar.Core.Interception.Attributes.FodyTraceAttribute");
    }

    private CustomAttribute? GetFodyTraceAttribute(TypeDefinition type)
    {
        if (!type.HasCustomAttributes)
            return null;

        return type.CustomAttributes.FirstOrDefault(attr => 
            attr.AttributeType.FullName == "Aevatar.Core.Interception.Attributes.FodyTraceAttribute");
    }

    private void InjectTracingCode(MethodDefinition method, CustomAttribute traceAttribute)
    {
        var processor = method.Body.GetILProcessor();
        var firstInstruction = method.Body.Instructions[0];

        // Get trace attribute properties
        var operationName = GetAttributePropertyValue<string>(traceAttribute, "OperationName", method.Name);
        var captureParameters = GetAttributePropertyValue<bool>(traceAttribute, "CaptureParameters", true);
        var captureReturnValue = GetAttributePropertyValue<bool>(traceAttribute, "CaptureReturnValue", true);
        var maxCaptureSize = GetAttributePropertyValue<int>(traceAttribute, "MaxCaptureSize", 1000);

        // Insert simple console logging for now - just get basic tracing working
        processor.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Ldstr, $"TRACE: Entering {operationName}"));
        processor.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Call, GetMethod(FindType("System.Console"), "WriteLine", _stringType)));

        // Simple logging at method end
        var lastInstruction = method.Body.Instructions[method.Body.Instructions.Count - 1];
        
        // Only log exit for synchronous methods - async methods are too complex to handle properly
        if (method.ReturnType.FullName != "System.Threading.Tasks.Task" && 
            !method.ReturnType.FullName.StartsWith("System.Threading.Tasks.Task`"))
        {
            processor.InsertBefore(lastInstruction, Instruction.Create(OpCodes.Ldstr, $"TRACE: Exiting {operationName}"));
            processor.InsertBefore(lastInstruction, Instruction.Create(OpCodes.Call, GetMethod(FindType("System.Console"), "WriteLine", _stringType)));
        }
    }

    private void InjectSetTag(ILProcessor processor, Instruction insertBefore, VariableDefinition activityLocal, string tagName, string tagValue)
    {
        processor.InsertBefore(insertBefore, Instruction.Create(OpCodes.Ldloc, activityLocal));
        processor.InsertBefore(insertBefore, Instruction.Create(OpCodes.Ldstr, tagName));
        processor.InsertBefore(insertBefore, Instruction.Create(OpCodes.Ldstr, tagValue));
        processor.InsertBefore(insertBefore, Instruction.Create(OpCodes.Callvirt, GetMethod(_activityType, "SetTag", _stringType, _objectType)));
    }

    private void InjectParameterCapture(ILProcessor processor, Instruction insertBefore, MethodDefinition method, VariableDefinition activityLocal, int maxCaptureSize)
    {
        if (!method.HasParameters)
            return;

        for (int i = 0; i < method.Parameters.Count; i++)
        {
            var parameter = method.Parameters[i];
            var parameterName = parameter.Name;

            // Check if parameter is not null
            var parameterNullCheck = Instruction.Create(OpCodes.Ldarg, parameter);
            processor.InsertBefore(insertBefore, parameterNullCheck);
            processor.InsertBefore(insertBefore, Instruction.Create(OpCodes.Brfalse, insertBefore));

            // Set parameter tag on activity
            processor.InsertBefore(insertBefore, Instruction.Create(OpCodes.Ldloc, activityLocal));
            processor.InsertBefore(insertBefore, Instruction.Create(OpCodes.Ldstr, $"parameter.{parameterName}"));
            processor.InsertBefore(insertBefore, Instruction.Create(OpCodes.Ldarg, parameter));
            processor.InsertBefore(insertBefore, Instruction.Create(OpCodes.Call, GetMethod(_objectType, "ToString")));
            processor.InsertBefore(insertBefore, Instruction.Create(OpCodes.Callvirt, GetMethod(_activityType, "SetTag", _stringType, _objectType)));
        }
    }

    private FieldReference GetStaticField(TypeReference? type, string fieldName)
    {
        if (type == null)
            throw new InvalidOperationException($"Type not found for field {fieldName}");

        var typeDef = type.Resolve();
        var field = typeDef.Fields.FirstOrDefault(f => f.Name == fieldName && f.IsStatic);
        
        if (field == null)
            throw new InvalidOperationException($"Static field {fieldName} not found on type {type.FullName}");

        return ModuleDefinition.ImportReference(field);
    }

    private void WrapMethodBodyInTryCatch(ILProcessor processor, MethodDefinition method, VariableDefinition activityLocal, VariableDefinition exceptionLocal, bool captureReturnValue, int maxCaptureSize)
    {
        var originalInstructions = method.Body.Instructions.ToList();
        var tryStart = originalInstructions[0];
        var tryEnd = originalInstructions[originalInstructions.Count - 1];

        // Create try block
        var tryBlock = Instruction.Create(OpCodes.Nop);
        processor.InsertBefore(tryStart, tryBlock);

        // Create catch block
        var catchBlock = Instruction.Create(OpCodes.Nop);
        processor.InsertAfter(tryEnd, catchBlock);

        // Create finally block
        var finallyBlock = Instruction.Create(OpCodes.Nop);
        processor.InsertAfter(catchBlock, finallyBlock);

        // Set exception status in catch block
        processor.InsertBefore(catchBlock, Instruction.Create(OpCodes.Stloc, exceptionLocal));
        processor.InsertBefore(catchBlock, Instruction.Create(OpCodes.Ldloc, activityLocal));
        processor.InsertBefore(catchBlock, Instruction.Create(OpCodes.Ldloc, exceptionLocal));
        processor.InsertBefore(catchBlock, Instruction.Create(OpCodes.Callvirt, GetMethod(_exceptionType, "get_Message")));
        processor.InsertBefore(catchBlock, Instruction.Create(OpCodes.Callvirt, GetMethod(_activityType, "SetStatus", _activityStatusCodeType, _stringType)));

        // Re-throw exception
        processor.InsertBefore(catchBlock, Instruction.Create(OpCodes.Ldloc, exceptionLocal));
        processor.InsertBefore(catchBlock, Instruction.Create(OpCodes.Throw));

        // End activity in finally block
        processor.InsertBefore(finallyBlock, Instruction.Create(OpCodes.Ldloc, activityLocal));
        processor.InsertBefore(finallyBlock, Instruction.Create(OpCodes.Callvirt, GetMethod(_activityType, "Dispose")));

        // Handle return value capture if needed
        if (captureReturnValue && method.ReturnType != _voidType)
        {
            InjectReturnValueCapture(processor, tryEnd, method, activityLocal, maxCaptureSize);
        }
    }

    private void InjectReturnValueCapture(ILProcessor processor, Instruction insertBefore, MethodDefinition method, VariableDefinition activityLocal, int maxCaptureSize)
    {
        // This is a simplified implementation - in practice, you'd need more sophisticated logic
        // to handle different return types and async methods
        processor.InsertBefore(insertBefore, Instruction.Create(OpCodes.Ldloc, activityLocal));
        processor.InsertBefore(insertBefore, Instruction.Create(OpCodes.Ldstr, "return.value"));
        processor.InsertBefore(insertBefore, Instruction.Create(OpCodes.Ldstr, "[captured]"));
        processor.InsertBefore(insertBefore, Instruction.Create(OpCodes.Callvirt, GetMethod(_activityType, "SetTag", _stringType, _objectType)));
    }

    private T GetAttributePropertyValue<T>(CustomAttribute attribute, string propertyName, T defaultValue)
    {
        if (!attribute.HasProperties)
            return defaultValue;

        var property = attribute.Properties.FirstOrDefault(p => p.Name == propertyName);
        if (property.Argument.Value is CustomAttributeArgument arg)
        {
            if (arg.Value is T typedValue)
            {
                return typedValue;
            }
        }
        return defaultValue;
    }

    private MethodReference GetActivitySourceMethod(TypeReference? type, string methodName, params TypeReference[] parameters)
    {
        if (type == null)
            throw new InvalidOperationException($"Type not found for method {methodName}");

        // Log available methods for debugging
        var availableMethods = type.Resolve().Methods.Where(m => m.Name == methodName).ToList();
        WriteInfo($"Available {methodName} methods on {type.FullName}: {string.Join(", ", availableMethods.Select(m => $"{m.Name}({string.Join(", ", m.Parameters.Select(p => p.ParameterType.Name))})"))}");

        // Try to find exact parameter match first
        var method = availableMethods.FirstOrDefault(m => 
            m.Parameters.Count == parameters.Length &&
            m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters.Select(p => p.FullName)));

        if (method != null)
        {
            WriteInfo($"Found exact match for {methodName} with {parameters.Length} parameters");
            return ModuleDefinition.ImportReference(method);
        }

        // Try to find method with compatible parameters (handling default values)
        if (methodName == "StartActivity" && parameters.Length == 2 && 
            parameters[0].FullName == "System.String" && parameters[1].FullName == "System.Object")
        {
            // For StartActivity, try to find the 2-parameter overload
            var startActivityMethod = availableMethods.FirstOrDefault(m => 
                m.Name == "StartActivity" && 
                m.Parameters.Count == 2 &&
                m.Parameters[0].ParameterType.FullName == "System.String" &&
                m.Parameters[1].ParameterType.FullName == "System.Object");

            if (startActivityMethod != null)
            {
                WriteInfo($"Found StartActivity method with compatible parameters");
                return ModuleDefinition.ImportReference(startActivityMethod);
            }
        }

        // If still not found, log detailed information and throw
        var methodDetails = string.Join("\n", availableMethods.Select(m => 
            $"  {m.Name}({string.Join(", ", m.Parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))})"));
        
        WriteError($"Method {methodName} not found on type {type.FullName}. Available methods:\n{methodDetails}");
        WriteError($"Requested parameters: {string.Join(", ", parameters.Select(p => p.FullName))}");
        
        throw new InvalidOperationException($"Method {methodName} not found on type {type.FullName}. Available methods:\n{methodDetails}");
    }

    private MethodReference GetMethod(TypeReference? type, string methodName, params TypeReference[] parameters)
    {
        if (type == null)
            throw new InvalidOperationException($"Type not found for method {methodName}");

        try
        {
            // Log available methods for debugging
            var availableMethods = type.Resolve().Methods.Where(m => m.Name == methodName).ToList();
            WriteInfo($"Available {methodName} methods on {type.FullName}: {string.Join(", ", availableMethods.Select(m => $"{m.Name}({string.Join(", ", m.Parameters.Select(p => p.ParameterType.Name))})"))}");
            WriteInfo($"Requested parameters: {string.Join(", ", parameters.Select(p => p.FullName))}");

            // Try to find exact parameter match first
            var method = availableMethods.FirstOrDefault(m => 
                m.Name == methodName && 
                m.Parameters.Count == parameters.Length &&
                m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters.Select(p => p.FullName)));

            if (method != null)
            {
                WriteInfo($"Found exact match for {methodName} with {parameters.Length} parameters");
                return ModuleDefinition.ImportReference(method);
            }

                         // Try to find method with compatible parameters (handling default values)
             if (methodName == "SetTag" && parameters.Length == 2 && 
                 parameters[0].FullName == "System.String" && parameters[1].FullName == "System.Object")
             {
                 // For SetTag, try to find the 2-parameter overload
                 var setTagMethod = availableMethods.FirstOrDefault(m => 
                     m.Name == "SetTag" && 
                     m.Parameters.Count == 2 &&
                     m.Parameters[0].ParameterType.FullName == "System.String" &&
                     m.Parameters[1].ParameterType.FullName == "System.Object");

                 if (setTagMethod != null)
                 {
                     WriteInfo($"Found SetTag method with compatible parameters");
                     return ModuleDefinition.ImportReference(setTagMethod);
                 }
             }

            // If still not found, log detailed information and throw
            var methodDetails = string.Join("\n", availableMethods.Select(m => 
                $"  {m.Name}({string.Join(", ", m.Parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))})"));
            
            WriteError($"Method {methodName} not found on type {type.FullName}. Available methods:\n{methodDetails}");
            WriteError($"Requested parameters: {string.Join(", ", parameters.Select(p => p.FullName))}");
            
            throw new InvalidOperationException($"Method {methodName} not found on type {type.FullName}. Available methods:\n{methodDetails}");
        }
        catch (Exception ex)
        {
            WriteError($"Error resolving method {methodName} on type {type.FullName}: {ex.Message}");
            throw;
        }
    }

    private MethodReference GetConstructor(TypeReference? type, params TypeReference[] parameters)
    {
        if (type == null)
            throw new InvalidOperationException("Type not found for constructor");

        // Log available constructors for debugging
        var availableConstructors = type.Resolve().Methods.Where(m => m.IsConstructor).ToList();
        WriteInfo($"Available constructors on {type.FullName}: {string.Join(", ", availableConstructors.Select(m => $"({string.Join(", ", m.Parameters.Select(p => p.ParameterType.Name))})"))}");
        WriteInfo($"Requested parameters: {string.Join(", ", parameters.Select(p => p.FullName))}");

        // Try to find exact parameter match first
        var constructor = availableConstructors.FirstOrDefault(m => 
            m.IsConstructor && 
            m.Parameters.Count == parameters.Length &&
            m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters.Select(p => p.FullName)));

        if (constructor != null)
        {
            WriteInfo($"Found exact constructor match with {parameters.Length} parameters");
            return ModuleDefinition.ImportReference(constructor);
        }

        // Try to find constructor with compatible parameters
        if (parameters.Length == 1 && parameters[0].FullName == "System.String")
        {
            // For single string parameter constructor, try to find any constructor that takes a string
            var stringConstructor = availableConstructors.FirstOrDefault(m => 
                m.IsConstructor && 
                m.Parameters.Count == 1 &&
                m.Parameters[0].ParameterType.FullName == "System.String");

            if (stringConstructor != null)
            {
                WriteInfo($"Found constructor with compatible string parameter");
                return ModuleDefinition.ImportReference(stringConstructor);
            }
        }

        // If still not found, log detailed information and throw
        var constructorDetails = string.Join("\n", availableConstructors.Select(m => 
            $"  ({string.Join(", ", m.Parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))})"));
        
        WriteError($"Constructor not found on type {type.FullName}. Available constructors:\n{constructorDetails}");
        WriteError($"Requested parameters: {string.Join(", ", parameters.Select(p => p.FullName))}");
        
        throw new InvalidOperationException($"Constructor not found on type {type.FullName}. Available constructors:\n{constructorDetails}");
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "System.Diagnostics";
        yield return "System.Threading.Tasks";
        yield return "System.Threading";
        yield return "System.Runtime";
        yield return "mscorlib";
        yield return "System.Private.CoreLib";
        yield return "System.Core";
        yield return "Aevatar.Core.Interception";
        yield return "Aevatar.Core.Abstractions";
        yield return "Aevatar.Core";
        yield return "System.Diagnostics.DiagnosticSource";
        yield return "System.Diagnostics.Activity";
    }
}
