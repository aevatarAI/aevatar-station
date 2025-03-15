using Aevatar.Application.Grains.Agents.Sports.Model;

namespace Aevatar.Application.Grains.Agents.Sports.Common;

public class CypherQueryBuilder
{
    private readonly List<string> _cypherParts = new();
    private readonly Dictionary<string, object> _parameters = new();
    private int _paramIndex;

    public (string Query, Dictionary<string, object> Parameters) Build(
        IEnumerable<Node> nodes, 
        IEnumerable<Relationship> relationships)
    {
        var nodeVariables = new Dictionary<Node, string>();
        var nodeVarsList = new List<string>();
        
        foreach (var node in nodes)
        {
            var varName = $"n{_paramIndex++}";
            nodeVariables[node] = varName;
            nodeVarsList.Add(varName);
        
            AddNodeCypher(node, varName);
        }
        
        if (nodeVarsList.Count > 0)
        {
            _cypherParts.Add($"WITH {string.Join(", ", nodeVarsList)}");
        }
        
        foreach (var rel in relationships)
        {
            if (!nodeVariables.TryGetValue(rel.StartNode, out var startVar) ||
                !nodeVariables.TryGetValue(rel.EndNode, out var endVar))
            {
                throw new ArgumentException("Relationship references unknown nodes");
            }
        
            AddRelationshipCypher(rel, startVar, endVar);
        }

        return (string.Join("\n", _cypherParts), _parameters);
    }

    private void AddNodeCypher(Node node, string varName)
    {
        var matchProp = $"{node.MatchKey}: ${varName}_{node.MatchKey}";
        var setClause = new List<string>();
        
        foreach (var prop in node.Properties)
        {
            _parameters.Add($"{varName}_{prop.Key}", prop.Value);
            if (prop.Key != node.MatchKey)
            {
                setClause.Add($"{varName}.{prop.Key} = ${varName}_{prop.Key}");
            }
        }

        var labels = string.Join(":", node.Labels);
        var cypher = $"MERGE ({varName}:{labels} {{{matchProp}}})";
        if (setClause.Count > 0)
        {
            cypher += $"\nON CREATE SET {string.Join(", ", setClause)}";
        }
        
        _cypherParts.Add(cypher);
    }

    private void AddRelationshipCypher(Relationship rel, string startVar, string endVar)
    {
        var relVar = $"r{_paramIndex++}";
        var props = new List<string>();
    
        foreach (var prop in rel.Properties ?? new Dictionary<string, object>())
        {
            var paramName = $"{relVar}_{prop.Key}";
            _parameters.Add(paramName, prop.Value);
            props.Add($"{prop.Key}: ${paramName}");
        }

        var propsClause = props.Count > 0 ? $" {{{string.Join(", ", props)}}}" : "";
        _cypherParts.Add(
            $"CREATE ({startVar})-[:{rel.Type}{propsClause}]->({endVar})");
    }
}