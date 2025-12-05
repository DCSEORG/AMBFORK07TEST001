using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService? _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IServiceProvider serviceProvider, ILogger<ChatController> logger)
    {
        // Chat service might not be registered if GenAI is not configured
        _chatService = serviceProvider.GetService<IChatService>();
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
    {
        if (_chatService == null)
        {
            return Ok(new ChatResponse
            {
                Success = false,
                Error = "GenAI not configured",
                Response = "The AI chat feature is not available. Please deploy with GenAI resources enabled."
            });
        }

        try
        {
            var response = await _chatService.SendMessageAsync(request.Message, request.History);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return Ok(new ChatResponse
            {
                Success = false,
                Error = ex.Message,
                Response = "I apologize, but I encountered an error. Please try again."
            });
        }
    }
}
