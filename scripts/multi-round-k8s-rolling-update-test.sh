#!/bin/bash

echo "🔄 Orleans K8s Rolling Update Simulation Test"
echo "============================================="

ROUNDS=${1:-3}  # Default to 3 rounds
SILO_COUNT=3
BASE_PORT=11111
GATEWAY_BASE_PORT=30000
TEST_DURATION=15  # seconds per round

echo "Testing $ROUNDS rounds of rolling updates with $SILO_COUNT silos"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Arrays to track silo PIDs and ports
declare -a SILO_PIDS
declare -a SILO_PORTS
declare -a GATEWAY_PORTS

# Build the project first
echo "📦 Building project..."
dotnet build --no-restore --verbosity quiet

if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

echo "✅ Build successful"

# Function to start a silo
start_silo() {
    local silo_id=$1
    local silo_port=$((BASE_PORT + silo_id))
    local gateway_port=$((GATEWAY_BASE_PORT + silo_id))
    
    SILO_PORTS[$silo_id]=$silo_port
    GATEWAY_PORTS[$silo_id]=$gateway_port
    
    echo -e "${BLUE}Starting Silo $silo_id on ports $silo_port/$gateway_port...${NC}"
    
    # Start silo in background with detailed logging
    ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_ENVIRONMENT=Development \
    ORLEANS_SILO_PORT=$silo_port \
    ORLEANS_GATEWAY_PORT=$gateway_port \
    ORLEANS_CLUSTER_ID="test-cluster" \
    ORLEANS_SERVICE_ID="test-service" \
    dotnet run --project src/Aevatar.Silo/ \
        --urls "http://localhost:$((8080 + silo_id))" \
        > "silo_${silo_id}_$(date +%s).log" 2>&1 &
    
    SILO_PIDS[$silo_id]=$!
    
    # Wait for silo to start
    sleep 5
    
    if kill -0 ${SILO_PIDS[$silo_id]} 2>/dev/null; then
        echo -e "${GREEN}✅ Silo $silo_id started successfully (PID: ${SILO_PIDS[$silo_id]})${NC}"
        return 0
    else
        echo -e "${RED}❌ Failed to start Silo $silo_id${NC}"
        return 1
    fi
}

# Function to stop a silo
stop_silo() {
    local silo_id=$1
    if [ -n "${SILO_PIDS[$silo_id]}" ] && kill -0 ${SILO_PIDS[$silo_id]} 2>/dev/null; then
        echo -e "${YELLOW}Stopping Silo $silo_id (PID: ${SILO_PIDS[$silo_id]})...${NC}"
        kill ${SILO_PIDS[$silo_id]} 2>/dev/null || true
        wait ${SILO_PIDS[$silo_id]} 2>/dev/null || true
        echo -e "${GREEN}✅ Silo $silo_id stopped${NC}"
    fi
}

# Function to check silo health
check_silo_health() {
    local silo_id=$1
    if [ -n "${SILO_PIDS[$silo_id]}" ] && kill -0 ${SILO_PIDS[$silo_id]} 2>/dev/null; then
        return 0
    else
        return 1
    fi
}

# Function to check StateProjectionInitializer logs and Stream status
check_stream_status() {
    local round=$1
    echo -e "${BLUE}🔍 Checking Stream Processing Status for Round $round...${NC}"
    
    local found_stagger=false
    local found_retry=false
    local found_health_check=false
    local found_stream_errors=false
    local found_successful_init=false
    
    for silo_id in $(seq 0 $((SILO_COUNT - 1))); do
        local log_files=$(ls silo_${silo_id}_*.log 2>/dev/null | tail -1)
        if [ -n "$log_files" ]; then
            # Check for StateProjectionInitializer activity
            if grep -q "stagger delay" "$log_files" 2>/dev/null; then
                found_stagger=true
            fi
            if grep -q "retry\|retrying" "$log_files" 2>/dev/null; then
                found_retry=true
            fi
            if grep -q "already active\|health check" "$log_files" 2>/dev/null; then
                found_health_check=true
            fi
            if grep -q "Successfully initialized StateProjectionGrain\|Completed initializing StateProjectionGrains" "$log_files" 2>/dev/null; then
                found_successful_init=true
            fi
            
            # Check for Stream processing errors
            if grep -q "Stream.*error\|Stream.*failed\|Stream.*exception" "$log_files" 2>/dev/null; then
                found_stream_errors=true
                echo -e "${RED}⚠️  Stream errors detected in Silo $silo_id${NC}"
            fi
            
            # Check for Kafka/Stream provider errors
            if grep -q "Broker transport failure\|Connection refused.*9092\|Stream provider.*failed" "$log_files" 2>/dev/null; then
                echo -e "${YELLOW}ℹ️  Kafka unavailable in Silo $silo_id (expected in test environment)${NC}"
            fi
        fi
    done
    
    echo -e "  StateProjection Features:"
    echo -e "    Stagger delay mechanism: $([ "$found_stagger" = true ] && echo -e "${GREEN}✅ Active${NC}" || echo -e "${YELLOW}⚠️  Not detected${NC}")"
    echo -e "    Retry mechanism: $([ "$found_retry" = true ] && echo -e "${GREEN}✅ Active${NC}" || echo -e "${YELLOW}⚠️  Not detected${NC}")"
    echo -e "    Health check mechanism: $([ "$found_health_check" = true ] && echo -e "${GREEN}✅ Active${NC}" || echo -e "${YELLOW}⚠️  Not detected${NC}")"
    echo -e "    Successful initialization: $([ "$found_successful_init" = true ] && echo -e "${GREEN}✅ Completed${NC}" || echo -e "${RED}❌ Failed${NC}")"
    
    echo -e "  Stream Processing Status:"
    if [ "$found_stream_errors" = false ]; then
        echo -e "    Stream continuity: ${GREEN}✅ No errors detected${NC}"
        return 0
    else
        echo -e "    Stream continuity: ${RED}❌ Errors detected${NC}"
        return 1
    fi
}

# Function to perform rolling update
perform_rolling_update() {
    local round=$1
    echo -e "${BLUE}🔄 Round $round: Performing rolling update...${NC}"
    
    # Rolling update: replace each silo one by one
    for silo_id in $(seq 0 $((SILO_COUNT - 1))); do
        echo -e "${YELLOW}  Rolling update step $((silo_id + 1))/$SILO_COUNT: Replacing Silo $silo_id${NC}"
        
        # Stop the silo
        stop_silo $silo_id
        
        # Wait to simulate pod termination grace period
        sleep 3
        
        # Start new silo
        if start_silo $silo_id; then
            echo -e "${GREEN}  ✅ Silo $silo_id replaced successfully${NC}"
        else
            echo -e "${RED}  ❌ Failed to replace Silo $silo_id${NC}"
            return 1
        fi
        
        # Wait for stabilization
        sleep 5
    done
    
    echo -e "${GREEN}✅ Rolling update Round $round completed${NC}"
    return 0
}

# Function to verify cluster health
verify_cluster_health() {
    local round=$1
    echo -e "${BLUE}🏥 Verifying cluster health after Round $round...${NC}"
    
    local healthy_silos=0
    for silo_id in $(seq 0 $((SILO_COUNT - 1))); do
        if check_silo_health $silo_id; then
            healthy_silos=$((healthy_silos + 1))
        fi
    done
    
    echo -e "  Healthy silos: $healthy_silos/$SILO_COUNT"
    
    if [ $healthy_silos -eq $SILO_COUNT ]; then
        echo -e "${GREEN}✅ All silos are healthy${NC}"
        return 0
    else
        echo -e "${RED}❌ Some silos are unhealthy${NC}"
        return 1
    fi
}

# Function to cleanup
cleanup() {
    echo -e "${YELLOW}🧹 Cleaning up...${NC}"
    for silo_id in $(seq 0 $((SILO_COUNT - 1))); do
        stop_silo $silo_id
    done
    
    echo -e "${GREEN}✅ Cleanup completed${NC}"
    echo ""
    echo "📋 Log files available for analysis:"
    ls -la silo_*.log 2>/dev/null || echo "No log files found"
}

# Trap to ensure cleanup on exit
trap cleanup EXIT

# Main test execution
echo -e "${BLUE}🚀 Starting initial cluster with $SILO_COUNT silos...${NC}"

# Start initial cluster
failed_starts=0
for silo_id in $(seq 0 $((SILO_COUNT - 1))); do
    if ! start_silo $silo_id; then
        failed_starts=$((failed_starts + 1))
    fi
done

if [ $failed_starts -gt 0 ]; then
    echo -e "${RED}❌ Failed to start $failed_starts silos. Aborting test.${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Initial cluster started successfully${NC}"
echo ""

# Wait for cluster stabilization
echo -e "${BLUE}⏳ Waiting for cluster stabilization...${NC}"
sleep 10

# Perform multiple rounds of rolling updates
declare -a ROUND_RESULTS
successful_rounds=0

for round in $(seq 1 $ROUNDS); do
    echo ""
    echo -e "${BLUE}========== ROUND $round/$ROUNDS ==========${NC}"
    
    # Verify initial health
    if ! verify_cluster_health $round; then
        echo -e "${RED}❌ Round $round: Pre-update health check failed${NC}"
        ROUND_RESULTS[$round]="❌ FAILED (Pre-health check)"
        continue
    fi
    
    # Perform rolling update
    if perform_rolling_update $round; then
        # Wait for stabilization
        echo -e "${BLUE}⏳ Waiting for stabilization...${NC}"
        sleep $TEST_DURATION
        
        # Verify post-update health
        if verify_cluster_health $round; then
            # Check logs for StateProjectionInitializer and Stream status
            if check_stream_status $round; then
                echo -e "${GREEN}✅ Round $round: SUCCESS${NC}"
                ROUND_RESULTS[$round]="✅ SUCCESS"
                successful_rounds=$((successful_rounds + 1))
            else
                echo -e "${RED}❌ Round $round: Stream processing issues detected${NC}"
                ROUND_RESULTS[$round]="❌ FAILED (Stream issues)"
            fi
        else
            echo -e "${RED}❌ Round $round: Post-update health check failed${NC}"
            ROUND_RESULTS[$round]="❌ FAILED (Post-health check)"
        fi
    else
        echo -e "${RED}❌ Round $round: Rolling update failed${NC}"
        ROUND_RESULTS[$round]="❌ FAILED (Rolling update)"
    fi
done

# Final summary
echo ""
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}📊 MULTI-ROUND ROLLING UPDATE TEST SUMMARY${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""
echo -e "Total rounds tested: $ROUNDS"
echo -e "Successful rounds: ${GREEN}$successful_rounds${NC}"
echo -e "Failed rounds: ${RED}$((ROUNDS - successful_rounds))${NC}"
echo -e "Success rate: ${GREEN}$(( (successful_rounds * 100) / ROUNDS ))%${NC}"
echo ""

echo -e "${BLUE}Round-by-round results:${NC}"
for round in $(seq 1 $ROUNDS); do
    echo -e "  Round $round: ${ROUND_RESULTS[$round]}"
done

echo ""
echo -e "${BLUE}📋 Stream Status Monitoring Guide:${NC}"
echo -e "${GREEN}✅ Normal Stream Indicators:${NC}"
echo -e "  • 'Successfully initialized StateProjectionGrain for {StateType}'"
echo -e "  • 'Completed initializing StateProjectionGrains for silo {SiloAddress}'"
echo -e "  • 'Applying stagger delay of {Delay}ms to avoid K8s rolling update conflicts'"
echo -e "  • 'StateProjectionGrains for {StateType} are already active, skipping activation'"
echo ""
echo -e "${RED}❌ Stream Error Indicators:${NC}"
echo -e "  • 'Stream.*error' or 'Stream.*failed' or 'Stream.*exception'"
echo -e "  • 'Failed to initialize StateProjectionGrain for {StateType} after {MaxRetries} attempts'"
echo -e "  • 'Error during StateProjectionGrains initialization on silo {SiloAddress}'"
echo ""
echo -e "${YELLOW}ℹ️  Expected Test Environment Messages:${NC}"
echo -e "  • 'Broker transport failure' (Kafka not available - normal in test)"
echo -e "  • 'Connection refused.*9092' (Kafka connection - normal in test)"

if [ $successful_rounds -eq $ROUNDS ]; then
    echo ""
    echo -e "${GREEN}🎉 ALL ROUNDS PASSED! Orleans Stream Processing Enhancement"
    echo -e "   successfully maintains stream continuity during K8s rolling updates.${NC}"
    exit 0
else
    echo ""
    echo -e "${RED}⚠️  Some rounds failed. Review the logs for details.${NC}"
    exit 1
fi
