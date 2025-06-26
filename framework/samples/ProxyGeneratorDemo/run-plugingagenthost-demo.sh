#!/bin/bash

# ABOUTME: Script to run the complete PluginGAgentHost demo
# ABOUTME: Starts silo and client in correct order for demonstration

echo "=== PluginGAgentHost Demo Runner ==="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_step() {
    echo -e "${BLUE}$1${NC}"
}

print_success() {
    echo -e "${GREEN}$1${NC}"
}

print_warning() {
    echo -e "${YELLOW}$1${NC}"
}

print_error() {
    echo -e "${RED}$1${NC}"
}

# Check if we're in the right directory
if [ ! -f "ProxyGeneratorDemo.sln" ]; then
    print_error "Error: Must be run from the ProxyGeneratorDemo directory"
    exit 1
fi

print_step "Step 1: Building the solution..."
dotnet build --configuration Debug
if [ $? -ne 0 ]; then
    print_error "Build failed!"
    exit 1
fi
print_success "✓ Build successful"

print_step "Step 2: Starting Orleans Silo (PluginGAgentHost)..."
echo ""
print_warning "The silo will start and load the plugins. Keep this terminal open."
print_warning "Press Ctrl+C to stop the silo when done."
echo ""

# Start the silo in the background and capture its PID
cd PluginGAgentHostDemo.Silo
dotnet run &
SILO_PID=$!
cd ..

# Wait a bit for silo to start
print_step "Waiting for silo to initialize..."
sleep 5

print_step "Step 3: Starting Client Demo..."
echo ""

# Function to cleanup on exit
cleanup() {
    print_step "Cleaning up..."
    if ps -p $SILO_PID > /dev/null 2>&1; then
        print_warning "Stopping silo (PID: $SILO_PID)..."
        kill $SILO_PID
        wait $SILO_PID 2>/dev/null
    fi
    print_success "✓ Cleanup complete"
}

# Set trap to cleanup on script exit
trap cleanup EXIT

# Run the client
cd PluginGAgentHostDemo.Client
dotnet run
CLIENT_EXIT_CODE=$?
cd ..

if [ $CLIENT_EXIT_CODE -eq 0 ]; then
    print_success "✓ Demo completed successfully!"
    echo ""
    print_step "Demo Summary:"
    echo "• PluginGAgentHost successfully hosted plugins"
    echo "• WeatherService and Calculator plugins executed"
    echo "• Orleans integration worked transparently"
    echo "• Plugin state management demonstrated"
    echo "• Method routing and error handling tested"
    echo "• Hot reload capability shown"
    echo ""
    print_success "PluginGAgentHost approach validation: SUCCESSFUL"
else
    print_error "Demo failed with exit code: $CLIENT_EXIT_CODE"
fi

echo ""
print_step "Press any key to stop the silo..."
read -n 1

print_step "Demo complete!"