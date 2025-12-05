using ExpenseManagement.Models;
using Azure.AI.OpenAI;
using Azure;
using Azure.Identity;
using System.Text.Json;
using OpenAI.Chat;

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

            var client = new AzureOpenAIClient(new Uri(endpoint), credential);
            var chatClient = client.GetChatClient(deploymentName);

            // Build conversation history
            var messages = new List<OpenAI.Chat.ChatMessage>();

            // System message with function calling instructions
            var systemMessage = @"You are an AI assistant for an Expense Management System. You can help users with:
- Viewing expenses (all expenses, by user, by status, by category)
- Getting expense summaries and statistics
- Creating new expenses
- Approving or rejecting expenses
- Viewing users and categories

When users ask to perform actions, use the provided functions to interact with the system.
When displaying lists, format them as markdown lists for better readability.
Always be helpful and provide clear, concise responses.";

            messages.Add(new SystemChatMessage(systemMessage));

            // Add conversation history if provided
            if (history != null)
            {
                foreach (var historyMessage in history)
                {
                    if (historyMessage.Role == "user")
                    {
                        messages.Add(new UserChatMessage(historyMessage.Content));
                    }
                    else if (historyMessage.Role == "assistant")
                    {
                        messages.Add(new AssistantChatMessage(historyMessage.Content));
                    }
                }
            }

            // Add current user message
            messages.Add(new UserChatMessage(message));

            // Define available functions
            var options = new ChatCompletionOptions();
            
            // Function to get all expenses
            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: "get_expenses",
                functionDescription: "Retrieves all expenses from the database",
                functionParameters: BinaryData.FromString("{\"type\":\"object\",\"properties\":{}}")
            ));

            // Function to get expenses by user
            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: "get_expenses_by_user",
                functionDescription: "Retrieves expenses for a specific user",
                functionParameters: BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""userId"": {
                            ""type"": ""integer"",
                            ""description"": ""The ID of the user""
                        }
                    },
                    ""required"": [""userId""]
                }")
            ));

            // Function to get expenses by status
            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: "get_expenses_by_status",
                functionDescription: "Retrieves expenses with a specific status (1=Pending, 2=Approved, 3=Rejected)",
                functionParameters: BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""statusId"": {
                            ""type"": ""integer"",
                            ""description"": ""The status ID (1=Pending, 2=Approved, 3=Rejected)""
                        }
                    },
                    ""required"": [""statusId""]
                }")
            ));

            // Function to create an expense
            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: "create_expense",
                functionDescription: "Creates a new expense record",
                functionParameters: BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""userId"": {
                            ""type"": ""integer"",
                            ""description"": ""The ID of the user creating the expense""
                        },
                        ""categoryId"": {
                            ""type"": ""integer"",
                            ""description"": ""The category ID of the expense""
                        },
                        ""amount"": {
                            ""type"": ""number"",
                            ""description"": ""The amount of the expense""
                        },
                        ""description"": {
                            ""type"": ""string"",
                            ""description"": ""A description of the expense""
                        },
                        ""expenseDate"": {
                            ""type"": ""string"",
                            ""description"": ""The date of the expense in YYYY-MM-DD format""
                        }
                    },
                    ""required"": [""userId"", ""categoryId"", ""amount"", ""description"", ""expenseDate""]
                }")
            ));

            // Function to get users
            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: "get_users",
                functionDescription: "Retrieves all users from the system",
                functionParameters: BinaryData.FromString("{\"type\":\"object\",\"properties\":{}}")
            ));

            // Function to get categories
            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: "get_categories",
                functionDescription: "Retrieves all expense categories",
                functionParameters: BinaryData.FromString("{\"type\":\"object\",\"properties\":{}}")
            ));

            // Conversation loop for function calling
            bool requiresAction = true;
            string finalResponse = "";
            int maxIterations = 5;
            int iteration = 0;

            while (requiresAction && iteration < maxIterations)
            {
                iteration++;
                
                var response = await chatClient.CompleteChatAsync(messages, options);
                var choice = response.Value.Choices[0];

                if (choice.FinishReason == ChatFinishReason.ToolCalls)
                {
                    // AI wants to call a function
                    messages.Add(new AssistantChatMessage(choice));

                    foreach (var toolCall in choice.ToolCalls)
                    {
                        var functionCall = toolCall as ChatToolCall;
                        if (functionCall == null) continue;

                        _logger.LogInformation("AI calling function: {FunctionName} with args: {Arguments}", 
                            functionCall.FunctionName, functionCall.FunctionArguments);

                        var functionResult = await ExecuteFunctionAsync(functionCall.FunctionName, functionCall.FunctionArguments);
                        
                        messages.Add(new ToolChatMessage(toolCall.Id, functionResult));
                    }
                }
                else
                {
                    // AI has finished
                    requiresAction = false;
                    finalResponse = choice.Message.Content[0].Text;
                }
            }

            return new ChatResponse
            {
                Success = true,
                Response = finalResponse
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

    private async Task<string> ExecuteFunctionAsync(string functionName, BinaryData arguments)
    {
        try
        {
            var argsJson = arguments.ToString();
            _logger.LogInformation("Executing function {FunctionName} with arguments: {Arguments}", functionName, argsJson);

            switch (functionName)
            {
                case "get_expenses":
                    var expenses = await _expenseService.GetAllExpensesAsync();
                    return JsonSerializer.Serialize(expenses);

                case "get_expenses_by_user":
                    var userArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argsJson);
                    var userId = userArgs?["userId"].GetInt32() ?? 0;
                    var userExpenses = await _expenseService.GetExpensesByUserIdAsync(userId);
                    return JsonSerializer.Serialize(userExpenses);

                case "get_expenses_by_status":
                    var statusArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argsJson);
                    var statusId = statusArgs?["statusId"].GetInt32() ?? 0;
                    var statusExpenses = await _expenseService.GetExpensesByStatusAsync(statusId);
                    return JsonSerializer.Serialize(statusExpenses);

                case "create_expense":
                    var createArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argsJson);
                    var newExpense = new ExpenseCreate
                    {
                        UserId = createArgs?["userId"].GetInt32() ?? 0,
                        CategoryId = createArgs?["categoryId"].GetInt32() ?? 0,
                        Amount = createArgs?["amount"].GetDecimal() ?? 0,
                        Description = createArgs?["description"].GetString() ?? "",
                        ExpenseDate = DateTime.Parse(createArgs?["expenseDate"].GetString() ?? DateTime.Now.ToString("yyyy-MM-dd"))
                    };
                    var expenseId = await _expenseService.CreateExpenseAsync(newExpense);
                    return JsonSerializer.Serialize(new { success = expenseId > 0, expenseId });

                case "get_users":
                    var users = await _userService.GetAllUsersAsync();
                    return JsonSerializer.Serialize(users);

                case "get_categories":
                    var categories = await _categoryService.GetAllCategoriesAsync();
                    return JsonSerializer.Serialize(categories);

                default:
                    return JsonSerializer.Serialize(new { error = "Unknown function" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
