import requests
import json
import time

# Configuration
BASE_URL = "http://localhost:8308"

def create_agent(agent_type, name, properties=None):
    """Create an agent with the specified type and name"""
    headers = {
        'accept': 'application/json',
        'Content-Type': 'application/json'
    }
    
    agent_data = {
        "agentType": agent_type,
        "name": name
    }
    if properties:
        agent_data["properties"] = properties
    
    try:
        response = requests.post(f'{BASE_URL}/api/agent/', headers=headers, json=agent_data)
        
        if response.status_code == 200:
            response_data = response.json()
            print(f"📄 Raw response: {response.text}")
            
            # Handle different response formats
            if 'data' in response_data and response_data['data']:
                if isinstance(response_data['data'], dict) and 'id' in response_data['data']:
                    agent_id = response_data['data']['id']
                elif isinstance(response_data['data'], str):
                    agent_id = response_data['data']
                else:
                    print(f"❌ Unexpected data format: {response_data['data']}")
                    return None
                
                print(f"✅ Created agent '{name}' with ID: {agent_id}")
                return agent_id
            else:
                print(f"❌ No data in response: {response_data}")
                return None
        else:
            print(f"❌ Failed to create agent '{name}': Status {response.status_code}")
            print(f"   Response: {response.text}")
            return None
            
    except Exception as e:
        print(f"❌ Error creating agent '{name}': {str(e)}")
        return None


def publish_event(agent_id, event_type, event_properties):
    """Publish an event to an agent"""
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
    
    response = requests.post(event_url, headers=headers, json=event_payload)
    
    if response.status_code == 200:
        print(f"✅ Event published successfully to agent {agent_id}")
        print(f"   Event Type: {event_type}")
        print(f"   Response: {response.text}")
        return True
    else:
        print(f"❌ Failed to publish event: {response.text}")
        return False

def create_multiple_agents(count=3):
    """Create multiple agents and publish events"""
    print(f"🚀 Creating {count} agents...")
    print("=" * 60)
    
    created_agents = []
    
    for i in range(1, count + 1):
        print(f"\n📋 Creating Agent {i}:")
        print("-" * 30)
        
        # Create agent
        agent_name = f"TwitterAgent{i}"
        agent_id = create_agent(
            agent_type="testagentwithconfiguration",
            name=agent_name
        )
        
        if not agent_id:
            print(f"❌ Failed to create agent {i}, skipping...")
            continue
        
        created_agents.append({
            'agent_id': agent_id,
            'agent_name': agent_name
        })
        print(f"✅ Agent {i} created successfully!")
        
        # Add delay between creations
        time.sleep(1)
    
    return created_agents

def publish_events_to_agents(agents):
    """Publish 3 events to each agent"""
    print(f"\n📤 Publishing 3 events to each of {len(agents)} agents...")
    print("=" * 60)
    
    for i, agent in enumerate(agents, 1):
        print(f"\n🎯 Publishing events to Agent {i}:")
        print("-" * 40)
        
        # Publish 3 events to agent
        events_success = 0
        for event_num in range(1, 4):
            success = publish_event(
                agent_id=agent['agent_id'],
                event_type="Aevatar.Application.Grains.Agents.TestAgent.SetNumberGEvent",
                event_properties={
                    "Key": "Number",
                    "Value": i * 10 + event_num
                }
            )
            if success:
                events_success += 1
            time.sleep(0.5)
        
        print(f"✅ Published {events_success}/3 events to {agent['agent_name']}")
        
        # Add delay between agents
        time.sleep(1)

def main():
    """Main execution function"""
    print("🚀 Starting Agent Creation Pattern")
    print("=" * 80)
    
    # Create multiple agents
    agents = create_multiple_agents(count=3)
    
    if not agents:
        print("❌ No agents were created successfully!")
        return
    
    # Publish events to all agents
    publish_events_to_agents(agents)
    
    # Final summary
    print("\n" + "=" * 80)
    print("🎉 FINAL SUMMARY")
    print("=" * 80)
    print(f"📊 Total agents created: {len(agents)}")
    print(f"📊 Total events published: {len(agents) * 3}")
    
    print("\n📋 Created Agents:")
    for i, agent in enumerate(agents, 1):
        print(f"  {i}. {agent['agent_name']} ({agent['agent_id']})")
    
    print("\n✨ All operations completed successfully!")

if __name__ == "__main__":
    main()