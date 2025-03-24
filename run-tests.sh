#!/bin/bash

# === Configuration Parameters ===
SOLUTION_DIR="./"                                        # Solution directory
TEST_DIR="./test"                                       # Test root directory
TEST_RESULTS_DIR="./TestResults"                        # Test results directory
COVERAGE_REPORT_DIR="$TEST_RESULTS_DIR/CoverageReport"  # Coverage report directory
MERGED_COVERAGE_FILE="$TEST_RESULTS_DIR/coverage.cobertura.xml"  # Merged coverage file

# === Tool Check ===
if ! command -v dotnet &> /dev/null; then
    echo "dotnet CLI is not installed. Please install .NET SDK first."
    exit 1
fi

if ! command -v reportgenerator &> /dev/null; then
    echo "reportgenerator tool is not installed. Installing..."
    dotnet tool install -g dotnet-reportgenerator-globaltool
fi

# === Clean Up ===
echo "Cleaning the solution..."
dotnet clean "$SOLUTION_DIR"

# === Build Solution ===
echo "Building the solution..."
dotnet build "$SOLUTION_DIR" --configuration Release

# Check if build was successful
if [ $? -ne 0 ]; then
    echo "Build failed. Please fix the build errors above."
    exit 1
fi

# === Clean Up Old Test Results ===
echo "Cleaning up old test results..."
rm -rf "$TEST_RESULTS_DIR"
mkdir -p "$TEST_RESULTS_DIR"

# === Run Tests with Coverage ===
echo "Running tests with coverage..."

# Find all test projects
TEST_PROJECTS=$(find "$TEST_DIR" -name "*.Tests.csproj")
TEST_COUNT=$(echo "$TEST_PROJECTS" | wc -l)

echo "Found $TEST_COUNT test projects"

# Run tests for each project
for PROJECT in $TEST_PROJECTS; do
    PROJECT_NAME=$(basename "$PROJECT" .csproj)
    echo "\nRunning tests for $PROJECT_NAME..."
    
    # Create project-specific results directory
    PROJECT_RESULTS_DIR="$TEST_RESULTS_DIR/$PROJECT_NAME"
    mkdir -p "$PROJECT_RESULTS_DIR"
    
    dotnet test "$PROJECT" \
        --no-build \
        --configuration Release \
        --results-directory "$PROJECT_RESULTS_DIR" \
        --collect:"XPlat Code Coverage" \
        /p:ExcludeByFile="**/*.g.cs"
    
    if [ $? -ne 0 ]; then
        echo "Test execution failed for $PROJECT_NAME. Please check the output above."
        exit 1
    fi
done

# === Merge Coverage Reports ===
echo "\nMerging coverage reports..."
reportgenerator \
    -reports:"$TEST_RESULTS_DIR/*/*/coverage.cobertura.xml" \
    -targetdir:"$TEST_RESULTS_DIR/Merged" \
    -reporttypes:Cobertura \
    -filefilters:"-**/*.g.cs"

# === Generate Final HTML Report ===
echo "Generating final HTML coverage report..."
reportgenerator \
    -reports:"$TEST_RESULTS_DIR/Merged/Cobertura.xml" \
    -targetdir:"$COVERAGE_REPORT_DIR" \
    -reporttypes:Html \
    -filefilters:"-**/*.g.cs"

if [ -f "$COVERAGE_REPORT_DIR/index.html" ]; then
    echo "\nTest execution and coverage analysis completed successfully!"
    echo "View the coverage report at: $COVERAGE_REPORT_DIR/index.html"
    open "$COVERAGE_REPORT_DIR/index.html"
else
    echo "Failed to generate coverage report. Please check the output above."
    exit 1
fi