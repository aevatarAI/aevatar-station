#!/bin/bash
# Configuration script for running regression tests against Aspire endpoints

# Show usage information
if [ "$1" == "-h" ] || [ "$1" == "--help" ]; then
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Automatically sets up a Python virtual environment and runs regression tests against Aspire endpoints."
    echo ""
    echo "Options:"
    echo "  -h, --help         Show this help message"
    echo "  --force-install    Force reinstall all Python dependencies"
    echo ""
    echo "Environment variables (optional):"
    echo "  AUTH_HOST          AuthServer URL (default: http://localhost:7001)"
    echo "  API_HOST           HttpApi.Host URL (default: http://localhost:7002)"
    echo "  API_SERVER_HOST    Developer.Host URL (default: http://localhost:7002)"
    echo ""
    exit 0
fi

# Navigate to the script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Setup Python virtual environment
VENV_DIR="venv"

# Check if virtual environment exists, create if not
if [ ! -d "$VENV_DIR" ]; then
    echo "Creating Python virtual environment..."
    python3 -m venv "$VENV_DIR"
fi

# Activate virtual environment
echo "Activating virtual environment..."
source "$VENV_DIR/bin/activate"

# Check if dependencies need to be installed/updated
REQUIREMENTS_FILE="requirements.txt"
REQUIREMENTS_STAMP="$VENV_DIR/.requirements_stamp"

# Install/upgrade dependencies if:
# 1. requirements.txt is newer than the stamp file
# 2. stamp file doesn't exist
# 3. --force-install flag is passed
if [ ! -f "$REQUIREMENTS_STAMP" ] || [ "$REQUIREMENTS_FILE" -nt "$REQUIREMENTS_STAMP" ] || [ "$1" == "--force-install" ]; then
    echo "Installing/upgrading dependencies..."
    pip install --upgrade pip
    pip install -r requirements.txt
    # Create/update stamp file
    touch "$REQUIREMENTS_STAMP"
else
    echo "Dependencies are up to date. Use --force-install to reinstall."
fi

# Set environment variables for Aspire endpoints
export AUTH_HOST="http://localhost:7001"  # AuthServer
export API_HOST="http://localhost:7002"   # HttpApi.Host - Main API
export API_SERVER_HOST="http://localhost:7002"  # Developer.Host - Developer API

# Enable LOCAL_RUN to use admin credentials
export LOCAL_RUN=true

# Default test credentials
export CLIENT_ID="Aevatar001"
export CLIENT_SECRET="123456"

echo "Runs regression tests against httpapi.host endpoints (not developer.host endpoints in ephemeral environment)."

# Run the regression tests
echo "Running regression tests against Aspire endpoints..."
echo "AUTH_HOST: $AUTH_HOST"
echo "API_HOST: $API_HOST"
echo "API_SERVER_HOST: $API_SERVER_HOST"

# Execute pytest
pytest regression_test.py -v
