#!/usr/bin/env python3
import os
import sys
import json
import time
import resource
import traceback
from typing import Optional

def get_memory_mb() -> float:
    """Get current memory usage in MB."""
    usage = resource.getrusage(resource.RUSAGE_SELF)
    return usage.ru_maxrss / 1024  # Convert KB to MB

def setup_sandbox():
    """Configure sandbox environment and restrictions."""
    # Disable dangerous builtins
    for name in ['open', 'eval', 'exec', 'compile', '__import__']:
        if name in __builtins__.__dict__:
            del __builtins__.__dict__[name]
    
    # Set resource limits
    timeout_seconds = int(os.environ.get('TIMEOUT_SECONDS', '30'))
    memory_mb = 512
    
    resource.setrlimit(resource.RLIMIT_CPU, (timeout_seconds, timeout_seconds))
    resource.setrlimit(resource.RLIMIT_AS, (memory_mb * 1024 * 1024, memory_mb * 1024 * 1024))

def run_code(code: str) -> tuple[bool, str, str, int]:
    """
    Run the provided code in a sandboxed environment.
    Returns: (success, stdout, stderr, exit_code)
    """
    import io
    import contextlib
    import sys
    
    stdout = io.StringIO()
    stderr = io.StringIO()
    exit_code = 0
    
    with contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
        try:
            exec(code, {'__builtins__': __builtins__}, {})
            success = True
        except Exception as e:
            traceback.print_exc(file=stderr)
            success = False
            exit_code = 1
    
    return success, stdout.getvalue(), stderr.getvalue(), exit_code

def main():
    """Main entry point for sandbox execution."""
    setup_sandbox()
    
    sandbox_execution_id = os.environ.get('SANDBOX_EXECUTION_ID')
    code = os.environ.get('CODE')
    
    if not sandbox_execution_id or not code:
        print("Missing required environment variables", file=sys.stderr)
        sys.exit(1)
    
    start_time = time.time()
    success, stdout, stderr, exit_code = run_code(code)
    exec_time = time.time() - start_time
    
    # Truncate output if too long
    max_size = 256 * 1024  # 256KB
    if len(stdout) > max_size:
        stdout = stdout[:max_size] + "\n... (truncated)"
    if len(stderr) > max_size:
        stderr = stderr[:max_size] + "\n... (truncated)"
    
    result = {
        "success": success,
        "stdout": stdout,
        "stderr": stderr,
        "exitCode": exit_code,
        "execTimeSec": exec_time,
        "memoryUsedMB": get_memory_mb()
    }
    
    print(json.dumps(result))
    sys.exit(exit_code)

if __name__ == '__main__':
    main()