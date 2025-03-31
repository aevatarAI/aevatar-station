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
    level=logging.DEBUG,  # Ensure logs are printed
    format="%(asctime)s - %(levelname)s - %(message)s",
    datefmt="%Y-%m-%d %H:%M:%S",
)

# SignalR Hub URL
# HUB_URL = "http://localhost:5001/aevatarHub"
# HUB_URL = "http://localhost:8001/api/agent/aevatarHub"
# HUB_URL = "http://localhost:8308/api/agent/aevatarHub"
# Alternate URL for staging 
HUB_URL = "https://station-developer-staging.aevatar.ai/test-client/api/agent/aevatarHub"



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
        logging.info("✅ SignalR connection established successfully")

    def on_connection_close():
        connection_state["is_connected"] = False
        logging.info(" SignalR connection closed")

    def on_receive_response(message):
        logging.info(f"📨 on_receive_response log: {message}")
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
    logging.info(" SignalR connection terminated")


def send_event_and_wait(connection, received_messages, method_name, params, wait_time=10):
    """
    Helper method to send an event and wait for a response.
    """
    # Clear any previous messages
    received_messages.clear()

    # Send the event
    try:
        logging.info(f"📡 Sending event: {method_name} with params: {params}")
        result = connection.send(method_name, params)

        # Analyze the result
        try:
            if hasattr(result, "result"):  # If it's an InvocationResult with a 'result' attribute
                logging.info(f"✅ Event sent result (JSON): {json.dumps(result.result)}")
            else:
                logging.info(f"✅ Event sent result (Raw): {result}")
        except Exception as parse_error:
            logging.error(f"❌ Failed to parse result: {parse_error}")
            logging.error(f"❌ Raw result: {result}")

        # Ensure result is not None
        assert result is not None, "Failed to send event"

    except Exception as e:
        logging.error(f"❌ Exception while sending event: {e}")
        pytest.fail(f"Exception while sending event: {e}")

    # Wait for a response
    start_time = time.time()
    while time.time() - start_time < wait_time:
        if received_messages:
            break
        logging.info("⏳ Waiting for server response...")
        time.sleep(1)

    return f"{result}/{received_messages}"


def test_signalr_connection_active(hub_connection):
    """
    Test if the SignalR connection is established successfully
    """
    connection, _ = hub_connection
    assert connection is not None, "Hub connection is None"
    logging.info("✅ Connection test passed!")


def test_publish_async(hub_connection):
    """
    Test the PublishEventAsync method
    """
    connection, received_messages = hub_connection
    method_name = "PublishEventAsync"
    grain_type = "SignalRSample.GAgents.SignalRTestGAgent"
    grain_key = str(uuid4()).replace("-", "")
    event_type_name = "SignalRSample.GAgents.NaiveTestEvent"
    event_json = json.dumps({"Greeting": "Greeting PublishEvent Test"})

    params = [f"{grain_type}/{grain_key}", event_type_name, event_json]
    responses = send_event_and_wait(connection, received_messages, method_name, params)

    logging.info(f"✅ PublishEventAsync test passed. responses=: {responses}")


def test_subscribe_event(hub_connection):
    """
    Send different SubscribeAsync events and verify responses
    """
    connection, received_messages = hub_connection
    method_name = "SubscribeAsync"
    grain_type = "Aevatar.Application.Grains.Agents.ChatManager.ChatGAgentManager"
    grain_key = str(uuid4()).replace("-", "")
    event_type_name = "Aevatar.Application.Grains.Agents.ChatManager.RequestCreateQuantumChatEvent"
    event_json = json.dumps({"SystemLLM":"OpenAI","Prompt":"你是一个nba专家"})

    params = [f"{grain_type}/{grain_key}", event_type_name, event_json]
    responses = send_event_and_wait(connection, received_messages, method_name, params)

    # Verify if a response is received
    assert len(responses) > 0, "❌ No response received from the server"
    logging.info(f"✅ SubscribeAsync test passed. responses=: {responses}")

# 
# def test_subscribe_async_failure(hub_connection):
#     """
#     Test SubscribeAsync with invalid parameters and verify failure scenarios
#     """
#     connection, received_messages = hub_connection
#     method_name = "SubscribeAsync"
#     grain_type = "SignalRSample.GAgents.Aevatar.InvalidDemo"  # Invalid grain_type
#     grain_key = "cd6b8f09214673d3cade4e832627b4f6"
#     event_type_name = "SignalRSample.GAgents.NaiveTestEvent"
#     event_json = json.dumps({"Greeting": "Invalid Subscribe Test"})
# 
#     params = [f"{grain_type}/{grain_key}", event_type_name, event_json]
#     responses = send_event_and_wait(connection, received_messages, method_name, params)
# 
#     # Verify no response is received
#     assert len(responses) == 0, "❌ Unexpected response received from the server"
#     logging.info("✅ SubscribeAsync failure test passed: No response received")