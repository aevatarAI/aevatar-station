import sys

print(f"Python version: {sys.version}")
print("Checking available modules...")

# 检查标准库
modules = ["json", "os", "datetime", "math", "random", "threading", "asyncio"]
for module in modules:
    try:
        __import__(module)
        print(f"✅ {module}: Available")
    except ImportError:
        print(f"❌ {module}: Not available")

# 检查第三方库
third_party = ["requests", "matplotlib", "numpy", "pandas"]
for module in third_party:
    try:
        __import__(module)
        print(f"✅ {module}: Available")
    except ImportError:
        print(f"❌ {module}: Not available")