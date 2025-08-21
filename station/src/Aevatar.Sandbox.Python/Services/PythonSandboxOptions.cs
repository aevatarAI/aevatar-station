namespace Aevatar.Sandbox.Python.Services;

public class PythonSandboxOptions
{
    public string PythonImage { get; set; } = "python:3.9-slim";
    public string Namespace { get; set; } = "sandbox-python";
}