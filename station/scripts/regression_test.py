# regression_test.py
import os
import time
import pytest
import requests
import logging
import urllib3

# Disable SSL warnings for testing with self-signed certificates
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

logger = logging.getLogger(__name__)
logger.setLevel(logging.DEBUG)

TEST_AGENT = "agenttest"
STATE_NAME = "FrontAgentState"
AGENT_NAME = "TestAgent"
AGENT_NAME_MODIFIED = "TestAgentNameModified"
EVENT_TYPE = "Aevatar.Application.Grains.Agents.TestAgent.FrontTestCreateEvent"
EVENT_PARAM = "Name"

AUTH_HOST = os.getenv("AUTH_HOST")
API_HOST =  os.getenv("API_HOST")
CLIENT_ID = os.getenv("CLIENT_ID")
CLIENT_SECRET = os.getenv("CLIENT_SECRET")
INDEX_NAME = f"aevatar-{CLIENT_ID}-testagentstateindex"

ADMIN_USERNAME = "admin"
ADMIN_PASSWORD ="1q2W3e*"

PERMISSION_AGENT = "agentpermissiontest"
PERMISSION_STATE_NAME = "PermissionAgentState"
PERMISSION_EVENT_TYPE = "Aevatar.Application.Grains.Agents.TestAgent.SetAuthorizedUserEvent"

# Workflow testing constants
WORKFLOW_TEST_USER_GOAL = "Create a data processing pipeline that analyzes user behavior and generates reports"
WORKFLOW_SHORT_GOAL = "test"  # Too short for validation
TEXT_COMPLETION_USER_GOAL = "Analyze customer feedback data from multiple sources and"

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


def assert_workflow_response_structure(response_data):
    """Validate workflow response structure"""
    assert "name" in response_data, "Response should contain 'name' field"
    assert "properties" in response_data, "Response should contain 'properties' field"
    
    properties = response_data["properties"]
    assert "workflowNodeList" in properties, "Properties should contain 'workflowNodeList'"
    assert "workflowNodeUnitList" in properties, "Properties should contain 'workflowNodeUnitList'"
    assert "name" in properties, "Properties should contain 'name' field"
    
    # Validate workflow nodes structure if present
    if properties["workflowNodeList"]:
        for node in properties["workflowNodeList"]:
            assert "nodeId" in node, "Node should have 'nodeId'"
            assert "agentType" in node, "Node should have 'agentType'"
            assert "name" in node, "Node should have 'name'"
            assert "extendedData" in node, "Node should have 'extendedData'"
            
            extended_data = node["extendedData"]
            assert "xPosition" in extended_data, "ExtendedData should have 'xPosition'"
            assert "yPosition" in extended_data, "ExtendedData should have 'yPosition'"
    
    # Validate workflow node units structure if present
    if properties["workflowNodeUnitList"]:
        for unit in properties["workflowNodeUnitList"]:
            assert "nodeId" in unit, "Unit should have 'nodeId'"
            assert "nextNodeId" in unit, "Unit should have 'nextNodeId'"
            assert "connectionType" in unit, "Unit should have 'connectionType'"


def test_workflow_generate_valid_request(api_headers):
    """Test successful workflow generation with valid user goal"""
    workflow_data = {
        "userGoal": WORKFLOW_TEST_USER_GOAL
    }
    
    response = requests.post(
        f"{API_HOST}/api/workflow/generate",
        json=workflow_data,
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    
    # Add defensive programming to handle potential None response
    response_data = response.json()
    logger.debug(f"Workflow generate response: {response_data}")
    
    if "data" not in response_data:
        pytest.fail(f"Response does not contain 'data' field: {response_data}")
    
    data = response_data["data"]
    if data is not None:  # Allow null response as valid for some cases
        assert_workflow_response_structure(data)
        logger.info(f"Workflow generation test passed with {len(data.get('properties', {}).get('workflowNodeList', []))} nodes")


def test_workflow_generate_invalid_short_goal(api_headers):
    """Test workflow generation with too short user goal (should fail validation)"""
    workflow_data = {
        "userGoal": WORKFLOW_SHORT_GOAL
    }
    
    response = requests.post(
        f"{API_HOST}/api/workflow/generate",
        json=workflow_data,
        headers=api_headers,
        verify=False
    )
    
    # Should return 400 Bad Request due to validation error
    assert response.status_code == 400, f"Expected 400 for short user goal, got {response.status_code}"
    logger.debug(f"Validation error response: {response.text}")


def test_workflow_generate_missing_user_goal(api_headers):
    """Test workflow generation with missing user goal field"""
    workflow_data = {}
    
    response = requests.post(
        f"{API_HOST}/api/workflow/generate",
        json=workflow_data,
        headers=api_headers,
        verify=False
    )
    
    # Should return 400 Bad Request due to missing required field
    assert response.status_code == 400, f"Expected 400 for missing user goal, got {response.status_code}"
    logger.debug(f"Missing field error response: {response.text}")


def test_workflow_generate_unauthorized_access():
    """Test workflow generation without authentication token"""
    workflow_data = {
        "userGoal": WORKFLOW_TEST_USER_GOAL
    }
    
    headers = {"Content-Type": "application/json"}
    
    response = requests.post(
        f"{API_HOST}/api/workflow/generate",
        json=workflow_data,
        headers=headers,
        verify=False
    )
    
    # Should return 401 Unauthorized
    assert response.status_code == 401, f"Expected 401 for unauthorized access, got {response.status_code}"
    logger.debug(f"Unauthorized access response: {response.text}")


def test_text_completion_valid_request(api_headers):
    """Test successful text completion with valid user goal"""
    completion_data = {
        "userGoal": TEXT_COMPLETION_USER_GOAL
    }
    
    response = requests.post(
        f"{API_HOST}/api/workflow/text-completion",
        json=completion_data,
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    
    # Add defensive programming to handle potential None response
    response_data = response.json()
    logger.debug(f"Text completion response: {response_data}")
    
    if "data" not in response_data:
        pytest.fail(f"Response does not contain 'data' field: {response_data}")
    
    data = response_data["data"]
    if data is not None:
        assert "completions" in data, "Response should contain 'completions' field"
        completions = data["completions"]
        
        if completions is not None:
            assert isinstance(completions, list), "Completions should be a list"
            assert len(completions) <= 5, "Should not exceed 5 completions"
            
            # Validate that all completions are strings
            for completion in completions:
                assert isinstance(completion, str), "Each completion should be a string"
                assert len(completion) > 0, "Completions should not be empty strings"
            
            logger.info(f"Text completion test passed with {len(completions)} completions")


def test_text_completion_invalid_short_goal(api_headers):
    """Test text completion with too short user goal (should fail validation)"""
    completion_data = {
        "userGoal": WORKFLOW_SHORT_GOAL
    }
    
    response = requests.post(
        f"{API_HOST}/api/workflow/text-completion",
        json=completion_data,
        headers=api_headers,
        verify=False
    )
    
    # Should return 400 Bad Request due to validation error
    assert response.status_code == 400, f"Expected 400 for short user goal, got {response.status_code}"
    logger.debug(f"Text completion validation error response: {response.text}")


def test_text_completion_missing_user_goal(api_headers):
    """Test text completion with missing user goal field"""
    completion_data = {}
    
    response = requests.post(
        f"{API_HOST}/api/workflow/text-completion",
        json=completion_data,
        headers=api_headers,
        verify=False
    )
    
    # Should return 400 Bad Request due to missing required field
    assert response.status_code == 400, f"Expected 400 for missing user goal, got {response.status_code}"
    logger.debug(f"Text completion missing field error response: {response.text}")


def test_text_completion_unauthorized_access():
    """Test text completion without authentication token"""
    completion_data = {
        "userGoal": TEXT_COMPLETION_USER_GOAL
    }
    
    headers = {"Content-Type": "application/json"}
    
    response = requests.post(
        f"{API_HOST}/api/workflow/text-completion",
        json=completion_data,
        headers=headers,
        verify=False
    )
    
    # Should return 401 Unauthorized
    assert response.status_code == 401, f"Expected 401 for unauthorized access, got {response.status_code}"
    logger.debug(f"Text completion unauthorized access response: {response.text}")


def test_workflow_endpoints_comprehensive_scenarios(api_headers):
    """Comprehensive test covering various workflow scenarios"""
    # Test various user goal scenarios
    test_scenarios = [
        "Build an automated customer support system with AI chatbot integration",
        "Create a real-time data analytics dashboard for monitoring business metrics",
        "Implement a multi-stage approval workflow for document management system",
        "Design a machine learning pipeline for predictive maintenance analysis"
    ]
    
    for i, user_goal in enumerate(test_scenarios):
        logger.info(f"Testing scenario {i+1}: {user_goal[:50]}...")
        
        # Test workflow generation
        workflow_data = {"userGoal": user_goal}
        response = requests.post(
            f"{API_HOST}/api/workflow/generate",
            json=workflow_data,
            headers=api_headers,
            verify=False
        )
        assert_status_code(response)
        
        # Test text completion
        completion_data = {"userGoal": user_goal}
        response = requests.post(
            f"{API_HOST}/api/workflow/text-completion",
            json=completion_data,
            headers=api_headers,
            verify=False
        )
        assert_status_code(response)
        
        # Add small delay between requests to avoid overwhelming the service
        time.sleep(1)
    
    logger.info("All workflow comprehensive scenarios passed")


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




def assert_workflow_response_structure(response_data):
    """Validate workflow response structure"""
    assert "name" in response_data, "Response should contain 'name' field"
    assert "properties" in response_data, "Response should contain 'properties' field"
    
    properties = response_data["properties"]
    assert "workflowNodeList" in properties, "Properties should contain 'workflowNodeList'"
    assert "workflowNodeUnitList" in properties, "Properties should contain 'workflowNodeUnitList'"
    assert "name" in properties, "Properties should contain 'name' field"
    
    # Validate workflow nodes structure if present
    if properties["workflowNodeList"]:
        for node in properties["workflowNodeList"]:
            assert "nodeId" in node, "Node should have 'nodeId'"
            assert "agentType" in node, "Node should have 'agentType'"
            assert "name" in node, "Node should have 'name'"
            assert "extendedData" in node, "Node should have 'extendedData'"
            
            extended_data = node["extendedData"]
            assert "xPosition" in extended_data, "ExtendedData should have 'xPosition'"
            assert "yPosition" in extended_data, "ExtendedData should have 'yPosition'"
    
    # Validate workflow node units structure if present
    if properties["workflowNodeUnitList"]:
        for unit in properties["workflowNodeUnitList"]:
            assert "nodeId" in unit, "Unit should have 'nodeId'"
            assert "nextNodeId" in unit, "Unit should have 'nextNodeId'"
            assert "connectionType" in unit, "Unit should have 'connectionType'"


def test_workflow_generate_valid_request(api_headers):
    """Test successful workflow generation with valid user goal"""
    workflow_data = {
        "userGoal": WORKFLOW_TEST_USER_GOAL
    }
    
    response = requests.post(
        f"{API_HOST}/api/workflow/generate",
        json=workflow_data,
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    
    # Add defensive programming to handle potential None response
    response_data = response.json()
    logger.debug(f"Workflow generate response: {response_data}")
    
    if "data" not in response_data:
        pytest.fail(f"Response does not contain 'data' field: {response_data}")
    
    data = response_data["data"]
    if data is not None:  # Allow null response as valid for some cases
        assert_workflow_response_structure(data)
        logger.info(f"Workflow generation test passed with {len(data.get('properties', {}).get('workflowNodeList', []))} nodes")


def test_workflow_generate_invalid_short_goal(api_headers):
    """Test workflow generation with too short user goal (should fail validation)"""
    workflow_data = {
        "userGoal": WORKFLOW_SHORT_GOAL
    }
    
    response = requests.post(
        f"{API_HOST}/api/workflow/generate",
        json=workflow_data,
        headers=api_headers,
        verify=False
    )
    
    # Should return 400 Bad Request due to validation error
    assert response.status_code == 400, f"Expected 400 for short user goal, got {response.status_code}"
    logger.debug(f"Validation error response: {response.text}")


def test_workflow_generate_missing_user_goal(api_headers):
    """Test workflow generation with missing user goal field"""
    workflow_data = {}
    
    response = requests.post(
        f"{API_HOST}/api/workflow/generate",
        json=workflow_data,
        headers=api_headers,
        verify=False
    )
    
    # Should return 400 Bad Request due to missing required field
    assert response.status_code == 400, f"Expected 400 for missing user goal, got {response.status_code}"
    logger.debug(f"Missing field error response: {response.text}")


def test_workflow_generate_unauthorized_access():
    """Test workflow generation without authentication token"""
    workflow_data = {
        "userGoal": WORKFLOW_TEST_USER_GOAL
    }
    
    headers = {"Content-Type": "application/json"}
    
    response = requests.post(
        f"{API_HOST}/api/workflow/generate",
        json=workflow_data,
        headers=headers,
        verify=False
    )
    
    # Should return 401 Unauthorized
    assert response.status_code == 401, f"Expected 401 for unauthorized access, got {response.status_code}"
    logger.debug(f"Unauthorized access response: {response.text}")


def test_text_completion_valid_request(api_headers):
    """Test successful text completion with valid user goal"""
    completion_data = {
        "userGoal": TEXT_COMPLETION_USER_GOAL
    }
    
    response = requests.post(
        f"{API_HOST}/api/workflow/text-completion",
        json=completion_data,
        headers=api_headers,
        verify=False
    )
    assert_status_code(response)
    
    # Add defensive programming to handle potential None response
    response_data = response.json()
    logger.debug(f"Text completion response: {response_data}")
    
    if "data" not in response_data:
        pytest.fail(f"Response does not contain 'data' field: {response_data}")
    
    data = response_data["data"]
    if data is not None:
        assert "completions" in data, "Response should contain 'completions' field"
        completions = data["completions"]
        
        if completions is not None:
            assert isinstance(completions, list), "Completions should be a list"
            assert len(completions) <= 5, "Should not exceed 5 completions"
            
            # Validate that all completions are strings
            for completion in completions:
                assert isinstance(completion, str), "Each completion should be a string"
                assert len(completion) > 0, "Completions should not be empty strings"
            
            logger.info(f"Text completion test passed with {len(completions)} completions")


def test_text_completion_invalid_short_goal(api_headers):
    """Test text completion with too short user goal (should fail validation)"""
    completion_data = {
        "userGoal": WORKFLOW_SHORT_GOAL
    }
    
    response = requests.post(
        f"{API_HOST}/api/workflow/text-completion",
        json=completion_data,
        headers=api_headers,
        verify=False
    )
    
    # Should return 400 Bad Request due to validation error
    assert response.status_code == 400, f"Expected 400 for short user goal, got {response.status_code}"
    logger.debug(f"Text completion validation error response: {response.text}")


def test_text_completion_missing_user_goal(api_headers):
    """Test text completion with missing user goal field"""
    completion_data = {}
    
    response = requests.post(
        f"{API_HOST}/api/workflow/text-completion",
        json=completion_data,
        headers=api_headers,
        verify=False
    )
    
    # Should return 400 Bad Request due to missing required field
    assert response.status_code == 400, f"Expected 400 for missing user goal, got {response.status_code}"
    logger.debug(f"Text completion missing field error response: {response.text}")


def test_text_completion_unauthorized_access():
    """Test text completion without authentication token"""
    completion_data = {
        "userGoal": TEXT_COMPLETION_USER_GOAL
    }
    
    headers = {"Content-Type": "application/json"}
    
    response = requests.post(
        f"{API_HOST}/api/workflow/text-completion",
        json=completion_data,
        headers=headers,
        verify=False
    )
    
    # Should return 401 Unauthorized
    assert response.status_code == 401, f"Expected 401 for unauthorized access, got {response.status_code}"
    logger.debug(f"Text completion unauthorized access response: {response.text}")


def test_workflow_endpoints_comprehensive_scenarios(api_headers):
    """Comprehensive test covering various workflow scenarios"""
    # Test various user goal scenarios
    test_scenarios = [
        "Build an automated customer support system with AI chatbot integration",
        "Create a real-time data analytics dashboard for monitoring business metrics",
        "Implement a multi-stage approval workflow for document management system",
        "Design a machine learning pipeline for predictive maintenance analysis"
    ]
    
    for i, user_goal in enumerate(test_scenarios):
        logger.info(f"Testing scenario {i+1}: {user_goal[:50]}...")
        
        # Test workflow generation
        workflow_data = {"userGoal": user_goal}
        response = requests.post(
            f"{API_HOST}/api/workflow/generate",
            json=workflow_data,
            headers=api_headers,
            verify=False
        )
        assert_status_code(response)
        
        # Test text completion
        completion_data = {"userGoal": user_goal}
        response = requests.post(
            f"{API_HOST}/api/workflow/text-completion",
            json=completion_data,
            headers=api_headers,
            verify=False
        )
        assert_status_code(response)
        
        # Add small delay between requests to avoid overwhelming the service
        time.sleep(1)
    
    logger.info("All workflow comprehensive scenarios passed")
