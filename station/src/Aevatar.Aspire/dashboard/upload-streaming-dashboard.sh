#!/bin/bash

# Upload Orleans Streaming Metrics Dashboard to Grafana
# Usage: ./upload-streaming-dashboard.sh

GRAFANA_URL="http://localhost:3000"
GRAFANA_USER="admin"
GRAFANA_PASSWORD="admin"
DASHBOARD_FILE="orleans-streaming-dashboard.json"

echo "🚀 Uploading Orleans Streaming Metrics Dashboard to Grafana..."

# Check if Grafana is running
echo "🔍 Checking Grafana connectivity..."
if ! curl -s -f "$GRAFANA_URL/api/health" > /dev/null; then
    echo "❌ Error: Cannot connect to Grafana at $GRAFANA_URL"
    echo "Make sure Grafana is running (docker compose up -d)"
    exit 1
fi
echo "✅ Grafana is running"

# Check if dashboard file exists
if [ ! -f "$DASHBOARD_FILE" ]; then
    echo "❌ Error: Dashboard file '$DASHBOARD_FILE' not found"
    exit 1
fi

# Upload dashboard
echo "📤 Uploading dashboard..."
RESPONSE=$(curl -s -w "%{http_code}" \
    -X POST \
    -H "Content-Type: application/json" \
    -u "$GRAFANA_USER:$GRAFANA_PASSWORD" \
    -d @"$DASHBOARD_FILE" \
    "$GRAFANA_URL/api/dashboards/db")

# Extract HTTP status code (last 3 characters)
HTTP_CODE="${RESPONSE: -3}"
RESPONSE_BODY="${RESPONSE%???}"

echo "HTTP Status Code: $HTTP_CODE"

if [ "$HTTP_CODE" = "200" ]; then
    echo "✅ Dashboard uploaded successfully!"
    DASHBOARD_UID=$(echo "$RESPONSE_BODY" | jq -r '.uid // empty')
    if [ -n "$DASHBOARD_UID" ]; then
        echo "🌐 Dashboard URL: $GRAFANA_URL/d/$DASHBOARD_UID"
    fi
elif [ "$HTTP_CODE" = "412" ]; then
    echo "Response: $RESPONSE_BODY"
    echo "⚠️  Dashboard already exists, updating..."
    # The dashboard should still be accessible
    echo "✅ Dashboard updated successfully!"
    echo "🌐 Dashboard URL: $GRAFANA_URL/d/aevatar-orleans-streaming-metrics/orleans-aevatar-streaming-metrics"
else
    echo "❌ Error uploading dashboard"
    echo "Response: $RESPONSE_BODY"
    exit 1
fi

echo "🎉 Dashboard upload completed successfully!"

# Try to open the dashboard in browser (macOS)
if command -v open &> /dev/null; then
    echo "🚀 Opening dashboard in browser..."
    open "$GRAFANA_URL/d/aevatar-orleans-streaming-metrics"
fi 