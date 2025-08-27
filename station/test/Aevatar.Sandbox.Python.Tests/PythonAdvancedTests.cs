using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Aevatar.Sandbox.Python.Tests
{
    public class PythonAdvancedTests
    {
        private readonly string _pythonPath;

        public PythonAdvancedTests()
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
        public async Task Should_Process_Json_Data()
        {
            // Arrange
            var code = @"
import json

# 创建示例JSON数据
data = {
    'name': 'John Doe',
    'age': 30,
    'skills': ['Python', 'C#', 'JavaScript'],
    'address': {
        'city': 'New York',
        'country': 'USA'
    }
}

# 将数据转换为JSON字符串
json_str = json.dumps(data, indent=2)
print('JSON字符串:')
print(json_str)

# 解析JSON字符串
parsed_data = json.loads(json_str)
print('\n解析后的数据:')
print('Name: ' + parsed_data['name'])
print('Age: ' + str(parsed_data['age']))
print('Skills: ' + ', '.join(parsed_data['skills']))
print('City: ' + parsed_data['address']['city'])

# 修改数据
parsed_data['age'] = 31
parsed_data['skills'].append('TypeScript')
parsed_data['address']['zipcode'] = '10001'

# 再次转换为JSON
updated_json = json.dumps(parsed_data, indent=2)
print('\n更新后的JSON:')
print(updated_json)
";

            // Act
            var result = await ExecutePythonCode(code);

            // Assert
            result.exitCode.ShouldBe(0);
            result.stdout.ShouldContain("JSON字符串:");
            result.stdout.ShouldContain("John Doe");
            result.stdout.ShouldContain("解析后的数据:");
            result.stdout.ShouldContain("Name: John Doe");
            result.stdout.ShouldContain("Age: 30");
            result.stdout.ShouldContain("Skills: Python, C#, JavaScript");
            result.stdout.ShouldContain("更新后的JSON:");
            result.stdout.ShouldContain("TypeScript");
            result.stdout.ShouldContain("zipcode");
            result.stderr.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Execute_Multithreaded_Code()
        {
            // Arrange
            var code = @"
import threading
import time
import random

# 共享数据和锁
counter = 0
counter_lock = threading.Lock()
results = []
results_lock = threading.Lock()

def worker(worker_id, iterations):
    global counter
    
    # 模拟工作
    for i in range(iterations):
        # 随机延迟
        time.sleep(random.uniform(0.001, 0.005))
        
        # 安全地更新计数器
        with counter_lock:
            counter += 1
        
        # 记录进度
        with results_lock:
            results.append(f'Worker {worker_id}: completed iteration {i+1}')
    
    # 工作完成
    with results_lock:
        results.append(f'Worker {worker_id}: finished all {iterations} iterations')

# 创建多个线程
threads = []
for i in range(3):
    iterations = random.randint(3, 5)
    thread = threading.Thread(target=worker, args=(i+1, iterations))
    threads.append(thread)
    print(f'Starting Worker {i+1} with {iterations} iterations')
    thread.start()

# 等待所有线程完成
for thread in threads:
    thread.join()

# 打印结果
print(f'\nFinal counter value: {counter}')
print(f'Total results: {len(results)}')
print('\nExecution log:')
for entry in results:
    print(f'- {entry}')
";

            // Act
            var result = await ExecutePythonCode(code);

            // Assert
            result.exitCode.ShouldBe(0);
            result.stdout.ShouldContain("Starting Worker");
            result.stdout.ShouldContain("Final counter value:");
            result.stdout.ShouldContain("Total results:");
            result.stdout.ShouldContain("Execution log:");
            result.stdout.ShouldContain("Worker 1: finished all");
            result.stdout.ShouldContain("Worker 2: finished all");
            result.stdout.ShouldContain("Worker 3: finished all");
            result.stderr.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Execute_Async_Code()
        {
            // Arrange
            var code = @"
import asyncio
import time

async def async_task(task_id, delay):
    print(f'Task {task_id} started')
    await asyncio.sleep(delay)  # 非阻塞延迟
    print(f'Task {task_id} completed after {delay:.2f} seconds')
    return f'Result from task {task_id}'

async def main():
    print('Starting async execution...')
    start_time = time.time()
    
    # 创建任务
    tasks = [
        async_task(1, 0.5),
        async_task(2, 0.3),
        async_task(3, 0.7),
    ]
    
    # 等待所有任务完成
    results = await asyncio.gather(*tasks)
    
    end_time = time.time()
    total_time = end_time - start_time
    
    print(f'\nAll tasks completed in {total_time:.2f} seconds')
    print('Results:')
    for i, result in enumerate(results):
        print(f'- {result}')
    
    # 验证异步执行比顺序执行快
    sequential_time = sum([0.5, 0.3, 0.7])
    print(f'\nSequential execution would take approximately {sequential_time:.2f} seconds')
    print(f'Async execution was {sequential_time/total_time:.2f}x faster')

# 运行异步主函数
asyncio.run(main())
";

            // Act
            var result = await ExecutePythonCode(code);

            // Assert
            result.exitCode.ShouldBe(0);
            result.stdout.ShouldContain("Starting async execution...");
            result.stdout.ShouldContain("Task 1 started");
            result.stdout.ShouldContain("Task 2 started");
            result.stdout.ShouldContain("Task 3 started");
            result.stdout.ShouldContain("All tasks completed in");
            result.stdout.ShouldContain("Result from task 1");
            result.stdout.ShouldContain("Result from task 2");
            result.stdout.ShouldContain("Result from task 3");
            result.stdout.ShouldContain("Async execution was");
            result.stderr.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Work_With_Files_And_Directories()
        {
            // Arrange
            var code = @"
import os
import json
import tempfile

# 创建临时目录
temp_dir = tempfile.mkdtemp()
print(f'Created temporary directory: {temp_dir}')

try:
    # 创建文件
    for i in range(3):
        file_path = os.path.join(temp_dir, f'test_file_{i}.txt')
        with open(file_path, 'w') as f:
            f.write(f'This is test file {i}\nCreated for testing file operations.')
        print(f'Created file: {file_path}')
    
    # 创建JSON文件
    data = {
        'files': [f'test_file_{i}.txt' for i in range(3)],
        'count': 3,
        'directory': temp_dir
    }
    
    json_path = os.path.join(temp_dir, 'metadata.json')
    with open(json_path, 'w') as f:
        json.dump(data, f, indent=2)
    print(f'Created JSON file: {json_path}')
    
    # 列出目录内容
    print('\nDirectory contents:')
    for item in os.listdir(temp_dir):
        item_path = os.path.join(temp_dir, item)
        size = os.path.getsize(item_path)
        print(f'- {item} ({size} bytes)')
    
    # 读取文件内容
    print('\nReading file contents:')
    sample_path = os.path.join(temp_dir, 'test_file_1.txt')
    with open(sample_path, 'r') as f:
        content = f.read()
        print(f'Content of test_file_1.txt:\n{content}')
    
    # 读取JSON文件
    print('\nReading JSON file:')
    with open(json_path, 'r') as f:
        loaded_data = json.load(f)
        print(f'JSON data: {loaded_data}')

finally:
    # 清理：删除临时文件和目录
    print('\nCleaning up...')
    for item in os.listdir(temp_dir):
        os.remove(os.path.join(temp_dir, item))
    os.rmdir(temp_dir)
    print(f'Removed temporary directory: {temp_dir}')
";

            // Act
            var result = await ExecutePythonCode(code);

            // Assert
            result.exitCode.ShouldBe(0);
            result.stdout.ShouldContain("Created temporary directory:");
            result.stdout.ShouldContain("Created file:");
            result.stdout.ShouldContain("Directory contents:");
            result.stdout.ShouldContain("Content of test_file_1.txt:");
            result.stdout.ShouldContain("This is test file 1");
            result.stdout.ShouldContain("JSON data:");
            result.stdout.ShouldContain("Removed temporary directory:");
            result.stderr.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Generate_Dynamic_Code()
        {
            // Arrange
            var code = @"
# 动态生成Python代码并执行
function_name = 'calculate_factorial'
parameter_name = 'n'
code_template = '''
def calculate_factorial(n):
    if n <= 1:
        return 1
    else:
        return n * calculate_factorial(n - 1)

# 测试函数
for i in range(6):
    print(str(i) + '! = ' + str(calculate_factorial(i)))
'''

print('Generated code:')
print('---')
print(code_template)
print('---\n')

print('Executing generated code:')
exec(code_template)
";

            // Act
            var result = await ExecutePythonCode(code);

            // Assert
            result.exitCode.ShouldBe(0);
            result.stdout.ShouldContain("Generated code:");
            result.stdout.ShouldContain("def calculate_factorial(n):");
            result.stdout.ShouldContain("Executing generated code:");
            result.stdout.ShouldContain("0! = 1");
            result.stdout.ShouldContain("5! = 120");
            result.stderr.ShouldBeEmpty();
        }
    }
}