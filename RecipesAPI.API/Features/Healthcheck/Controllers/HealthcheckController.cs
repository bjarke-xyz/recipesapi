using Microsoft.AspNetCore.Mvc;
using RecipesAPI.API.Features.Healthcheck.BLL;

namespace RecipesAPI.API.Features.Healthcheck.Controllers;

[Route("{controller}")]
public class HealthcheckController : ControllerBase
{
    private readonly HealthcheckService healthcheckService;

    public HealthcheckController(HealthcheckService healthcheckService)
    {
        this.healthcheckService = healthcheckService;
    }

    [HttpGet("alive")]
    public ActionResult Alive()
    {
        return Ok();
    }

    [HttpGet("ready")]
    public ActionResult Ready()
    {
        if (healthcheckService.IsReady())
        {
            return Ok();
        }
        else
        {
            return StatusCode(500);
        }
    }
}