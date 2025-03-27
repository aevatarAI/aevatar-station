# tests/test_agent_api.py
import pytest
import requests

# 环境配置
AUTH_HOST = "http://127.0.0.1:8082"
API_HOST = "http://localhost:8001"

TEST_AGENT = "testagentwithconfiguration"
STATE_NAME = "TestAgentState"
AGENT_NAME = "TestAgent"
AGENT_NAME_MODIFIED = "TestAgentNameModified"
INDEX_NAME = "aevatar-aevatar-testagentstateindex"
EVENT_TYPE = "Aevatar.Application.Grains.Agents.TestAgent.SetNumberGEvent"
EVENT_PARAM = "Number"


# 从环境变量获取凭证
CLIENT_ID = os.getenv("CLIENT_ID")
CLIENT_SECRET = os.getenv("CLIENT_SECRET")


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
        headers={"Content-Type": "application/x-www-form-urlencoded"}
    )
    assert response.status_code == 200
    print(response.json()["access_token"])
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
        "name": AGENT_NAME,
        "properties": {
            "Name": AGENT_NAME
        }
    }

    # create agent
    response = requests.post(
        f"{API_HOST}/api/agent",
        json=agent_data,
        headers=api_headers
    )
    assert_status_code(response)
    agent_id = response.json()["data"]["id"]

    print("agent_id", agent_id)
    yield agent_id

    # delete agent after test
    response = requests.delete(
        f"{API_HOST}/api/agent/{agent_id}",
        headers=api_headers
    )
    assert response.status_code == 200


def test_login(access_token):
    """test login"""
    assert len(access_token) > 100


# def test_agent_crud(api_headers, test_agent):
#     """test agent CRUD"""
#     # get agent
#     response = requests.get(
#         f"{API_HOST}/api/agent/{test_agent}",
#         headers=api_headers
#     )
#     assert_status_code(response)
#     assert response.json()["data"]["name"] == AGENT_NAME
#
#     # update agent
#     update_data = {
#         "name": AGENT_NAME_MODIFIED,
#         "properties": {"Name": AGENT_NAME_MODIFIED}
#     }
#     response = requests.put(
#         f"{API_HOST}/api/agent/{test_agent}",
#         json=update_data,
#         headers=api_headers
#     )
#     assert_status_code(response)
#     assert response.json()["data"]["name"] == AGENT_NAME_MODIFIED
#
#     time.sleep(2)
#     # 验证列表
#     response = requests.get(
#         f"{API_HOST}/api/agent/agent-list",
#         params={"pageIndex": 0, "pageSize": 100},
#         headers=api_headers
#     )
#     assert_status_code(response)
#     agent_ids = [agent["id"] for agent in response.json()["data"]]
#     assert test_agent in agent_ids
#
#
# def test_agent_relationships(api_headers, test_agent):
#     """test agent relationships"""
#     # create sub agent
#     response = requests.post(
#         f"{API_HOST}/api/agent",
#         json={"agentType": TEST_AGENT, "name": "child Agent"},
#         headers=api_headers
#     )
#     assert_status_code(response)
#     sub_agent = response.json()["data"]["id"]
#
#     # add sub agent
#     response = requests.post(
#         f"{API_HOST}/api/agent/{test_agent}/add-subagent",
#         json={"subAgents": [sub_agent]},
#         headers=api_headers
#     )
#     assert_status_code(response)
#     assert sub_agent in response.json()["data"]["subAgents"]
#
#     # check relationship
#     response = requests.get(
#         f"{API_HOST}/api/agent/{test_agent}/relationship",
#         headers=api_headers
#     )
#     assert_status_code(response)
#     assert sub_agent in response.json()["data"]["subAgents"]
#
#     # remove sub agent
#     response = requests.post(
#         f"{API_HOST}/api/agent/{test_agent}/remove-subagent",
#         json={"removedSubAgents": [sub_agent]},
#         headers=api_headers
#     )
#     assert_status_code(response)
#     assert sub_agent not in response.json()["data"]["subAgents"]
#
#     # check relationship again
#     response = requests.get(
#         f"{API_HOST}/api/agent/{test_agent}/relationship",
#         headers=api_headers
#     )
#     assert_status_code(response)
#     assert sub_agent not in response.json()["data"]["subAgents"]
#
#     # delete sub agent
#     response = requests.delete(f"{API_HOST}/api/agent/{sub_agent}", headers=api_headers)
#     assert_status_code(response)
#
#
# def test_event_operations(api_headers, test_agent):
#     """test event operations"""
#     # create sub agent
#     response = requests.post(
#         f"{API_HOST}/api/agent",
#         json={"agentType": TEST_AGENT, "name": "child Agent"},
#         headers=api_headers
#     )
#     assert_status_code(response)
#     sub_agent = response.json()["data"]["id"]
#
#     # add to group
#     response = requests.post(
#         f"{API_HOST}/api/agent/{test_agent}/add-subagent",
#         json={"subAgents": [sub_agent]},
#         headers=api_headers
#     )
#     assert_status_code(response)
#     assert sub_agent in response.json()["data"]["subAgents"]
#
#     # query available events
#     response = requests.get(
#         f"{API_HOST}/api/subscription/events/{test_agent}",
#         headers=api_headers
#     )
#     assert_status_code(response)
#     assert any(et["eventType"] == EVENT_TYPE for et in response.json()["data"])
#     event = [et for et in response.json()["data"] if et["eventType"] == EVENT_TYPE][0]
#     assert any(property["name"] == EVENT_PARAM for property in event["eventProperties"])
#
#     number = 10
#     # publish event
#     event_data = {
#         "agentId": test_agent,
#         "eventType": EVENT_TYPE,
#         "eventProperties": {EVENT_PARAM: number}
#     }
#     response = requests.post(
#         f"{API_HOST}/api/agent/publishEvent",
#         json=event_data,
#         headers=api_headers
#     )
#     assert_status_code(response)
#
#     time.sleep(2)
#     # query parent agent state
#     response = requests.get(
#         f"{API_HOST}/api/query/state",
#         params={"stateName": STATE_NAME, "id": test_agent},
#         headers=api_headers
#     )
#     assert_status_code(response)
#     assert "state" in response.json()["data"]
#     assert response.json()["data"]["state"]["number"] == number
#
#     # query sub agent state
#     response = requests.get(
#         f"{API_HOST}/api/query/state",
#         params={"stateName": STATE_NAME, "id": sub_agent},
#         headers=api_headers
#     )
#     assert_status_code(response)
#     assert "state" in response.json()["data"]
#     assert response.json()["data"]["state"]["number"] == number
#
#
# def test_query_operations(api_headers, test_agent):
#     """test query operations"""
#     time.sleep(2)
#     # query state
#     response = requests.get(
#         f"{API_HOST}/api/query/state",
#         params={"stateName": STATE_NAME, "id": test_agent},
#         headers=api_headers
#     )
#     assert_status_code(response)
#     assert "state" in response.json()["data"]
#     assert response.json()["data"]["state"]["name"] == AGENT_NAME
#
#     # query es
#     response = requests.get(
#         f"{API_HOST}/api/query/es",
#         params={
#             "index": INDEX_NAME,
#             "queryString": f"name: {AGENT_NAME}",
#             "pageSize": 1
#         },
#         headers=api_headers
#     )
#     assert_status_code(response)
#     assert response.json()["data"]["totalCount"] > 0
#
#
# def test_query_agent_list(api_headers, test_agent):
#     """test query agent list"""
#     # query available agent list
#     response = requests.get(
#         f"{API_HOST}//api/agent/agent-type-info-list",
#         headers=api_headers
#     )
#     assert_status_code(response)
#     assert any(at["agentType"] == TEST_AGENT for at in response.json()["data"])

# def test_demo():
#     """demo test"""
#     assert False
