#!/bin/bash
# set-env.template.sh - Template for setting environment variables
# 
# Instructions:
# 1. Copy this file to set-env.sh: cp set-env.template.sh set-env.sh
# 2. Edit set-env.sh and fill in your actual credentials
# 3. Run: source ./set-env.sh
# 4. NEVER commit set-env.sh to version control!

echo "Setting Aevatar authentication environment variables..."

# Authentication credentials (replace with your actual values)
export AEVATAR_USERNAME="your-username"
export AEVATAR_PASSWORD="your-password"
export AEVATAR_CLIENT_ID="your-client-id"
export AEVATAR_SCOPE="your-scope"

# Optional: Additional environment variables
# export AEVATAR_AUTH_URL="https://your-auth-url/connect/token"
# export AEVATAR_BASE_URL="https://your-base-url"

echo "Environment variables set successfully!"
echo ""
echo "Available variables:"
echo "  - AEVATAR_USERNAME"
echo "  - AEVATAR_PASSWORD (hidden)"
echo "  - AEVATAR_CLIENT_ID"
echo "  - AEVATAR_SCOPE"
echo ""
echo "Now you can run: ./test-tool-calling.sh" 