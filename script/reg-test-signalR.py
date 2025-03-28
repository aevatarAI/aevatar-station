import time
import json
import logging
from uuid import uuid4
from signalrcore.hub_connection_builder import HubConnectionBuilder

# Configure logging
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(levelname)s - %(message)s'
)

# SignalR Hub URL
hub_url = "http://localhost:8001/api/agent/aevatarHub"

# Create SignalR Hub connection with automatic reconnect
hub_connection = HubConnectionBuilder() \
    .with_url(hub_url, options={
        "verify_ssl": False,
        "skip_negotiation": False,  # Enable negotiation
        "headers": {
            "User-Agent": "Python-SignalR-Client",
            "Content-Type": "application/json"
        },
        "negotiate_headers": {
            "User-Agent": "Python-SignalR-Client",
            "Content-Type": "application/json"
        },
        "handshake_response_timeout": 10000,  # 10 seconds
        "request_timeout": 10000,  # 10 seconds
        "protocols": ["json"]
    }) \
    .with_automatic_reconnect({
        "type": "raw",
        "keep_attempting": True,
        "retries": 3,
        "intervals": [1000, 2000, 5000]  # Retry intervals in milliseconds
    }) \
    .configure_logging(logging.DEBUG) \
    .build()

# Global variable to store received messages
received_messages = []

# Event listener for server-side events
def on_receive_response(message):
    try:
        if isinstance(message, str):
            message = json.loads(message)
        logging.info(f"📨 Raw message: {message}")
        
        # 处理 SignalRResponseEvent 类型的消息
        if isinstance(message, dict) and 'Message' in message and 'Data' in message:
            logging.info(f"📨 SignalRResponseEvent: Message={message['Message']}, Data={message['Data']}")
        
        received_messages.append(message)
    except Exception as e:
        logging.error(f"❌ Message handling error: {e}")

# Connection event handlers
def on_connection_open():
    logging.info("✅ Connection established")

def on_connection_close():
    logging.warning("⚠️ Connection closed")

def on_connection_error(error):
    logging.error(f"❌ Connection error: {error}")

# Register event handlers
hub_connection.on("ReceiveResponse", on_receive_response)
hub_connection.on_open(on_connection_open)
hub_connection.on_close(on_connection_close)
hub_connection.on_error(on_connection_error)

# Start SignalR connection
def start_signalr_connection():
    try:
        hub_connection.start()
        time.sleep(2)  # Wait for connection to establish
        
        # Check connection state
        for _ in range(3):  # Retry a few times
            if is_connection_active():
                logging.info("✅ SignalR connection started successfully")
                return True
            time.sleep(1)
            logging.info("Retrying connection...")
        
        logging.error("❌ Failed to establish WebSocket connection after retries")
        return False
    except Exception as e:
        logging.error(f"❌ SignalR connection failed: {e}")
        return False

# Check if connection is active
def is_connection_active():
    try:
        return hub_connection is not None and hub_connection.transport is not None and hub_connection.transport.state == 1
    except Exception as e:
        logging.error(f"Connection check error: {e}")
        return False

# Publish event to the server
def publish_event(method_name, greeting="Test message"):
    try:
        # 构建事件参数
        grain_type = "SignalRSample.GAgents.signalR"
        grain_key = str(uuid4()).replace("-", "")
        event_type_name = "NaiveTestEvent"
        event_json = json.dumps({"greeting": greeting})

        # 构建 grain_id
        grain_id = {
            "Type": grain_type,
            "Key": grain_key,
            "IsDefault": False,
            "TypeCode": 1,
            "N": 0
        }

        # 发送事件
        logging.info(f"📤 Publishing {method_name} event: {event_json}")
        result = hub_connection.send(
            method_name,
            [grain_id, event_type_name, event_json]
        )
        logging.info("✅ Event published successfully")
        return True
    except Exception as e:
        logging.error(f"❌ Failed to publish event: {e}")
        return False

# Pytest fixtures and tests
import pytest

@pytest.fixture(scope="module")
def signalr_connection():
    """Fixture to manage SignalR connection"""
    try:
        if not start_signalr_connection():
            pytest.fail("❌ Failed to start SignalR connection")
        yield hub_connection
    finally:
        hub_connection.stop()

def test_signalr_connection(signalr_connection):
    """Test SignalR connection establishment"""
    assert is_connection_active(), "SignalR connection should be active"

def test_publish_events(signalr_connection):
    """Test publishing events to SignalR hub"""
    # Test events to publish
    test_events = [
        ("PublishEventAsync", "Test message"),
        ("SubscribeAsync", "Subscribe message")
    ]

    # Publish each event
    for method_name, greeting in test_events:
        assert publish_event(method_name, greeting), f"Failed to publish event: {method_name}"
        time.sleep(1)  # Brief pause between events

def test_receive_response(signalr_connection):
    """Test receiving responses from SignalR hub"""
    # Clear any existing messages
    global received_messages
    received_messages = []

    try:
        # First subscribe to receive events
        assert publish_event("SubscribeAsync", "Subscribe message"), "Failed to subscribe"
        time.sleep(2)  # Wait for subscription to be processed

        # Then publish a test event
        assert publish_event("PublishEventAsync", "Test response message"), "Failed to publish event"

        # Wait for response with longer timeout
        max_wait = 10  # seconds
        start_time = time.time()
        while time.time() - start_time < max_wait:
            if received_messages:
                logging.info(f"Received {len(received_messages)} messages")
                break
            time.sleep(1)  # Longer sleep to reduce CPU usage
            logging.info("Waiting for response...")

        assert len(received_messages) > 0, "No response received"
    except Exception as e:
        logging.error(f"Test error: {e}")
        raise