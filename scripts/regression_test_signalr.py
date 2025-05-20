import os
import pytest
import json
import time
import logging
from uuid import uuid4
from signalrcore.hub_connection_builder import HubConnectionBuilder

# Ignore SSL/TLS warnings
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# Ignore DeprecationWarnings
import warnings
warnings.simplefilter("ignore", category=DeprecationWarning)

# Configure logging
logging.basicConfig(
    # level=logging.DEBUG,  # Ensure logs are printed
    format="%(asctime)s - %(levelname)s - %(message)s",
    datefmt="%Y-%m-%d %H:%M:%S",
)

# SignalR Hub URL
# HUB_URL = "http://localhost:5001/aevatarHub"
# HUB_URL = "http://192.168.3.11:8001/api/agent/aevatarHub"
# HUB_URL = "http://localhost:8001/api/agent/aevatarHub"
# HUB_URL = "http://localhost:8308/api/agent/aevatarHub"  
API_HOST = os.getenv("API_HOST")
HUB_URL = f"{API_HOST}/api/agent/aevatarHub"

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
        "retries": 1,
        "intervals": [0, 2000, 10000, 30000],
    }) \
        .configure_logging(logging.DEBUG) \
        .build()

    # Variables to track connection state and received messages
    connection_state = {"is_connected": False}
    received_messages = []

    def on_connection_open():
        connection_state["is_connected"] = True

    def on_connection_close():
        connection_state["is_connected"] = False

    def on_receive_response(message):
        received_messages.append(message)

    connection.on_open(on_connection_open)
    connection.on_close(on_connection_close)
    connection.on("ReceiveResponse", on_receive_response)

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


def send_event_and_wait(connection, received_messages, method_name, params, wait_time=10):
    """
    Helper method to send an event and wait for a response.
    """
    # Clear any previous messages
    received_messages.clear()

    # Send the event
    try:
        result = connection.send(method_name, params)


        # Ensure result is not None
        assert result is not None, "Failed to send event"

    except Exception as e:
        pytest.fail(f"Exception while sending event: {e}")

    # Wait for a response
    start_time = time.time()
    while time.time() - start_time < wait_time:
        if received_messages:
            break
        time.sleep(1)

    return received_messages


def test_signalr_connection_active(hub_connection):
    """
    Test if the SignalR connection is established successfully
    """
    connection, _ = hub_connection
    assert connection is not None, "Hub connection is None"


def test_publish_async(hub_connection):
    """
    Test the PublishEventAsync method
    """
    connection, received_messages = hub_connection
    method_name = "PublishEventAsync"
    grain_type = "Aevatar.Application.Grains.Agents.TestAgent.SignalRTestGAgent"
    grain_key = str(uuid4()).replace("-", "")
    event_type_name = "Aevatar.Application.Grains.Agents.TestAgent.NaiveTestEvent"
    event_json = json.dumps({"Greeting": "Greeting PublishEvent Test"})

    params = [f"{grain_type}/{grain_key}", event_type_name, event_json]
    responses = send_event_and_wait(connection, received_messages, method_name, params)

    assert len(responses) > 0, "❌ No response received from the server"


def test_subscribe_async(hub_connection):
    """
    Test the SubscribeAsync method
    """
    connection, received_messages = hub_connection
    method_name = "SubscribeAsync"
    grain_type = "Aevatar.Application.Grains.Agents.TestAgent.SignalRTestGAgent"
    grain_key = str(uuid4()).replace("-", "")
    event_type_name = "Aevatar.Application.Grains.Agents.TestAgent.NaiveTestEvent"
    event_json = json.dumps({"Greeting": "Greeting PublishEvent Test"})

    params = [f"{grain_type}/{grain_key}", event_type_name, event_json]
    responses = send_event_and_wait(connection, received_messages, method_name, params)

    assert len(responses) > 0, "❌ No response received from the server"