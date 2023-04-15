using Microsoft.AspNetCore.Mvc;
using RecipesAPI.API.Features.Healthcheck.BLL;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Healthcheck.Controllers;

public class HealthcheckController : BaseController
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