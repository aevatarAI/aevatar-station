import requests
import json

# ==================== Step 1: Get Station Token ====================
AUTH_BASE_URL = "https://auth-station-staging.aevatar.ai"
STATION_BASE_URL = "https://station-developer-staging.aevatar.ai/pressuretest-client"

# ==================== Step 1: Get Station Token ====================
auth_url = f"{AUTH_BASE_URL}/connect/token"
headers = {
    'accept': 'application/json, text/plain, */*',
    'content-type': 'application/x-www-form-urlencoded'
}
data = {
    'grant_type': 'client_credentials',
    'scope': 'Aevatar',
    'client_id': 'AIMining',
    'client_secret': 'AIMining123'
}

# Send authentication request[1,3](@ref)
auth_response = requests.post(auth_url, headers=headers, data=data)
access_token = auth_response.json()['access_token']  # Extract access token

# ==================== Step 2: Create AIMining Main Agent ====================
agent_headers = {
    'accept': 'application/json',
    'Content-Type': 'application/json',
    'Authorization': f'Bearer {access_token}'
}

main_agent_data = {
    "agentType": "MineAiFun.GAgents.GAgents.Common.AIMiningGAgent",
    "name": "AIMining",
    "properties": {
        "desc": "First AIMining top gagent"
    }
}

# Create main Agent[2,5](@ref)
main_agent_response = requests.post(
    f'{STATION_BASE_URL}/api/agent/',
    headers=agent_headers,
    json=main_agent_data
)
aimining_agent_id = main_agent_response.json()['data']['id']  # Record main Agent ID

# ==================== Step 3: Create AIMiningSub Sub-Agent ====================
sub_agent_data = {
    "agentType": "MineAiFun.GAgents.GAgents.Common.AIMiningSubGAgent",
    "name": "AIMiningSubGAgent",
    "properties": {
        "desc": "First AIMiningSubGAgent top gagent"
    }
}

# Create sub-Agent[2,5](@ref)
sub_agent_response = requests.post(
    f'{STATION_BASE_URL}/api/agent/',
    headers=agent_headers,
    json=sub_agent_data
)
sub_agent_id = sub_agent_response.json()['data']['id']  # Record sub-Agent ID

# ==================== Step 4: Bind Sub-Agent to Main Agent ====================
bind_url = f'{STATION_BASE_URL}/api/agent/{aimining_agent_id}/add-subagent'
bind_payload = {
    "subAgents": [sub_agent_id]
}

# Binding operation[2,5](@ref)
bind_response = requests.post(
    bind_url,
    headers=agent_headers,
    json=bind_payload
)


# ==================== Step 6: Initialize AIMining Agent ====================
init_url = f'{STATION_BASE_URL}/api/agent/publishEvent'
init_payload = {
    "agentId": aimining_agent_id,
    "eventType": "MineAiFun.GAgents.GAgents.Common.GEvents.InitAiMingGEvent",
    "eventProperties": {
        "WarRewardsPool": 1000000000000
    }
}

# Execute initialization[5,11](@ref)
init_response = requests.post(
    init_url,
    headers=agent_headers,
    json=init_payload
)

# Process initialization response
if init_response.status_code == 200:
    print(f"🎉 Agent initialization completed: {aimining_agent_id}")
else:
    print(f"Initialization failed: {init_response.text}")