namespace Aevatar.AI.Common;

internal static class ContestConstant
{
    public static readonly string KnowledgePrompt = @"
            Please use this information to answer the question:
            {{#with (SearchPlugin-GetTextSearchResults prompt)}}
              {{#each this}}
                Name: {{Name}}
                Value: {{Value}}
                Link: {{Link}}
                -----------------
              {{/each}}
            {{/with}}

            Include citations to the relevant information where it is referenced in the response.

            Question: {{prompt}}
            ";
}