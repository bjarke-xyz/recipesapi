using Microsoft.AspNetCore.Mvc;
using RecipesAPI.API.Features.Healthcheck.BLL;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Healthcheck.Controllers;

public class HealthcheckController : BaseController
{
    private readonly ILogger<HealthcheckController> logger;
    private readonly HealthcheckService healthcheckService;

    public HealthcheckController(ILogger<HealthcheckController> logger, HealthcheckService healthcheckService)
    {
        this.logger = logger;
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

    [HttpGet("test")]
    public ActionResult Test()
    {
        throw new Exception("test!");

    }
}