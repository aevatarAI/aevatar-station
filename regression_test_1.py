import time
import json
from signalrcore.hub_connection_builder import HubConnectionBuilder
from uuid import uuid4
import logging

# Configure logging
logging.basicConfig(level=logging.DEBUG)

# SignalR Hub URL
hub_url = "http://localhost:8001/api/agent/aevatarHub"  # Ensure this matches the server configuration

# Create SignalR Hub connection with automatic reconnect
hub_connection = HubConnectionBuilder() \
    .with_url(hub_url) \
    .with_automatic_reconnect({
        "type": "raw",
        "keep_attempting": True,
        "retries": 3,
        "intervals": [0, 2000, 10000, 30000]  # Retry intervals in milliseconds
    }) \
    .build()

# Event listener for server-side events
def on_receive_response(message):
    logging.info(f"[Event Received] {message}")

hub_connection.on("ReceiveResponse", on_receive_response)

# Start SignalR connection
def start_signalr_connection():
    try:
        hub_connection.start()
        logging.info("SignalR connection has started successfully.")
    except Exception as e:
        logging.error(f"SignalR connection failed: {e}")
        exit(1)

# Check if connection is active
def is_connection_active():
    try:
        # Try to send a ping or check if the hub_connection is not None
        return hub_connection is not None and hub_connection._transport is not None
    except Exception:
        return False

# Publish event to the server
def publish_event(method_name):
    try:
        # Prepare event payload
        event_data = {
            "Greeting": "Test message"
        }
        event_json = json.dumps(event_data)

        # Grain type and key
        grain_type = "SignalRSample.GAgents.Aevatar.SignalRDemo"
        grain_key = "cd6b8f09214673d3cade4e832627b4f6"
        event_type_name = "SignalRSample.GAgents.NaiveTestEvent"

        # Send event with retry logic
        send_event_with_retry(method_name, grain_type, grain_key, event_type_name, event_json)
        logging.info(f"✅ Success: Event published using {method_name}")
    except Exception as ex:
        logging.error(f"❌ Error during {method_name}: {str(ex)}")

# Retry logic for sending events
def send_event_with_retry(method_name, grain_type, grain_key, event_type_name, event_json):
    max_retries = 3
    retry_count = 0

    while retry_count < max_retries:
        try:
            # Send message to server
            signal_r_grain_id = hub_connection.send(method_name, [grain_type+"/"+grain_key, event_type_name, event_json])
            logging.info(f"SignalRGAgentGrainId: {signal_r_grain_id}")
            return
        except Exception as ex:
            retry_count += 1
            logging.warning(f"Retry {retry_count}/{max_retries} failed: {str(ex)}")
            if retry_count >= max_retries:
                raise  # Rethrow the exception after maximum retries
            time.sleep(1 * retry_count)  # Delay before the next retry

# Main task
if __name__ == "__main__":
    try:
        # Start SignalR connection
        start_signalr_connection()

        # Publish test events
        publish_event("PublishEventAsync")
        publish_event("SubscribeAsync")

        # Keep connection alive indefinitely
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        logging.info("Exiting...")
    finally:
        hub_connection.stop()