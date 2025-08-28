using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Aevatar.Sandbox.Python.Tests
{
    public class PythonExecutionTests
    {
        private readonly string _pythonPath;

        public PythonExecutionTests()
        {
            // 使用环境中的Python解释器
            _pythonPath = "python3";
        }

        /// <summary>
        /// 执行Python代码并返回结果
        /// </summary>
        /// <param name="code">要执行的Python代码</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>包含标准输出、标准错误和退出码的执行结果</returns>
        private async Task<(string stdout, string stderr, int exitCode)> ExecutePythonCode(string code, int timeoutMs = 5000)
        {
            // 创建临时Python文件
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, code);

            try
            {
                // 创建进程启动信息
                var startInfo = new ProcessStartInfo
                {
                    FileName = _pythonPath,
                    Arguments = tempFile,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // 启动进程
                using var process = new Process { StartInfo = startInfo };
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        outputBuilder.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        errorBuilder.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // 等待进程完成或超时
                var completed = await Task.Run(() => process.WaitForExit(timeoutMs));
                if (!completed)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // 忽略终止进程时的错误
                    }
                    return (outputBuilder.ToString(), "执行超时", -1);
                }

                return (outputBuilder.ToString(), errorBuilder.ToString(), process.ExitCode);
            }
            finally
            {
                // 删除临时文件
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // 忽略删除临时文件时的错误
                }
            }
        }

        [Fact]
        public async Task Should_Execute_Simple_Print_Statement()
        {
            // Arrange
            var code = @"
print('Hello, World!')
print('This is a test')
";

            // Act
            var result = await ExecutePythonCode(code);

            // Assert
            result.exitCode.ShouldBe(0);
            result.stdout.ShouldContain("Hello, World!");
            result.stdout.ShouldContain("This is a test");
            result.stderr.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Execute_Math_Calculations()
        {
            // Arrange
            var code = @"
# 简单的数学计算
a = 10
b = 5
print(f'a + b = {a + b}')
print(f'a - b = {a - b}')
print(f'a * b = {a * b}')
print(f'a / b = {a / b}')
print(f'a ** b = {a ** b}')  # 指数运算

# 复杂一点的计算
import math
print(f'Square root of 16 is {math.sqrt(16)}')
print(f'Sin(30°) = {math.sin(math.radians(30))}')
print(f'Log(100) = {math.log10(100)}')
";

            // Act
            var result = await ExecutePythonCode(code);

            // Assert
            result.exitCode.ShouldBe(0);
            result.stdout.ShouldContain("a + b = 15");
            result.stdout.ShouldContain("a - b = 5");
            result.stdout.ShouldContain("a * b = 50");
            result.stdout.ShouldContain("a / b = 2.0");
            result.stdout.ShouldContain("a ** b = 100000");
            result.stdout.ShouldContain("Square root of 16 is 4.0");
            result.stdout.ShouldContain("Sin(30°) = 0.49");
            result.stdout.ShouldContain("Log(100) = 2.0");
            result.stderr.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Handle_Errors()
        {
            // Arrange
            var code = @"
# 这段代码会产生一个除零错误
try:
    result = 10 / 0
    print('This will not be printed')
except ZeroDivisionError:
    print('Caught a division by zero error')

# 这段代码会产生一个未捕获的错误
print('Before error')
result = 10 / 0
print('After error - this will not be printed')
";

            // Act
            var result = await ExecutePythonCode(code);

            // Assert
            result.exitCode.ShouldNotBe(0);  // 应该有非零的退出码表示错误
            result.stdout.ShouldContain("Caught a division by zero error");
            result.stdout.ShouldContain("Before error");
            result.stdout.ShouldNotContain("After error");
            result.stderr.ShouldContain("ZeroDivisionError");
        }

        [Fact]
        public async Task Should_Import_Standard_Libraries()
        {
            // Arrange
            var code = @"
# 导入标准库
import os
import sys
import datetime
import json
import random

# 使用os库
print(f'Current working directory: {os.getcwd()}')
print(f'OS name: {os.name}')

# 使用sys库
print(f'Python version: {sys.version}')
print(f'Platform: {sys.platform}')

# 使用datetime库
now = datetime.datetime.now()
print(f'Current date: {now.strftime(""Y-%m-%d"")}')
print(f'Current time: {now.strftime(""H:%M:%S"")}')

# 使用json库
data = {
    'name': 'Test User',
    'age': 30,
    'is_active': True,
    'skills': ['Python', 'C#', 'JavaScript']
}
json_str = json.dumps(data)
print(f'JSON string: {json_str}')
parsed_data = json.loads(json_str)
print(f'Parsed name: {parsed_data[""name""]}')

# 使用random库
print(f'Random number between 1 and 10: {random.randint(1, 10)}')
print(f'Random choice from list: {random.choice([""apple"", ""banana"", ""cherry""])}')
";

            // Act
            var result = await ExecutePythonCode(code);

            // Assert
            result.exitCode.ShouldBe(0);
            result.stdout.ShouldContain("Current working directory:");
            result.stdout.ShouldContain("OS name:");
            result.stdout.ShouldContain("Python version:");
            result.stdout.ShouldContain("Platform:");
            result.stdout.ShouldContain("Current date:");
            result.stdout.ShouldContain("Current time:");
            result.stdout.ShouldContain("JSON string:");
            result.stdout.ShouldContain("Parsed name: Test User");
            result.stdout.ShouldContain("Random number between 1 and 10:");
            result.stdout.ShouldContain("Random choice from list:");
            result.stderr.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Execute_Complex_Code()
        {
            // Arrange
            var code = @"
# 定义一个类
class Person:
    def __init__(self, name, age):
        self.name = name
        self.age = age
    
    def greet(self):
        return f'Hello, my name is {self.name} and I am {self.age} years old.'
    
    def celebrate_birthday(self):
        self.age += 1
        return f'Happy birthday! Now I am {self.age} years old.'

# 创建对象并使用
person = Person('Alice', 30)
print(person.greet())
print(person.celebrate_birthday())
print(person.greet())

# 使用异常处理和自定义异常
class CustomError(Exception):
    def __init__(self, message):
        self.message = message
        super().__init__(self.message)

def validate_age(age):
    if age < 0:
        raise CustomError('Age cannot be negative')
    elif age > 120:
        raise CustomError('Age is too high')
    return f'Age {age} is valid'

# 测试异常处理
try:
    print(validate_age(25))
    print(validate_age(-5))
except CustomError as e:
    print(f'Caught custom error: {e.message}')

try:
    print(validate_age(150))
except CustomError as e:
    print(f'Caught another error: {e.message}')
";

            // Act
            var result = await ExecutePythonCode(code);

            // Assert
            result.exitCode.ShouldBe(0);
            result.stdout.ShouldContain("Hello, my name is Alice and I am 30 years old.");
            result.stdout.ShouldContain("Happy birthday! Now I am 31 years old.");
            result.stdout.ShouldContain("Hello, my name is Alice and I am 31 years old.");
            result.stdout.ShouldContain("Age 25 is valid");
            result.stdout.ShouldContain("Caught custom error: Age cannot be negative");
            result.stdout.ShouldContain("Caught another error: Age is too high");
            result.stderr.ShouldBeEmpty();
        }
    }
}