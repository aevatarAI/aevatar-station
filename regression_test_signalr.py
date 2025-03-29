import pytest
import json
import time
import logging
from signalrcore.hub_connection_builder import HubConnectionBuilder

# Configure logging
logging.basicConfig(level=logging.DEBUG)

# SignalR Hub URL
HUB_URL = "http://localhost:8001/api/agent/aevatarHub"


@pytest.fixture(scope="module")
def hub_connection():
    """
    Create a SignalR connection and close it after the test ends
    """
    connection = HubConnectionBuilder() \
        .with_url(HUB_URL, options={"verify_ssl": False}) \
        .with_automatic_reconnect({
            "type": "raw",
            "keep_attempting": True,
            "retries": 3,
            "intervals": [0, 2000, 10000, 30000],
        }) \
        .configure_logging(logging.DEBUG) \
        .build()

    # Variables to track connection state and received messages
    connection_state = {"is_connected": False}
    received_messages = []

    def on_connection_open():
        connection_state["is_connected"] = True
        logging.info("✅ SignalR connection established successfully")

    def on_connection_close():
        connection_state["is_connected"] = False
        logging.info("❌ SignalR connection closed")

    def on_receive_response(message):
        logging.info(f"📨 Received message: {message}")
        received_messages.append(message)

    connection.on_open(on_connection_open)
    connection.on_close(on_connection_close)
    connection.on("ReceiveResponse", on_receive_response)

    logging.info("🔌 Connecting to SignalR...")
    connection.start()

    # Wait for the connection to establish (up to 30 seconds)
    for _ in range(30):
        if connection_state["is_connected"]:
            break
        time.sleep(1)
    else:
        pytest.fail("❌ Failed to establish SignalR connection")

    yield connection, received_messages

    # Stop the connection
    connection.stop()
    logging.info("❌ SignalR connection terminated")


def test_signalr_connection_active(hub_connection):
    """
    Test if the SignalR connection is established successfully
    """
    connection, _ = hub_connection
    assert connection is not None, "Hub connection is None"
    logging.info("✅ Connection test passed!")


def test_publish_event(hub_connection):
    """
    Test publishing an event to the Hub
    """
    connection, _ = hub_connection
    method_name = "PublishEventAsync"
    grain_type = "SignalRSample.GAgents.Aevatar.SignalRDemo"
    grain_key = "cd6b8f09214673d3cade4e832627b4f6"
    event_type_name = "SignalRSample.GAgents.NaiveTestEvent"
    event_json = json.dumps({"Greeting": "Hello from Python"})

    result = connection.send(method_name, [f"{grain_type}/{grain_key}", event_type_name, event_json])
    logging.info(f"✅ Event published successfully: {result}")
    assert result is not None, "Failed to send event"


@pytest.mark.parametrize("test_event", [
    {"Greeting": "Message A"},
    {"Greeting": "Message B"}
])
def test_dynamic_subscribe_event(hub_connection, test_event):
    """
    Dynamically send different events and verify if a response is received
    """
    connection, received_messages = hub_connection
    method_name = "SubscribeAsync"
    grain_type = "SignalRSample.GAgents.Aevatar.SignalRDemo"
    grain_key = "cd6b8f09214673d3cade4e832627b4f6"
    event_type_name = "SignalRSample.GAgents.NaiveTestEvent"
    event_json = json.dumps(test_event)

    # Clear received messages
    received_messages.clear()

    # Send the event
    result = connection.send(method_name, [f"{grain_type}/{grain_key}", event_type_name, event_json])
    assert result is not None, "Failed to send event"
    logging.info(f"✅ Event sent successfully: {test_event}")

    # Wait for a response (up to 3 seconds)
    max_wait_time = 3
    start_time = time.time()
    while time.time() - start_time < max_wait_time:
        if received_messages:
            break
        logging.info("⏳ Waiting for server response...")
        time.sleep(1)

    # Verify if a response is received
    assert len(received_messages) > 0, "❌ No response received from the server"
    logging.info(f"✅ Received messages: {received_messages}")


def test_dynamic_subscribe_event_failure(hub_connection):
    """
    Dynamically send invalid event data and verify failure scenarios
    """
    connection, received_messages = hub_connection
    method_name = "SubscribeAsync"
    grain_type = "SignalRSample.GAgents.Aevatar.SignalRDemo1"  # Invalid grain_type
    grain_key = "cd6b8f09214673d3cade4e832627b4f6"
    event_type_name = "SignalRSample.GAgents.NaiveTestEvent"
    event_json = json.dumps({"Greeting": "Test Failure Case"})

    # Clear received messages
    received_messages.clear()

    # Send the event
    result = connection.send(method_name, [f"{grain_type}/{grain_key}", event_type_name, event_json])
    assert result is not None, "Failed to send event"
    logging.info(f"✅ Event sent successfully (failure test): {event_json}")

    # Wait for a response (up to 3 seconds)
    max_wait_time = 3
    start_time = time.time()
    while time.time() - start_time < max_wait_time:
        if received_messages:  # Check if any messages are received
            break
        logging.info("⏳ Waiting for server response (failure test)...")
        time.sleep(1)

    # Verify no response is received
    assert len(received_messages) == 0, "❌ Unexpected response received from the server"
    logging.info("✅ Failure test passed: No response received")