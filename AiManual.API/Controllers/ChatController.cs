using Microsoft.AspNetCore.Mvc;
using AiManual.API.Models;
using AiManual.API.Services; // 🔥 IMPORTANT

namespace AiManual.API.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;

        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            var response = await _chatService.GetAnswer(request.Question);
            return Ok(response);
        }
    }
}