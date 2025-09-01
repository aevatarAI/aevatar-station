#!/bin/bash
# Configuration script for running regression tests against Aspire endpoints

# Set environment variables for Aspire endpoints
export AUTH_HOST="http://localhost:7001"  # AuthServer
export API_HOST="http://localhost:7002"   # HttpApi.Host - Main API
export API_SERVER_HOST="http://localhost:7002"  # Developer.Host - Developer API

# Enable LOCAL_RUN to use admin credentials
export LOCAL_RUN=true

# Default test credentials
export CLIENT_ID="Aevatar001"
export CLIENT_SECRET="123456"

# Run the regression tests
echo "Running regression tests against Aspire endpoints..."
echo "AUTH_HOST: $AUTH_HOST"
echo "API_HOST: $API_HOST"
echo "API_SERVER_HOST: $API_SERVER_HOST"

# Execute pytest
python3 -m pytest regression_test.py -v
