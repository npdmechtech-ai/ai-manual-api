using Microsoft.AspNetCore.Mvc;
using AiManual.API.Models;
using AiManual.API.Services;

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
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Question))
                {
                    return BadRequest(new { error = "Question cannot be empty." });
                }

                // ✅ USE CORRECT METHOD NAME
                var answer = await _chatService.GetAnswer(request.Question);

                return Ok(new
                {
                    response = answer
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal Server Error",
                    details = ex.Message
                });
            }
        }
    }
}
