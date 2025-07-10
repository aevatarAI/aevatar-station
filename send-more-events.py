import requests
import json
import time

# Configuration
BASE_URL = "http://localhost:8308"

# Agent IDs from Memory phase create-agents-with-subagents.py execution  
agent_ids = [
    # Main agents - Updated with correct IDs from latest Memory mode execution
    "4dd803cd-14f8-4108-9a88-ee1dd0857d26",  # TwitterAgent1
    "d74c9423-0ff5-4a7c-85f7-673efdb294e9",  # TwitterAgent2
    "3714d1d7-e3f9-4a16-995e-9a212c27804e",  # TwitterAgent3
]

def publish_event(agent_id, event_type, event_properties, event_description):
    """Publish an event to an agent without authentication"""
    headers = {
        'accept': 'application/json',
        'Content-Type': 'application/json'
    }
    
    event_url = f'{BASE_URL}/api/agent/publishEvent'
    event_payload = {
        "agentId": agent_id,
        "eventType": event_type,
        "eventProperties": event_properties
    }
    
    try:
        response = requests.post(event_url, headers=headers, json=event_payload)
        
        if response.status_code == 200:
            print(f"✅ {event_description}")
            print(f"   Agent: {agent_id}")
            print(f"   Response: {response.text}")
            return True
        else:
            print(f"❌ Failed to publish event: {event_description}")
            print(f"   Status: {response.status_code}")
            print(f"   Response: {response.text}")
            return False
            
    except Exception as e:
        print(f"❌ Error publishing event: {str(e)}")
        return False

def send_multiple_events():
    """Send multiple events to different agents"""
    print("🚀 Sending multiple events to agents...")
    print("=" * 80)
    
    events_sent = 0
    
    # Event 1: Set different numbers for all agents
    print("\n📤 Round 1: Setting different numbers for each agent")
    print("-" * 50)
    for i, agent_id in enumerate(agent_ids):
        success = publish_event(
            agent_id=agent_id,
            event_type="Aevatar.Application.Grains.Agents.TestAgent.SetNumberGEvent",
            event_properties={
                "Key": "Number",
                "Value": (i + 1) * 100
            },
            event_description=f"Set Number to {(i + 1) * 100} for agent {i + 1}"
        )
        if success:
            events_sent += 1
        time.sleep(0.5)
    
    # Event 2: Send different event types to selected agents
    print("\n📤 Round 2: Sending events with different values to selected agents")
    print("-" * 50)
    
    # Send to all 3 main agents with high values
    for i, agent_id in enumerate(agent_ids):
        success = publish_event(
            agent_id=agent_id,
            event_type="Aevatar.Application.Grains.Agents.TestAgent.SetNumberGEvent",
            event_properties={
                "Key": "Number",
                "Value": 5000 + (i * 1000)
            },
            event_description=f"Set high value {5000 + (i * 1000)} for main agent {i + 1}"
        )
        if success:
            events_sent += 1
        time.sleep(0.5)
    
    # Event 3: Send another round to main agents with special values
    print("\n📤 Round 3: Sending special events to main agents")
    print("-" * 50)
    
    for i, agent_id in enumerate(agent_ids):
        success = publish_event(
            agent_id=agent_id,
            event_type="Aevatar.Application.Grains.Agents.TestAgent.SetNumberGEvent",
            event_properties={
                "Key": "Number",
                "Value": 9000 + (i * 100)
            },
            event_description=f"Set special value {9000 + (i * 100)} for main agent {i + 1}"
        )
        if success:
            events_sent += 1
        time.sleep(0.5)
    
    # Event 4: Rapid fire events to one agent
    print("\n📤 Round 4: Rapid fire events to first agent")
    print("-" * 50)
    
    target_agent = agent_ids[0]
    for i in range(1):
        success = publish_event(
            agent_id=target_agent,
            event_type="Aevatar.Application.Grains.Agents.TestAgent.SetNumberGEvent",
            event_properties={
                "Key": "Number",
                "Value": 10000 + i
            },
            event_description=f"Rapid fire event #{i + 1} with value {10000 + i}"
        )
        if success:
            events_sent += 1
        time.sleep(0.2)
    
    # Event 5: Send events with different keys
    print("\n📤 Round 5: Sending events with different properties")
    print("-" * 50)
    
    # Test with different property keys
    test_properties = [
        {"Key": "Status", "Value": "Active"},
        {"Key": "Priority", "Value": 1},
        {"Key": "Counter", "Value": 999}
    ]
    
    for i, properties in enumerate(test_properties):
        agent_id = agent_ids[i % len(agent_ids)]  # Cycle through all agents
        success = publish_event(
            agent_id=agent_id,
            event_type="Aevatar.Application.Grains.Agents.TestAgent.SetNumberGEvent",
            event_properties=properties,
            event_description=f"Set {properties['Key']} to {properties['Value']}"
        )
        if success:
            events_sent += 1
        time.sleep(0.5)
    
    # Final summary
    print("\n" + "=" * 80)
    print("🎉 EVENT SENDING SUMMARY")
    print("=" * 80)
    print(f"📊 Total events sent: {events_sent}")
    print(f"📊 Agents targeted: {len(agent_ids)}")
    print(f"📊 Event rounds completed: 5")
    
    print("\n📋 Agent IDs used:")
    agent_names = [
        "TwitterAgent1", "TwitterAgent2", "TwitterAgent3"
    ]
    for i, (agent_id, name) in enumerate(zip(agent_ids, agent_names)):
        print(f"  {i + 1}. {name}: {agent_id}")
    
    print("\n✨ All event sending completed!")

if __name__ == "__main__":
    send_multiple_events()