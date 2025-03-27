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
        
        # Step 1: Establish SignalR connection
        def on_connect():
            print("[Step 1: Connection] Connected to the hub")

        def on_disconnect():
            print("[Step 1: Connection] Disconnected from the hub")

        def on_error(error):
            print(f"[Error] {error}")

        hub_connection.on_open(on_connect)
        hub_connection.on_close(on_disconnect)
        hub_connection.on_error(on_error)

        hub_connection.start()
        time.sleep(2)  # Wait for connection to stabilize

        time.sleep(3)  # Wait a bit to ensure connection is stable

        # Step 2: Disconnect from SignalR
        try:
            hub_connection.stop()
            time.sleep(1)
            print("[Step 4: Disconnect] Successfully disconnected from the SignalR Hub.")
        except Exception as e:
            terminate_test_on_failure(f"Failed to disconnect: {e}")

        # If all tests pass, terminate with success
        terminate_test_on_success()

    except Exception as e:
        terminate_test_on_failure(f"An exception occurred: {e}")


if __name__ == "__main__":
    # Execute the SignalR workflow test
    test_signalr_workflow()
    
    
## required
## pip install signalrcore    