Console.WriteLine("Testing ZooKeeper connection...");

try
{
    var zk = new org.apache.zookeeper.ZooKeeper("localhost:2181", 30000, new TestWatcher());
    
    // Wait for connection
    var timeout = DateTime.UtcNow.AddSeconds(10);
    while (zk.getState() != org.apache.zookeeper.ZooKeeper.States.CONNECTED)
    {
        if (DateTime.UtcNow > timeout)
        {
            throw new TimeoutException("Failed to connect to ZooKeeper");
        }
        await Task.Delay(100);
    }
    
    Console.WriteLine("✓ ZooKeeper connection successful");
    Console.WriteLine($"ZooKeeper state: {zk.getState()}");
    
    // List root nodes
    var children = await zk.getChildrenAsync("/");
    Console.WriteLine($"Root nodes: {string.Join(", ", children.Children)}");
    
    // Check Orleans nodes
    if (children.Children.Contains("orleans"))
    {
        var orleansChildren = await zk.getChildrenAsync("/orleans");
        Console.WriteLine($"Orleans nodes: {string.Join(", ", orleansChildren.Children)}");
        
        if (orleansChildren.Children.Contains("clusters"))
        {
            var clusters = await zk.getChildrenAsync("/orleans/clusters");
            Console.WriteLine($"Clusters: {string.Join(", ", clusters.Children)}");
        }
    }
    else
    {
        Console.WriteLine("No Orleans nodes found - this is expected for a fresh ZooKeeper");
    }
    
    await zk.closeAsync();
    Console.WriteLine("✓ ZooKeeper test completed successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Test failed: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

public class TestWatcher : org.apache.zookeeper.Watcher
{
    public override Task process(org.apache.zookeeper.WatchedEvent @event)
    {
        Console.WriteLine($"ZooKeeper event: {@event.getState()} {@event.getPath()}");
        return Task.CompletedTask;
    }
} 