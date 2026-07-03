using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TechStore.Domain.DTOs.Chat;
using TechStore.Service.IService;

namespace TechStoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KnowledgeController : ControllerBase
    {
        private readonly IKnowledgeBaseService _knowledgeBaseService;

        public KnowledgeController(IKnowledgeBaseService knowledgeBaseService)
        {
            _knowledgeBaseService = knowledgeBaseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _knowledgeBaseService.GetAllKnowledgeAsync();
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] KnowledgeUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var item = await _knowledgeBaseService.AddKnowledgeAsync(request);
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _knowledgeBaseService.DeleteKnowledgeAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
