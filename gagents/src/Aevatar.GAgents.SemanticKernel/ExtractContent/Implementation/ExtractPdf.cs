using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Brain;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;

namespace Aevatar.GAgents.SemanticKernel.ExtractContent;

public class ExtractPdf(IChatCompletionService chatCompletionService) : IExtractContent
{
    public async Task<List<ExtractResult>> Extract(BrainContent brainContent, CancellationToken cancellationToken)
    {
        var sections = LoadTextAndImages(brainContent.Content, cancellationToken);
        var result = new List<ExtractResult>();
        foreach (var section in sections)
        {
            var sectionName = $"{brainContent.Name}-{section.PageNumber}";
            if (section.Text != null)
            {
                result.Add(new ExtractResult()
                    { Name = sectionName, Content = section.Text });
                continue;
            }

            var textFromImage = await ConvertImageToTextWithRetryAsync(
                chatCompletionService,
                section.Image!.Value,
                cancellationToken).ConfigureAwait(false);
            result.Add(new ExtractResult(){Name = sectionName, Content = textFromImage});
        }

        return result;
    }

    private static IEnumerable<RawContent> LoadTextAndImages(byte[] fileBytes, CancellationToken cancellationToken)
    {
        using (var document = PdfDocument.Open(fileBytes))
        {
            foreach (var page in document.GetPages())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                foreach (var image in page.GetImages())
                {
                    if (image.TryGetPng(out var png))
                    {
                        yield return new RawContent { Image = png, PageNumber = page.Number };
                    }
                    else
                    {
                        Console.WriteLine($"Unsupported image format on page {page.Number}");
                    }
                }

                var blocks = DefaultPageSegmenter.Instance.GetBlocks(page.GetWords());
                foreach (var block in blocks)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    yield return new RawContent { Text = block.Text, PageNumber = page.Number };
                }
            }
        }
    }

    /// <summary>
    /// Add a simple retry mechanism to image to text.
    /// </summary>
    /// <param name="chatCompletionService">The chat completion service to use for generating text from images.</param>
    /// <param name="imageBytes">The image to generate the text for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The generated text.</returns>
    private static async Task<string> ConvertImageToTextWithRetryAsync(
        IChatCompletionService chatCompletionService,
        ReadOnlyMemory<byte> imageBytes,
        CancellationToken cancellationToken)
    {
        var tries = 0;

        while (true)
        {
            try
            {
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage([
                    new TextContent("What’s in this image?"),
                    new ImageContent(imageBytes, "image/png"),
                ]);
                var result = await chatCompletionService
                    .GetChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return string.Join("\n", result.Select(x => x.Content));
            }
            catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                tries++;

                if (tries < 3)
                {
                    Console.WriteLine($"Failed to generate text from image. Error: {ex}");
                    Console.WriteLine("Retrying text to image conversion...");
                    await Task.Delay(10_000, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Private model for returning the content items from a PDF file.
    /// </summary>
    private sealed class RawContent
    {
        public string? Text { get; init; }

        public ReadOnlyMemory<byte>? Image { get; init; }

        public int PageNumber { get; init; }
    }
}