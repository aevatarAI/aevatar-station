using System.Reflection;
using NJsonSchema.Generation;

namespace Aevatar.Schema;

public interface IDropDownSchemaProcess
{
    string OptionName { get; }
    void ProcessSchema(SchemaProcessorContext processorContext, PropertyInfo propertyInfo, DynamicDropDownContext? dropDownContext);
}

public abstract class DropDownSchemaProcessBase : IDropDownSchemaProcess
{
    public abstract string OptionName { get; }
    public abstract void ProcessSchema(SchemaProcessorContext processorContext, PropertyInfo propertyInfo, DynamicDropDownContext? dropDownContext);
}