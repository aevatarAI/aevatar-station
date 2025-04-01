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
# HUB_URL = "http://192.168.3.11:8001/api/agent/aevatarHub"
# HUB_URL = "http://localhost:8001/api/agent/aevatarHub"
# HUB_URL = "http://localhost:8308/api/agent/aevatarHub" 
HUB_URL = "https://station-developer-staging.aevatar.ai/test-client/api/agent/aevatarHub"

TOKEN = "eyJhbGciOiJSUzI1NiIsImtpZCI6IkQyRjhGMkUxOEJFMEMwRTMwRTkxRjgxNUI0MDNDNTlGQjZGNTZFRDIiLCJ4NXQiOiIwdmp5NFl2Z3dPTU9rZmdWdEFQRm43YjFidEkiLCJ0eXAiOiJhdCtqd3QifQ.eyJpc3MiOiJodHRwOi8vbG9jYWxob3N0OjgwODIvIiwiZXhwIjoxNzQzNjczODg1LCJpYXQiOjE3NDM1MDEwODYsImF1ZCI6IkFldmF0YXIiLCJzY29wZSI6IkFldmF0YXIiLCJqdGkiOiI0NTY1NjE2NS02YTExLTQ4MjYtOGEwMi1hOGY4ODMwNGZhZjEiLCJzdWIiOiI4OGUwZjZkNS1kODg2LWNjNjAtZGMyMS0zYTE3NjA3NDBhMmYiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJhZG1pbiIsImVtYWlsIjoiYWRtaW5AYWJwLmlvIiwicm9sZSI6ImFkbWluIiwiZ2l2ZW5fbmFtZSI6ImFkbWluIiwicGhvbmVfbnVtYmVyX3ZlcmlmaWVkIjoiRmFsc2UiLCJlbWFpbF92ZXJpZmllZCI6IkZhbHNlIiwidW5pcXVlX25hbWUiOiJhZG1pbiIsIm9pX3Byc3QiOiJBZXZhdGFyQXV0aFNlcnZlciIsImNsaWVudF9pZCI6IkFldmF0YXJBdXRoU2VydmVyIiwib2lfdGtuX2lkIjoiYzNiYjBlOGMtZDc3ZC1hY2JlLTY5MTAtM2ExOTAyZjAyNWNkIn0.FJ75JJSp7bg55B8IqLvTF3H788O4wIBYWfi4h06_on-uCGPgYX49GZ-KE1ct3oT8EqRa43ns72mKJcpZRkZVy5RXaNm0zK559l_WkE-T68BugGKDEn3GFWYfIUgRok-B5rsUVxGOfVraF3mgXIA-iTv6dxodhPgDQB2aDa1ffZXXPa9KIwtxQ6Pxc-MVAr6r0U_8ILbXFeesk-AyXGzUrrx72hhg3W7iHNmCVGQnNjlCLmIvDz-F07v-Qb_c3jFXqV8IuDxTpX0IMFOhobl5ccwGmmHihwU8thr-XPi9cCaWLigfVqRTYpNmxfM088TPenIF1WUlxpVyqkk3SuaMkg.eyJpc3MiOiJodHRwOi8vbG9jYWxob3N0OjgwODIvIiwiZXhwIjoxNzQzNjczODg1LCJpYXQiOjE3NDM1MDEwODYsImF1ZCI6IkFldmF0YXIiLCJzY29wZSI6IkFldmF0YXIiLCJqdGkiOiI0NTY1NjE2NS02YTExLTQ4MjYtOGEwMi1hOGY4ODMwNGZhZjEiLCJzdWIiOiI4OGUwZjZkNS1kODg2LWNjNjAtZGMyMS0zYTE3NjA3NDBhMmYiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJhZG1pbiIsImVtYWlsIjoiYWRtaW5AYWJwLmlvIiwicm9sZSI6ImFkbWluIiwiZ2l2ZW5fbmFtZSI6ImFkbWluIiwicGhvbmVfbnVtYmVyX3ZlcmlmaWVkIjoiRmFsc2UiLCJlbWFpbF92ZXJpZmllZCI6IkZhbHNlIiwidW5pcXVlX25hbWUiOiJhZG1pbiIsIm9pX3Byc3QiOiJBZXZhdGFyQXV0aFNlcnZlciIsImNsaWVudF9pZCI6IkFldmF0YXJBdXRoU2VydmVyIiwib2lfdGtuX2lkIjoiYzNiYjBlOGMtZDc3ZC1hY2JlLTY5MTAtM2ExOTAyZjAyNWNkIn0.FJ75JJSp7bg55B8IqLvTF3H788O4wIBYWfi4h06_on-uCGPgYX49GZ-KE1ct3oT8EqRa43ns72mKJcpZRkZVy5RXaNm0zK559l_WkE-T68BugGKDEn3GFWYfIUgRok-B5rsUVxGOfVraF3mgXIA-iTv6dxodhPgDQB2aDa1ffZXXPa9KIwtxQ6Pxc-MVAr6r0U_8ILbXFeesk-AyXGzUrrx72hhg3W7iHNmCVGQnNjlCLmIvDz-F07v-Qb_c3jFXqV8IuDxTpX0IMFOhobl5ccwGmmHihwU8thr-XPi9cCaWLigfVqRTYpNmxfM088TPenIF1WUlxpVyqkk3SuaMkg"

@pytest.fixture(scope="module")
def hub_connection():
    """
    Create a SignalR connection and close it after the test ends
    """
    connection = HubConnectionBuilder() \
        .with_url(HUB_URL, options={"verify_ssl": False})  \
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

    return received_messages


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

    assert len(responses) > 0, "❌ No response received from the server"
    logging.info(f"✅ PublishEventAsync test passed. responses=: {responses}")


def test_subscribe_async(hub_connection):
    """
    Test the SubscribeAsync method
    """
    connection, received_messages = hub_connection
    method_name = "SubscribeAsync"
    grain_type = "SignalRSample.GAgents.SignalRTestGAgent"
    grain_key = str(uuid4()).replace("-", "")
    event_type_name = "SignalRSample.GAgents.NaiveTestEvent"
    event_json = json.dumps({"Greeting": "Greeting PublishEvent Test"})

    params = [f"{grain_type}/{grain_key}", event_type_name, event_json]
    responses = send_event_and_wait(connection, received_messages, method_name, params)

    assert len(responses) > 0, "❌ No response received from the server"
    logging.info(f"✅ PublishEventAsync test passed. responses=: {responses}")