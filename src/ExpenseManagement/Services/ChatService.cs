using ExpenseManagement.Models;
using Azure.AI.OpenAI;
using Azure;
using Azure.Identity;
using System.Text.Json;

namespace ExpenseManagement.Services;

public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(string message, List<ChatMessage>? history = null);
}

public class ChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly IExpenseService _expenseService;
    private readonly IUserService _userService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IConfiguration configuration,
        IExpenseService expenseService,
        IUserService userService,
        ICategoryService categoryService,
        ILogger<ChatService> logger)
    {
        _configuration = configuration;
        _expenseService = expenseService;
        _userService = userService;
        _categoryService = categoryService;
        _logger = logger;
    }

    public async Task<ChatResponse> SendMessageAsync(string message, List<ChatMessage>? history = null)
    {
        try
        {
            var endpoint = _configuration["OpenAI:Endpoint"];
            var deploymentName = _configuration["OpenAI:DeploymentName"];
            var managedIdentityClientId = _configuration["ManagedIdentityClientId"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentName))
            {
                return new ChatResponse
                {
                    Success = false,
                    Error = "GenAI resources are not configured. Deploy with -DeployGenAI switch to enable chat functionality.",
                    Response = "I'm sorry, but the AI chat feature requires Azure OpenAI to be deployed. Please redeploy your infrastructure with the -DeployGenAI switch."
                };
            }

            // Create credential with managed identity
            Azure.Core.TokenCredential credential;
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                credential = new ManagedIdentityCredential(managedIdentityClientId);
            }
            else
            {
                _logger.LogInformation("Using DefaultAzureCredential");
                credential = new DefaultAzureCredential();
            }

            var client = new OpenAIClient(new Uri(endpoint), credential);

            // Build conversation messages
            var chatMessages = new List<ChatRequestMessage>();
            
            // System message
            chatMessages.Add(new ChatRequestSystemMessage(@"You are an AI assistant for an Expense Management System. 
You can help users view expenses, create records, and answer questions about their expense data.
Always be helpful and provide clear responses."));

            // Add history
            if (history != null)
            {
                foreach (var msg in history)
                {
                    if (msg.Role == "user")
                    {
                        chatMessages.Add(new ChatRequestUserMessage(msg.Content));
                    }
                    else if (msg.Role == "assistant")
                    {
                        chatMessages.Add(new ChatRequestAssistantMessage(msg.Content));
                    }
                }
            }

            // Add current message
            chatMessages.Add(new ChatRequestUserMessage(message));

            var chatCompletionsOptions = new ChatCompletionsOptions(deploymentName, chatMessages)
            {
                MaxTokens = 800,
                Temperature = 0.7f
            };

            var response = await client.GetChatCompletionsAsync(chatCompletionsOptions);
            var result = response.Value.Choices[0].Message.Content;

            return new ChatResponse
            {
                Success = true,
                Response = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat service");
            return new ChatResponse
            {
                Success = false,
                Error = ex.Message,
                Response = "I apologize, but I encountered an error while processing your request. Please try again."
            };
        }
    }
}
