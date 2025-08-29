using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging;
using ErrorLogPrioritization.Api.Controllers;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.AI.Projects;
using Azure.AI.Agents.Persistent;

namespace ErrorLogPrioritization.Api.Services
{
    public static class AzureAiFoundryClient
    {
        public static async Task<string> ProcessFormAssistanceRequest(SendMessageRequest request)
        {
            var content = new
            {
                prompt = request.Message
            };

            var aiPrompt = JsonSerializer.Serialize(content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            try
            {
                Console.WriteLine($"AI Service: Sending prompt to agent...");
                var aiMessages = await RunAgentConversation(aiPrompt);
                Console.WriteLine($"AI Service: Received {aiMessages.Count} messages from agent");
                var aiResponse = aiMessages.LastOrDefault();
                //if (!string.IsNullOrEmpty(aiResponse))
                //{
                //    Console.WriteLine($"AI Service: Agent response: {aiResponse}");
                //    var jsonMatch = Regex.Match(aiResponse, @"\{.*\}", RegexOptions.Singleline);
                //    if (jsonMatch.Success)
                //    {
                //        var jsonResponse = jsonMatch.Value;
                //        var parsedResponse = JsonSerializer.Deserialize<string>(jsonResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                //        if (parsedResponse != null)
                //        {
                //            Console.WriteLine($"AI Service: Successfully parsed response with type: {parsedResponse.Type}");
                //            return parsedResponse;
                //        }
                //    }
                //    else
                //    {
                //        Console.WriteLine("AI Service: No JSON found in response, using fallback");
                //    }
                //}
                return aiResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI Processing Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return null;
            }
        }



        public static async Task<List<string>> RunAgentConversation(string userMessage)
        {
            var endpoint = new Uri("https://hackathon-aue-rxm.services.ai.azure.com/api/projects/105-aue-rxm-project");
            AIProjectClient projectClient = new(endpoint, new DefaultAzureCredential());

            PersistentAgentsClient agentsClient = projectClient.GetPersistentAgentsClient();

            PersistentAgent agent = agentsClient.Administration.GetAgent("asst_WTnoBCs0I8ICox3XyujVKc24");

            PersistentAgentThread thread = agentsClient.Threads.CreateThread();
            Console.WriteLine($"Created thread, ID: {thread.Id}");

            PersistentThreadMessage messageResponse = agentsClient.Messages.CreateMessage(
                thread.Id,
                MessageRole.User,
                userMessage);

            ThreadRun run = agentsClient.Runs.CreateRun(
                thread.Id,
                agent.Id);

            // Poll until the run reaches a terminal status
            do
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                run = agentsClient.Runs.GetRun(thread.Id, run.Id);
            }
            while (run.Status == RunStatus.Queued
                || run.Status == RunStatus.InProgress);
            if (run.Status != RunStatus.Completed)
            {
                throw new InvalidOperationException($"Run failed or was canceled: {run.LastError?.Message}");
            }

            Pageable<PersistentThreadMessage> messages = agentsClient.Messages.GetMessages(
                thread.Id, order: ListSortOrder.Ascending);

            var messageList = new List<string>();
            foreach (PersistentThreadMessage threadMessage in messages)
            {
                Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");
                foreach (Azure.AI.Agents.Persistent.MessageContent contentItem in threadMessage.ContentItems)
                {
                    if (contentItem is MessageTextContent textItem)
                    {
                        Console.Write(textItem.Text);
                        if (threadMessage.Role == MessageRole.Agent)
                        {
                            messageList.Add(textItem.Text);
                        }
                    }
                    else if (contentItem is MessageImageFileContent imageFileItem)
                    {
                        Console.Write($"<image from ID: {imageFileItem.FileId}");
                    }
                    Console.WriteLine();
                }
            }

            return messageList;
        }

    }
}
