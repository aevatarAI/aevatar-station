using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Aevatar.Application.Grains.Agents.ChatManager.Common
{
    public static class GodPromptHelper
    {
        private static ILogger? _logger;

        // Method to initialize the static logger
        public static void InitializeLogger(ILogger? logger)
        {
            _logger = logger;
        }

        public static string LoadGodPrompt()
        {
            return CommonHelper
                .LoadFileFromCurrentDirectory("./god_prompt.txt")
                .Replace("\r", " ") // Converts standalone carriage returns to Unix-style line endings;
                .Replace("\n", " "); // Converts Windows-style line endings to Unix-style line endings
        }

        public static async Task<string> LoadNewGodPromptAsync()
        {
            try
            {
                // Load the template file content
                string template = CommonHelper.LoadFileFromCurrentDirectory("./god_prompt_template.txt");

                // Fetch core content from GitHub
                // string coreContent = await GithubUtil.FetchCoreContentAsync();
                // string formalTheoryCoreContent = await GithubUtil.FetchFormalTheoryCoreContentAsync();
                string formalTheoryCosmicOntologyContent = await GithubUtil.FetchFormalTheoryCosmicOntologyContentAsync();
                string hyperintelligenceContent = await GithubUtil.FetcHyperintelligenceContentAsync();

                // Ensure the template is successfully loaded
                if (string.IsNullOrWhiteSpace(template))
                {
                    _logger?.LogWarning(
                        "The god_prompt_template.txt file is empty or could not be loaded. Loading the default GodPrompt.");
                    return LoadGodPrompt();
                }

                // Ensure the coreContent is successfully loaded
                if (string.IsNullOrWhiteSpace(formalTheoryCosmicOntologyContent))
                {
                    _logger?.LogWarning(
                        "The core content from GitHub could not be fetched. Loading the default GodPrompt.");
                    return LoadGodPrompt();
                }

                // Ensure the formalTheoryCoreContent is successfully loaded
                if (string.IsNullOrWhiteSpace(formalTheoryCosmicOntologyContent))
                {
                    _logger?.LogWarning(
                        "The formal theory core content from GitHub could not be fetched. Loading the default GodPrompt.");
                    return LoadGodPrompt();
                }

                // Replace placeholders in the template
                string replacedTemplate = template
                    .Replace("{formal_theory_cosmic_ontology}", formalTheoryCosmicOntologyContent ?? string.Empty) // Replace {core}
                    .Replace("{formal_theory_transcendental_hyperintelligence}",
                        hyperintelligenceContent ?? string.Empty); // Replace {formal_theory_core}

                return replacedTemplate
                    .Replace("\r", " ") // Converts standalone carriage returns to Unix-style line endings;
                    .Replace("\n", " "); // Converts Windows-style line endings to Unix-style line endings

            }
            catch (Exception ex)
            {
                // Log the error
                _logger?.LogError(ex, "Error in LoadNewGodPrompt: {ErrorMessage}", ex.Message);

                // Rethrow or return a default value if necessary
                return LoadGodPrompt();
            }
        }
    }
}