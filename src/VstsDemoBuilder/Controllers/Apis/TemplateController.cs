using Microsoft.AspNetCore.Mvc;
using VstsDemoBuilder.ServiceInterfaces;
using VstsDemoBuilder.Services;

namespace VstsDemoBuilder.Controllers.Apis
{
    [ApiController]
    [Route("api/templates")]
    public class TemplateController : ControllerBase
    {
        private readonly ITemplateService templateService;

        public TemplateController(ITemplateService templateService)
        {
            this.templateService = templateService;
        }

        [HttpGet("AllTemplates")]
        public IActionResult GetTemplates()
        {
            ProjectService.TrackFeature("api/templates/Alltemplates");
            var templates = templateService.GetAllTemplates();
            return Ok(templates);
        }

        [HttpGet("TemplatesByTags")]
        public IActionResult TemplatesByTags(string tags)
        {
            ProjectService.TrackFeature("api/templates/TemplateByTags");
            var templates = templateService.GetTemplatesByTags(tags);
            return Ok(templates);
        }
    }
}
