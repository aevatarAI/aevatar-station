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
    agent_ids = [agent["id"] for agent in response.json()["data"]]
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
    assert any(et["eventType"] == EVENT_TYPE for et in response.json()["data"])
    event = [et for et in response.json()["data"] if et["eventType"] == EVENT_TYPE][0]
    assert any(property["name"] == EVENT_PARAM for property in event["eventProperties"])

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
    assert any(at["agentType"] == TEST_AGENT for at in response.json()["data"])


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


def test_k8s_deployment_update(api_admin_headers):
    """test kubernetes deployment update functionality - comprehensive test for k8s operations"""
    # Test data
    test_client_id = f"test-k8s-{int(time.time())}"
    test_cors_urls = "http://localhost:3000,https://example.com"
    copy_client_id = f"copy-k8s-{int(time.time())}"
    
    try:
        # 1. Create Host
        logger.info(f"Creating k8s host for client: {test_client_id}")
        response = requests.post(
            f"{API_HOST}/api/users/CreateHost",
            params={"clientId": test_client_id, "corsUrls": test_cors_urls},
            headers=api_admin_headers,
            verify=False
        )
        assert_status_code(response)
        logger.info("K8s host created successfully")
        
        # Wait for deployment to be ready
        time.sleep(30)
        
        # 2. Test Docker Image Updates for all host types
        host_types = [
            ("Silo", "test-silo-image:v1.0"),
            ("Client", "test-client-image:v1.0"), 
            ("WebHook", "test-webhook-image:v1.0")
        ]
        
        for host_type, image_name in host_types:
            logger.info(f"Updating docker image for {host_type} host type")
            response = requests.post(
                f"{API_HOST}/api/users/updateDockerImageByAdmin",
                params={
                    "hostId": test_client_id,
                    "hostType": host_type,
                    "imageName": image_name
                },
                headers=api_admin_headers,
                verify=False
            )
            assert_status_code(response)
            logger.info(f"Docker image updated successfully for {host_type}")
            time.sleep(15)  # Wait for update to complete
        
        # 3. Copy Host Operation
        logger.info(f"Testing host copy from {test_client_id} to {copy_client_id}")
        response = requests.post(
            f"{API_HOST}/api/users/CopyHost",
            params={"sourceClientId": test_client_id, "newClientId": copy_client_id},
            headers=api_admin_headers,
            verify=False
        )
        assert_status_code(response)
        logger.info("Host copy operation completed successfully")
        
        # Wait for copy operation to complete
        time.sleep(30)
        
        # 4. Test Host Logs Retrieval
        for host_type, _ in host_types:
            logger.info(f"Testing log retrieval for {host_type}")
            response = requests.get(
                f"{API_HOST}/api/host/log",
                params={
                    "appId": test_client_id,
                    "hostType": host_type,
                    "offset": 0
                },
                headers=api_admin_headers,
                verify=False
            )
            assert_status_code(response)
            logs = response.json().get("data", [])
            logger.info(f"Retrieved {len(logs)} log entries for {host_type}")
            
            # Verify log structure if logs exist
            if logs and isinstance(logs, list) and len(logs) > 0:
                first_log = logs[0]
                assert isinstance(first_log, dict), f"Log entry should be a dictionary for {host_type}"
                logger.info(f"Log structure validated for {host_type}")
        
        # 5. Test batch image updates on copied host
        logger.info("Testing batch image updates on copied host")
        for host_type, _ in host_types[:2]:  # Test Silo and Client only for copied host
            updated_image = f"updated-{host_type.lower()}-image:v2.0"
            response = requests.post(
                f"{API_HOST}/api/users/updateDockerImageByAdmin",
                params={
                    "hostId": copy_client_id,
                    "hostType": host_type,
                    "imageName": updated_image
                },
                headers=api_admin_headers,
                verify=False
            )
            assert_status_code(response)
            logger.info(f"Batch update successful for {host_type} on copied host")
            time.sleep(10)
        
        logger.info("All K8s deployment update tests completed successfully")
        
    finally:
        # Cleanup: Destroy both hosts
        cleanup_hosts = [test_client_id, copy_client_id]
        
        for host_id in cleanup_hosts:
            try:
                logger.info(f"Destroying k8s host: {host_id}")
                response = requests.post(
                    f"{API_HOST}/api/users/destroyHost",
                    params={"clientId": host_id},
                    headers=api_admin_headers,
                    verify=False
                )
                if response.status_code == 200:
                    logger.info(f"K8s host {host_id} destroyed successfully")
                else:
                    logger.warning(f"Failed to destroy k8s host {host_id}: {response.text}")
                time.sleep(5)  # Delay between destroys
            except Exception as e:
                logger.warning(f"Error destroying k8s host {host_id}: {e}")


if __name__ == "__main__":
    # Run k8s deployment update test specifically
    pytest.main([
        __file__ + "::test_k8s_deployment_update",
        "-v", "-s"
    ])