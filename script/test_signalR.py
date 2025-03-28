import time
from signalrcore.hub_connection_builder import HubConnectionBuilder
import logging
import sys

# SignalR Hub URL
HUB_URL = "http://localhost:8001/api/agent/aevatarHub"

def terminate_test_on_failure(exception_message):
    """
    Terminates the entire test process upon failure with exit code 1.
    """
    print(f"[Test Failure] {exception_message}")
    exit(1)  # Exit the process with a failure code


def terminate_test_on_success():
    """
    Terminates the entire test process upon success with exit code 0.
    """
    print("[Test Success] All tests passed successfully!")
    exit(0)  # Exit the process with a success code


def test_signalr_workflow():
    """
    Unified method to test SignalR connection, sending, receiving, and disconnecting.
    """
    try:
        # Step 1: Establish SignalR connection
        hub_connection = HubConnectionBuilder() \
            .with_url(HUB_URL) \
            .configure_logging(logging.DEBUG) \
            .build()
        
        # Wait to ensure the connection is established
        hub_connection.start()
        time.sleep(2)  # Wait for connection to stabilize
        if not hub_connection.started:
            terminate_test_on_failure("Failed to establish connection to the SignalR Hub.")
        print("[Step 1: Connection] Successfully connected to the SignalR Hub.")

        # Step 2: Setup a listener for server messages
        received_messages = []

        def on_receive_message(message):
            print(f"[Step 2: ReceiveMessage] Received: {message}")
            received_messages.append(message)

        hub_connection.on("ReceiveMessage", on_receive_message)

        # Step 3: Test sending a message
        test_user = "TestUser"
        test_message = "Hello from pytest!"
        print(f"[Step 3: Sending] Sending message '{test_message}' as user '{test_user}'...")
        hub_connection.send("SendMessage", [test_user, test_message])
        time.sleep(2)  # Wait to possibly receive messages

        if len(received_messages) == 0:
            terminate_test_on_failure("No messages received from the server after sending.")
        print("[Step 3: Sending] Message sent and response received successfully.")

        # Step 4: Disconnect from SignalR
        hub_connection.stop()
        time.sleep(1)
        if hub_connection.started:
            terminate_test_on_failure("Failed to disconnect from the SignalR Hub.")
        print("[Step 4: Disconnect] Successfully disconnected from the SignalR Hub.")

        # If all tests pass, terminate with success
        terminate_test_on_success()

    except Exception as e:
        terminate_test_on_failure(f"An exception occurred: {e}")


if __name__ == "__main__":
    # Execute the SignalR workflow test
    test_signalr_workflow()
    
    
## required
## pip install signalrcore    