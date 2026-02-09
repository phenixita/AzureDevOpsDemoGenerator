using Microsoft.AspNetCore.Mvc;
using VstsDemoBuilder.Services;
using VstsDemoBuilder.ServiceInterfaces;

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

        [HttpGet]
        [Route("AllTemplates")]
        public IActionResult GetTemplates()
        {
            ProjectService.TrackFeature("api/templates/Alltemplates");
            var templates = templateService.GetAllTemplates();
            return Ok(templates);
        }

        [HttpGet]
        [Route("TemplatesByTags")]
        public IActionResult templatesbyTags(string Tags)
        {
            ProjectService.TrackFeature("api/templates/TemplateByTags");
            var templates = templateService.GetTemplatesByTags(Tags);
            return Ok(templates);
        }       


    }
}
