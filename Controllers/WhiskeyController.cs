using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("whisky")]
public class WhiskyController : Controller
{
    private IWhiskyRepository _whiskyRepository;

    public WhiskyController(IWhiskyRepository whiskyRepository)
    {
        _whiskyRepository = whiskyRepository;
    }

    /// <summary>
    /// Get all whiskies
    /// </summary>
    /// <returns>List of whisky</returns>
    /// <response code="200">Returns the list of whisky</response>
    [HttpGet]
    public IActionResult Index()
    {
        return Ok(_whiskyRepository.GetAll());
    }

    /// <summary>
    /// Get all the whisky regions
    /// </summary>
    /// <returns>List of whisky regions</returns>
    /// <response code="200">Returns the list of whisky regions</response>
    [HttpGet("regions")]
    public IActionResult Regions()
    {
        return Ok(_whiskyRepository.GetAll().Select(p => p.RegionStyle).Distinct().OrderBy(p=>p));
    }
}