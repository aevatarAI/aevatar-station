import pytest
import time
import json
import logging
from dataclasses import dataclass
from typing import Optional, List
from uuid import uuid4
from signalrcore.hub_connection_builder import HubConnectionBuilder
from threading import Event, Lock

# Configure logging
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(levelname)s - %(message)s'
)

@dataclass
class SignalRConfig:
    """SignalR configuration settings"""
    hub_url: str = "http://localhost:8001/api/agent/aevatarHub"
    verify_ssl: bool = False
    retry_count: int = 3
    retry_intervals: List[int] = None
    connection_timeout: int = 30

    def __post_init__(self):
        if self.retry_intervals is None:
            self.retry_intervals = [1000, 2000, 5000]

class TestEvent:
    """Test event data structure"""
    def __init__(self, greeting: str = "Test message"):
        self.greeting = greeting

    def to_json(self) -> str:
        return json.dumps({"greeting": self.greeting})

class SignalRTestClient:
    """SignalR test client wrapper"""
    def __init__(self, config: SignalRConfig):
        self.config = config
        self.connection = None
        self.received_messages = []
        self.connection_established = Event()
        self.connection_lock = Lock()
        self._setup_connection()

    def _setup_connection(self):
        """Setup SignalR connection with configuration"""
        self.connection = HubConnectionBuilder() \
            .with_url(self.config.hub_url, options={
                "verify_ssl": self.config.verify_ssl,
                "skip_negotiation": False,
                "headers": {
                    "User-Agent": "Python-SignalR-Client",
                    "Content-Type": "application/json"
                },
                "negotiate_headers": {
                    "User-Agent": "Python-SignalR-Client",
                    "Content-Type": "application/json"
                }
            }) \
            .with_automatic_reconnect({
                "type": "raw",
                "keep_attempting": True,
                "retries": self.config.retry_count,
                "intervals": self.config.retry_intervals
            }) \
            .configure_logging(logging.DEBUG) \
            .build()

        # Register event handlers
        self.connection.on("ReceiveResponse", self._on_message_received)
        self.connection.on_open(self._on_connection_open)
        self.connection.on_close(self._on_connection_close)
        self.connection.on_error(self._on_connection_error)

    def _on_message_received(self, message):
        """Handle received messages"""
        try:
            # 将消息转换为字典
            if isinstance(message, str):
                message = json.loads(message)
            logging.info(f"📨 Raw message: {message}")
            
            # 处理 SignalRResponseEvent 类型的消息
            if isinstance(message, dict) and 'Message' in message and 'Data' in message:
                logging.info(f"📨 SignalRResponseEvent: Message={message['Message']}, Data={message['Data']}")
            
            self.received_messages.append(message)
        except Exception as e:
            logging.error(f"❌ Message handling error: {e}")

    def _on_connection_open(self):
        """Handle connection open"""
        logging.info("✅ Connection established")
        self.connection_established.set()

    def _on_connection_close(self):
        """Handle connection close"""
        logging.warning("⚠️ Connection closed")
        self.connection_established.clear()

    def _on_connection_error(self, error):
        """Handle connection error"""
        logging.error(f"❌ Connection error: {error}")

    def start(self) -> bool:
        """Start SignalR connection with timeout"""
        try:
            self.connection.start()
            return self.connection_established.wait(timeout=self.config.connection_timeout)
        except Exception as e:
            logging.error(f"Failed to start connection: {e}")
            return False

    def stop(self):
        """Stop SignalR connection"""
        if self.connection:
            self.connection.stop()

    def publish_event(self, method_name: str, event: TestEvent) -> Optional[str]:
        """Publish test event to SignalR hub"""
        try:
            # 构建事件参数
            grain_type = "SignalRSample.GAgents.signalR"
            grain_key = str(uuid4()).replace("-", "")
            event_type_name = "NaiveTestEvent"
            event_json = event.to_json()

            # 构建 grain_id
            grain_id = {
                "Type": grain_type,
                "Key": grain_key,
                "IsDefault": False,
                "TypeCode": 1,
                "N": 0
            }

            # 发送事件
            result = self.connection.send(
                method_name,
                [grain_id, event_type_name, event_json]
            )
            logging.info(f"✅ Event published successfully: {method_name}")
            return grain_id
        except Exception as e:
            logging.error(f"❌ Failed to publish event: {e}")
            return None

@pytest.fixture
def signalr_client():
    """Fixture to create and manage SignalR test client"""
    config = SignalRConfig()
    client = SignalRTestClient(config)
    yield client
    client.stop()

def test_signalr_connection(signalr_client):
    """Test SignalR connection establishment"""
    assert signalr_client.start(), "Failed to establish SignalR connection"

def test_publish_events(signalr_client):
    """Test publishing events to SignalR hub"""
    # Ensure connection is established
    assert signalr_client.start(), "Failed to establish SignalR connection"

    # Test events to publish
    test_events = [
        ("PublishEventAsync", TestEvent()),
        ("SubscribeAsync", TestEvent())
    ]

    # Publish each event
    for method_name, event in test_events:
        grain_id = signalr_client.publish_event(method_name, event)
        assert grain_id is not None, f"Failed to publish event: {method_name}"
        time.sleep(1)  # Brief pause between events

def test_receive_response(signalr_client):
    """Test receiving responses from SignalR hub"""
    # Ensure connection is established
    assert signalr_client.start(), "Failed to establish SignalR connection"

    # Clear any existing messages
    signalr_client.received_messages.clear()

    # First subscribe to receive events
    event = TestEvent(greeting="Subscribe message")
    grain_id = signalr_client.publish_event("SubscribeAsync", event)
    assert grain_id is not None, "Failed to subscribe"
    time.sleep(2)  # Wait for subscription to be processed

    # Then publish a test event
    event = TestEvent(greeting="Test response message")
    grain_id = signalr_client.publish_event("PublishEventAsync", event)
    assert grain_id is not None, "Failed to publish event"

    # Wait for response with longer timeout
    max_wait = 30  # seconds
    start_time = time.time()
    while time.time() - start_time < max_wait:
        if signalr_client.received_messages:
            logging.info(f"Received {len(signalr_client.received_messages)} messages")
            break
        time.sleep(1)  # Longer sleep to reduce CPU usage
        logging.info("Waiting for response...")

    assert len(signalr_client.received_messages) > 0, "No response received"

if __name__ == "__main__":
    pytest.main([__file__, "-v"])