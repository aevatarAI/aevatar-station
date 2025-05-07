using Orleans.TestingHost;
using Xunit;

namespace Aevatar.Application.Grains.Tests.Core
{
    public abstract class GodGPTTestBase : IAsyncLifetime
    {
        protected TestCluster Cluster { get; private set; }
        protected IGrainFactory GrainFactory => Cluster.GrainFactory;
        protected IClusterClient ClusterClient => Cluster.Client;

        protected GodGPTTestBase()
        {
            var builder = new TestClusterBuilder();
            builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
            Cluster = builder.Build();
        }

        public virtual async Task InitializeAsync()
        {
            await Cluster.DeployAsync();
        }

        public virtual async Task DisposeAsync()
        {
            await Cluster.DisposeAsync();
        }
    }

    public class TestSiloConfigurations : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            // 配置测试环境
            siloBuilder
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryGrainStorage("LogStorage");
        }
    }
} 