namespace Orleans.TestKit;

public class TestGrainContextAccessor : IGrainContextAccessor
{
    public IGrainContext GrainContext { get; }

    public TestGrainContextAccessor(IGrainContext grainContext) => GrainContext = grainContext;
}
