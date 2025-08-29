using Aevatar.GAgents.PsiOmni.Interfaces;
using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.PsiOmni.Plugins;

public static class MathFunctionRegistration
{
    public static void RegisterAllMathFunctions(IKernelFunctionRegistry registry)
    {
        registry.RegisterFunction("Math.Add", 
            KernelFunctionFactory.CreateFromMethod(
                (double a, double b) => a + b,
                "Add",
                "Add two numbers together"));

        registry.RegisterFunction("Math.Subtract", 
            KernelFunctionFactory.CreateFromMethod(
                (double a, double b) => a - b,
                "Subtract",
                "Subtract two numbers"));

        registry.RegisterFunction("Math.Multiply", 
            KernelFunctionFactory.CreateFromMethod(
                (double a, double b) => a * b,
                "Multiply", 
                "Multiply two numbers"));

        registry.RegisterFunction("Math.Divide", 
            KernelFunctionFactory.CreateFromMethod(
                (double a, double b) => b != 0 ? a / b : throw new DivideByZeroException("Cannot divide by zero"),
                "Divide",
                "Divide two numbers"));

        registry.RegisterFunction("Math.Average", 
            KernelFunctionFactory.CreateFromMethod(
                (double[] numbers) => numbers.Average(),
                "Average",
                "Calculate the average of a list of numbers"));

        registry.RegisterFunction("Math.Sum", 
            KernelFunctionFactory.CreateFromMethod(
                (double[] numbers) => numbers.Sum(),
                "Sum",
                "Calculate the sum of a list of numbers"));

        registry.RegisterFunction("Math.Factorial", 
            KernelFunctionFactory.CreateFromMethod(
                (int n) => n <= 1 ? 1 : Enumerable.Range(1, n).Aggregate(1, (acc, x) => acc * x),
                "Factorial",
                "Calculate factorial of a number"));
    }
} 