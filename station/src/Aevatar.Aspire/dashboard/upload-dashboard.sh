#!/bin/bash

# Grafana Dashboard Upload Script
# This script uploads the Aevatar Stream Latency Percentiles dashboard to Grafana

set -e

# Configuration
GRAFANA_URL="http://localhost:3000"
GRAFANA_USER="admin"
GRAFANA_PASSWORD="admin"
DASHBOARD_FILE="orleans-latency-dashboard.json"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}🚀 Uploading Aevatar Stream Latency Dashboard to Grafana...${NC}"

# Check if dashboard file exists
if [ ! -f "$DASHBOARD_FILE" ]; then
    echo -e "${RED}❌ Dashboard file '$DASHBOARD_FILE' not found!${NC}"
    exit 1
fi

# Check if Grafana is running
echo -e "${YELLOW}🔍 Checking Grafana connectivity...${NC}"
if ! curl -s -f "${GRAFANA_URL}/api/health" > /dev/null; then
    echo -e "${RED}❌ Cannot connect to Grafana at ${GRAFANA_URL}${NC}"
    echo -e "${YELLOW}💡 Make sure Grafana is running: docker compose up -d${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Grafana is running${NC}"

# Upload dashboard
echo -e "${YELLOW}📤 Uploading dashboard...${NC}"

# Use curl to upload the dashboard
response=$(curl -s -X POST \
  -H "Content-Type: application/json" \
  -u "${GRAFANA_USER}:${GRAFANA_PASSWORD}" \
  -d @"$DASHBOARD_FILE" \
  "${GRAFANA_URL}/api/dashboards/db" \
  -w "%{http_code}")

# Extract HTTP status code (last 3 characters)
http_code="${response: -3}"
response_body="${response%???}"

echo "HTTP Status Code: $http_code"
echo "Response: $response_body"

# Check response
if [ "$http_code" -eq 200 ]; then
    echo -e "${GREEN}✅ Dashboard uploaded successfully!${NC}"
    
    # Extract dashboard URL from response
    dashboard_url=$(echo "$response_body" | grep -o '"url":"[^"]*' | cut -d'"' -f4)
    if [ -n "$dashboard_url" ]; then
        echo -e "${GREEN}🌐 Dashboard URL: ${GRAFANA_URL}${dashboard_url}${NC}"
    fi
    
    echo -e "${GREEN}📊 You can now view your Aevatar Stream Latency Percentiles dashboard in Grafana${NC}"
    echo -e "${YELLOW}🔗 Grafana: ${GRAFANA_URL}${NC}"
    echo -e "${YELLOW}🔑 Credentials: ${GRAFANA_USER}/${GRAFANA_PASSWORD}${NC}"
    
elif [ "$http_code" -eq 412 ]; then
    echo -e "${YELLOW}⚠️  Dashboard already exists, updating...${NC}"
    
    # For updates, we need to set overwrite to true
    temp_file=$(mktemp)
    jq '.overwrite = true' "$DASHBOARD_FILE" > "$temp_file"
    
    response=$(curl -s -X POST \
      -H "Content-Type: application/json" \
      -u "${GRAFANA_USER}:${GRAFANA_PASSWORD}" \
      -d @"$temp_file" \
      "${GRAFANA_URL}/api/dashboards/db" \
      -w "%{http_code}")
    
    rm "$temp_file"
    
    http_code="${response: -3}"
    response_body="${response%???}"
    
    if [ "$http_code" -eq 200 ]; then
        echo -e "${GREEN}✅ Dashboard updated successfully!${NC}"
        dashboard_url=$(echo "$response_body" | grep -o '"url":"[^"]*' | cut -d'"' -f4)
        if [ -n "$dashboard_url" ]; then
            echo -e "${GREEN}🌐 Dashboard URL: ${GRAFANA_URL}${dashboard_url}${NC}"
        fi
    else
        echo -e "${RED}❌ Failed to update dashboard. HTTP Status: $http_code${NC}"
        echo -e "${RED}Response: $response_body${NC}"
        exit 1
    fi
    
else
    echo -e "${RED}❌ Failed to upload dashboard. HTTP Status: $http_code${NC}"
    echo -e "${RED}Response: $response_body${NC}"
    
    # Common error troubleshooting
    if [ "$http_code" -eq 401 ]; then
        echo -e "${YELLOW}💡 Authentication failed. Check username/password.${NC}"
    elif [ "$http_code" -eq 403 ]; then
        echo -e "${YELLOW}💡 Permission denied. Check user permissions.${NC}"
    elif [ "$http_code" -eq 422 ]; then
        echo -e "${YELLOW}💡 Invalid dashboard format. Check JSON syntax.${NC}"
    fi
    
    exit 1
fi

echo -e "${GREEN}🎉 Dashboard upload completed successfully!${NC}"

# Optional: Open dashboard in browser (macOS)
if command -v open &> /dev/null && [ -n "$dashboard_url" ]; then
    echo -e "${YELLOW}🚀 Opening dashboard in browser...${NC}"
    open "${GRAFANA_URL}${dashboard_url}"
fi 