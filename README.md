# *Aevatar Station*

*Your all-in-one platform for creating, managing, and deploying AI agents.*

---
## 🚀 **Introduction**

**Aevatar Station** is a cutting-edge developer platform designed to simplify the creation, management, and deployment of intelligent AI agents. With a focus on flexibility, scalability, and ease of use, Aevatar Station empowers developers and organizations to harness the power of AI in a streamlined and efficient way.

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- MongoDB
- Elasticsearch
- Redis

## Configuration

1. Update the `appsettings.json` file in the Silo project with your specific configurations (e.g., connection strings, Orleans clustering configurations).

    ```json
    {
      "ConnectionStrings": {
        "Default": "mongodb://localhost:27017/Aevatar"
      },
      "Orleans": {
        "ClusterId": "AevatarSiloCluster",
        "ServiceId": "AevatarBasicService",
        "AdvertisedIP": "127.0.0.1",
        "GatewayPort": 20001,
        "SiloPort": 10001,
        "MongoDBClient": "mongodb://localhost:27017/?maxPoolSize=555",
        "DataBase": "AevatarDb",
        "DashboardUserName": "admin",
        "DashboardPassword": "123456",
        "DashboardCounterUpdateIntervalMs": 1000,
        "DashboardPort": 8080,
        "EventStoreConnection": "ConnectTo=tcp://localhost:1113; HeartBeatTimeout=500",
        "ClusterDbConnection": "127.0.0.1:6379",
        "ClusterDbNumber": 0,
        "GrainStorageDbConnection": "127.0.0.1:6379",
        "GrainStorageDbNumber": 0
      }
    }
    ```

2. Update the `appsettings.json` file in the HttpApi.Host project with your specific configurations (e.g., connection strings, Orleans clustering configurations).

    ```json
    {
      "ConnectionStrings": {
        "Default": "mongodb://localhost:27017/Aevatar"
      },
      "Orleans": {
        "ClusterId": "AevatarSiloCluster",
        "ServiceId": "AevatarBasicService",
        "MongoDBClient": "mongodb://localhost:27017/?maxPoolSize=555",
        "DataBase": "AevatarDb"
      }
    }
    ```

### Running the Application

1. Go to the `src` folder
    ```shell
    cd src
    ```
2. Run the `Aevatar.DbMigrator` project to create the initial database from `src`.
    ```shell
    cd Aevatar.DbMigrator
    dotnet run
    ```
3. Run the `Aevatar.AuthServer` project to create the initial database from `src`.
    ```shell
    cd Aevatar.AuthServer
    dotnet run
    ```
4. Run the `Aevatar.Silo` project to start the Orleans Silo from `src`.
    ```shell
    cd ../Aevatar.Silo
    dotnet run
    ```
5. Run the `Aevatar.HttpApi.Host` project to start the API from `src`.
    ```shell
    cd ../Aevatar.HttpApi.Host
    dotnet run
    ```
## Contributing

If you encounter a bug or have a feature request, please use the [Issue Tracker](https://github.com/AISmartProject/aevatar-station/issues/new). The project is also open to contributions, so feel free to fork the project and open pull requests.

## License

Distributed under the MIT License. See [License](LICENSE) for more information.