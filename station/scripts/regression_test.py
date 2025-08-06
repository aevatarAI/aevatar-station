# regression_test.py
import os
import sys
import time
import pytest
import requests
import logging
import urllib3

# Print startup notification
print("=" * 80)
print("🧪 REGRESSION_TEST.PY IS NOW RUNNING!")
print("📍 Aevatar Station Regression Test Suite Starting...")
print("🕐 Started at:", time.strftime("%Y-%m-%d %H:%M:%S"))
print("=" * 80)

# Disable SSL warnings for testing with self-signed certificates
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

logger = logging.getLogger(__name__)
logger.setLevel(logging.DEBUG)


TEST_AGENT = "agenttest"
WORKFLOW_VIEW_AGENT = "Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView.WorkflowViewGAgent"
STATE_NAME = "FrontAgentState"
AGENT_NAME = "TestAgent"
AGENT_NAME_MODIFIED = "TestAgentNameModified"
EVENT_TYPE = "Aevatar.Application.Grains.Agents.TestAgent.FrontTestCreateEvent"
EVENT_PARAM = "Name"

# Docker environment detection
RUNNING_IN_DOCKER = os.getenv("RUNNING_IN_DOCKER", "false").lower() == "true" or os.path.exists('/.dockerenv')

# Environment-aware configuration
if RUNNING_IN_DOCKER:
    # Use internal Docker network URLs
    AUTH_HOST = os.getenv("AUTH_HOST", "http://authserver:8082")
    API_HOST = os.getenv("API_HOST", "http://api:8001")
    # Disable SSL verification in Docker (internal network)
    KUBECONFIG = os.getenv("KUBECONFIG", "/app/kubeconfig")
else:
    # Use localhost URLs for local development
    AUTH_HOST = os.getenv("AUTH_HOST", "https://localhost:44300")
    API_HOST = os.getenv("API_HOST", "https://localhost:44301")
    # Disable SSL verification due to self-signed certs
    KUBECONFIG = None

CLIENT_ID = os.getenv("CLIENT_ID", "AevatarTestClient")
CLIENT_SECRET = os.getenv("CLIENT_SECRET", "test-secret-key")
INDEX_NAME = f"aevatar-{CLIENT_ID}-testagentstateindex"

ADMIN_USERNAME = os.getenv("ADMIN_USERNAME", "admin")
ADMIN_PASSWORD = os.getenv("ADMIN_PASSWORD", "1q2W3e*")

PERMISSION_AGENT = "agentpermissiontest"
PERMISSION_STATE_NAME = "PermissionAgentState"
PERMISSION_EVENT_TYPE = "Aevatar.Application.Grains.Agents.TestAgent.SetAuthorizedUserEvent"

@pytest.fixture(scope="session")
def access_token():
    """get access token"""
    auth_data = {
        "grant_type": "client_credentials",
        "client_id": CLIENT_ID,
        "client_secret": CLIENT_SECRET,
        "scope": "Aevatar"
    }

    response = requests.post(
        f"{AUTH_HOST}/connect/token",
        data=auth_data,
        headers={"Content-Type": "application/x-www-form-urlencoded"},
        verify=False
    )
    assert_status_code(response)
    return response.json()["access_token"]


@pytest.fixture
def api_headers(access_token):
    """generate request header with access token"""
    return {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }


def assert_status_code(response):
    """assert response status code"""
    if response.status_code != 200:
        error_info = f"""
        ====== Abnormal Response ======
        Request URL: {response.request.url}
        Method: {response.request.method}
        Status Code: {response.status_code}
        Response:
        {response.text}
        =====================
        """
        pytest.fail(error_info)


@pytest.fixture
def test_agent(api_headers):
    """Create a test agent and return its ID, with automatic cleanup after testing completes"""
    agent_data = {
        "agentType": TEST_AGENT,
        "name": AGENT_NAME
    }

    # create agent
    response = requests.post(
        f"{API_HOST}/api/agent",
        json=agent_data,
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    agent_id = response.json()["data"]["id"]

    yield agent_id

    # delete agent after test
    response = requests.delete(
        f"{API_HOST}/api/agent/{agent_id}",
        headers=api_headers,
        verify=False
    )
    assert response.status_code == 200


def test_login(access_token):
    """test login"""
    assert len(access_token) > 100


def test_agent_operations(api_headers, test_agent):
    """test agent operation"""
    # get agent
    response = requests.get(
        f"{API_HOST}/api/agent/{test_agent}",
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    assert response.json()["data"]["name"] == AGENT_NAME
    assert response.json()["data"]["businessAgentGrainId"] == f"{TEST_AGENT}/{test_agent.replace('-', '')}"

    # update agent
    update_data = {
        "name": AGENT_NAME_MODIFIED
    }
    response = requests.put(
        f"{API_HOST}/api/agent/{test_agent}",
        json=update_data,
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    assert response.json()["data"]["name"] == AGENT_NAME_MODIFIED
    assert response.json()["data"]["businessAgentGrainId"] == f"{TEST_AGENT}/{test_agent.replace('-', '')}"

    # test my agent list
    time.sleep(3)
    response = requests.get(
        f"{API_HOST}/api/agent/agent-list",
        params={"pageIndex": 0, "pageSize": 100},
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    
    # Add defensive programming to handle potential None response
    response_data = response.json()
    logger.debug(f"Agent list response: {response_data}")
    
    if "data" not in response_data:
        pytest.fail(f"Response does not contain 'data' field: {response_data}")
    
    data = response_data["data"]
    if data is None:
        pytest.fail(f"Response data field is None: {response_data}")
    
    if not isinstance(data, (list, tuple)):
        pytest.fail(f"Response data is not iterable, got type {type(data)}: {data}")
    
    agent_ids = [agent["id"] for agent in data]
    assert test_agent in agent_ids


def test_agent_relationships(api_headers, test_agent):
    """test agent relationships"""
    # create sub agent
    agent_data = {
        "agentType": TEST_AGENT,
        "name": "child Agent"
    }
    response = requests.post(
        f"{API_HOST}/api/agent",
        json=agent_data,
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    sub_agent = response.json()["data"]["id"]

    # add sub agent
    response = requests.post(
        f"{API_HOST}/api/agent/{test_agent}/add-subagent",
        json={"subAgents": [sub_agent]},
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    assert sub_agent in response.json()["data"]["subAgents"]

    # check relationship
    response = requests.get(
        f"{API_HOST}/api/agent/{test_agent}/relationship",
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    assert sub_agent in response.json()["data"]["subAgents"]

    # remove sub agent
    response = requests.post(
        f"{API_HOST}/api/agent/{test_agent}/remove-subagent",
        json={"removedSubAgents": [sub_agent]},
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    assert sub_agent not in response.json()["data"]["subAgents"]

    # check relationship again
    response = requests.get(
        f"{API_HOST}/api/agent/{test_agent}/relationship",
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    assert sub_agent not in response.json()["data"]["subAgents"]

    # delete sub agent
    response = requests.delete(f"{API_HOST}/api/agent/{sub_agent}", headers=api_headers, verify=False)
    assert_status_code(response)


def test_event_operations(api_headers, test_agent):
    """test event operations"""
    # create sub agent
    agent_data = {
        "agentType": TEST_AGENT,
        "name": "child Agent"
    }
    response = requests.post(
        f"{API_HOST}/api/agent",
        json=agent_data,
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    sub_agent = response.json()["data"]["id"]

    # add to group
    response = requests.post(
        f"{API_HOST}/api/agent/{test_agent}/add-subagent",
        json={"subAgents": [sub_agent]},
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    assert sub_agent in response.json()["data"]["subAgents"]

    # query available events
    response = requests.get(
        f"{API_HOST}/api/subscription/events/{test_agent}",
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    
    # Add defensive programming to handle potential None response
    response_data = response.json()
    logger.debug(f"Available events response: {response_data}")
    
    if "data" not in response_data:
        pytest.fail(f"Response does not contain 'data' field: {response_data}")
    
    data = response_data["data"]
    if data is None:
        pytest.fail(f"Response data field is None: {response_data}")
    
    if not isinstance(data, (list, tuple)):
        pytest.fail(f"Response data is not iterable, got type {type(data)}: {data}")
    
    assert any(et["eventType"] == EVENT_TYPE for et in data)
    event = [et for et in data if et["eventType"] == EVENT_TYPE][0]
    
    # Check event properties exist and are iterable
    if "eventProperties" not in event:
        pytest.fail(f"Event does not contain 'eventProperties' field: {event}")
    
    event_properties = event["eventProperties"]
    if event_properties is None:
        pytest.fail(f"Event properties field is None: {event}")
    
    if not isinstance(event_properties, (list, tuple)):
        pytest.fail(f"Event properties is not iterable, got type {type(event_properties)}: {event_properties}")
    
    assert any(property["name"] == EVENT_PARAM for property in event_properties)

    name = "test name"
    # publish event
    event_data = {
        "agentId": test_agent,
        "eventType": EVENT_TYPE,
        "eventProperties": {EVENT_PARAM: name}
    }
    response = requests.post(
        f"{API_HOST}/api/agent/publishEvent",
        json=event_data,
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)

    time.sleep(5)
    # query parent agent state
    response = requests.get(
        f"{API_HOST}/api/query/state",
        params={"stateName": STATE_NAME, "id": test_agent},
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    assert "state" in response.json()["data"]
    assert response.json()["data"]["state"]["name"] == name

    # query sub agent state
    response = requests.get(
        f"{API_HOST}/api/query/state",
        params={"stateName": STATE_NAME, "id": sub_agent},
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    assert "state" in response.json()["data"]
    assert response.json()["data"]["state"]["name"] == name

    # query es
    response = requests.get(
        f"{API_HOST}/api/query/es",
        params={
            "stateName": STATE_NAME,
            "queryString": f"name: {name}",
            "pageSize": 1
        },
        headers=api_headers,
        verify=False
    )

    assert_status_code(response)
    assert response.json()["data"]["totalCount"] > 0

    # test es count endpoint
    response = requests.get(
        f"{API_HOST}/api/query/es/count",
        params={
            "stateName": STATE_NAME,
            "queryString": f"name: {name}",
        },
        headers=api_headers,
        verify=False
    )
    
    assert_status_code(response)
    assert response.json()["data"]["count"] > 0
    
    # verify count matches the query totalCount
    query_response = requests.get(
        f"{API_HOST}/api/query/es",
        params={
            "stateName": STATE_NAME,
            "queryString": f"name: {name}",
            "pageSize": 1
        },
        headers=api_headers,
        verify=False
    )
    assert_status_code(query_response)
    expected_count = query_response.json()["data"]["totalCount"]
    assert response.json()["data"]["count"] == expected_count


def test_query_agent_list(api_headers, test_agent):
    """test query agent list"""
    # query available agent list
    response = requests.get(
        f"{API_HOST}//api/agent/agent-type-info-list",
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    
    # Add defensive programming to handle potential None response
    response_data = response.json()
    logger.debug(f"Agent type list response: {response_data}")
    
    if "data" not in response_data:
        pytest.fail(f"Response does not contain 'data' field: {response_data}")
    
    data = response_data["data"]
    if data is None:
        pytest.fail(f"Response data field is None: {response_data}")
    
    if not isinstance(data, (list, tuple)):
        pytest.fail(f"Response data is not iterable, got type {type(data)}: {data}")
    
    assert any(at["agentType"] == TEST_AGENT for at in data)


def test_agent_service_basic_operations(api_headers):
    """Basic AgentService functionality test"""
    # Test get all agent types
    response = requests.get(
        f"{API_HOST}/api/agent/agent-type-info-list",
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    
    # Test agent list query with pagination
    response = requests.get(
        f"{API_HOST}/api/agent/agent-list",
        params={"pageIndex": 0, "pageSize": 10},
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    
    # Verify response structure
    response_data = response.json()
    assert "data" in response_data
    logger.debug(f"Agent service basic operations test passed")

@pytest.fixture(scope="session")
def admin_access_token():
    """get access token"""
    auth_data = {
        "grant_type": "password",
        "username": ADMIN_USERNAME,
        "password": ADMIN_PASSWORD,
        "scope": "Aevatar",
        "client_id": "AevatarAuthServer"
    }

    response = requests.post(
        f"{AUTH_HOST}/connect/token",
        data=auth_data,
        headers={"Content-Type": "application/x-www-form-urlencoded"},
        verify=False
    )
    assert_status_code(response)
    return response.json()["access_token"]


@pytest.fixture
def api_admin_headers(admin_access_token):
    """generate request header with access token"""
    return {
        "Authorization": f"Bearer {admin_access_token}",
        "Content-Type": "application/json"
    }

def test_permission(api_headers, api_admin_headers):
    """test event operations"""
    # create sub agent
    agent_data = {
        "agentType": PERMISSION_AGENT,
        "name": "permission agent"
    }
    response = requests.post(
        f"{API_HOST}/api/agent",
        json=agent_data,
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    agent_id = response.json()["data"]["id"]

    # add to group
    response = requests.post(
        f"{API_HOST}/api/agent/{agent_id}/add-subagent",
        json={"subAgents": [agent_id]},
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    assert agent_id in response.json()["data"]["subAgents"]

    time.sleep(5)
    
    # publish event
    response = requests.get(
        f"{API_HOST}/api/identity/users/by-username/{ADMIN_USERNAME}",
        headers=api_admin_headers,
        verify=False
    )
    admin_id = response.json()["data"]["id"]
    
    event_data = {
        "agentId": agent_id,
        "eventType": PERMISSION_EVENT_TYPE,
        "eventProperties": {"UserId": admin_id}
    }
    response = requests.post(
        f"{API_HOST}/api/agent/publishEvent",
        json=event_data,
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)

    time.sleep(5)
    
    # query es
    response = requests.get(
        f"{API_HOST}/api/query/es",
        params={
            "stateName": PERMISSION_STATE_NAME,
            "queryString": f"_id:{agent_id}",
            "pageSize": 10
        },
        headers=api_headers,
        verify=False
    )

    assert_status_code(response)
    logger.debug(response.json()["data"])
    assert response.json()["data"]["totalCount"] == 0
    
    response = requests.get(
        f"{API_HOST}/api/query/es",
        params={
            "stateName": PERMISSION_STATE_NAME,
            "queryString": f"_id:{agent_id}",
            "pageSize": 10
        },
        headers=api_admin_headers,
        verify=False
    )
    
    assert_status_code(response)
    logger.debug(response.json()["data"])
    assert response.json()["data"]["totalCount"] > 0
    
    # test es count with permissions - non-admin user should get 0
    response = requests.get(
        f"{API_HOST}/api/query/es/count",
        params={
            "stateName": PERMISSION_STATE_NAME,
            "queryString": f"_id:{agent_id}",
        },
        headers=api_headers,
        verify=False
    )
    
    assert_status_code(response)
    assert response.json()["data"]["count"] == 0
    
    # test es count with permissions - admin user should get count > 0
    response = requests.get(
        f"{API_HOST}/api/query/es/count",
        params={
            "stateName": PERMISSION_STATE_NAME,
            "queryString": f"_id:{agent_id}",
        },
        headers=api_admin_headers,
        verify=False
    )
    
    assert_status_code(response)
    assert response.json()["data"]["count"] > 0

def test_publish_workflow_view(api_headers, api_admin_headers):
    """test publish workflow view"""
    # create workflowView agent
    agent_data = {
        "agentType": WORKFLOW_VIEW_AGENT,
        "name": "workflowViewAgent",
        "properties": {
            "workflowNodeList": [
                {
                    "agentType": "agenttest",
                    "name": "agenttest",
                    "extendedData": {
                        "xPosition": "1",
                        "yPosition": "1"
                    },
                    "nodeId": "9516a447-ca28-457a-a328-f2019863ebaa",
                    "jsonProperties": "{}"
                }
            ],
            "workflowNodeUnitList": [],
            "name": "workflowViewAgent"
        }
    }
    response = requests.post(
        f"{API_HOST}/api/agent",
        json=agent_data,
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    view_agent_id = response.json()["data"]["id"]

    # publish workflow
    response = requests.post(
        f"{API_HOST}/api/workflow-view/{view_agent_id}/publish-workflow",
        json={},
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    test_agent_id = response.json()["data"]["properties"]["workflowNodeList"][0]["agentId"]
    workflow_agent_id = response.json()["data"]["properties"]["workflowCoordinatorGAgentId"]
    logger.debug(f"test_agent_id: {test_agent_id}")
    logger.debug(f"workflow_agent_id: {workflow_agent_id}")
    
    assert test_agent_id != "00000000-0000-0000-0000-000000000000"
    assert workflow_agent_id != "00000000-0000-0000-0000-000000000000"

# Pytest hook to show completion notification with detailed test counts
def pytest_sessionfinish(session, exitstatus):
    """Called after whole test run finished"""
    # Get test results from session
    passed = session.testscollected - session.testsfailed - session.testsskipped
    failed = session.testsfailed
    skipped = session.testsskipped
    total = session.testscollected
    
    print("\n" + "=" * 80)
    print("🏁 REGRESSION_TEST.PY EXECUTION COMPLETED!")
    print("=" * 80)
    print("📊 TEST RESULTS SUMMARY:")
    print(f"   Total Tests:   {total}")
    print(f"   ✅ Passed:     {passed}")
    print(f"   ❌ Failed:     {failed}")
    print(f"   ⏭️  Skipped:    {skipped}")
    print("=" * 80)
    print("📈 Overall Status:", "✅ ALL TESTS PASSED" if exitstatus == 0 else f"❌ {failed} TEST(S) FAILED")
    print("🕐 Completed at:", time.strftime("%Y-%m-%d %H:%M:%S"))
    print("=" * 80)

# Show completion notification when run directly
if __name__ == "__main__":
    print("\n" + "=" * 80)
    print("🏁 REGRESSION_TEST.PY DIRECT EXECUTION COMPLETED!")
    print("💡 Note: For full test suite, run with pytest")
    print("🕐 Completed at:", time.strftime("%Y-%m-%d %H:%M:%S"))
    print("=" * 80)
    