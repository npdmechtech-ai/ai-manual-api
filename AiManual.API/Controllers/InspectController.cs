using Microsoft.AspNetCore.Mvc;
using AiManual.API.Services;

namespace AiManual.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InspectController : ControllerBase
    {
        private readonly InspectService _inspectService;

        public InspectController(InspectService inspectService)
        {
            _inspectService = inspectService;
        }

        [HttpPost]
        public async Task<IActionResult> Inspect(IFormFile image)
        {
            if (image == null)
                return BadRequest("Image is required");

            var result = await _inspectService.AnalyzeImage(image);

            // ✅ RETURN CLEAN JSON
            return Content(result, "application/json");
        }
    }
}