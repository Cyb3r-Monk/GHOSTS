using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Socializer.Controllers;

[Route("/images")]
[Route("/files")]
public class FilesController : Controller
{
    private readonly ILogger<FilesController> _logger;
    
    public FilesController(ILogger<FilesController> logger)
    {
        _logger = logger;
    }
    
    [HttpPost]
    public async Task<IActionResult> UploadFile([FromForm] FileInputModel model)
    {
        this._logger.LogTrace("{RequestScheme}://{RequestHost}{RequestPath}{RequestQueryString}|{RequestMethod}|{Join}", Request.Scheme, Request.Host, Request.Path, Request.QueryString, Request.Method, string.Join(",", Request.Form));

        var guid = Guid.NewGuid().ToString();
        var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);
        savePath = Path.Combine(savePath, guid);
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);
        
        savePath = Path.Combine(savePath, model.File.FileName);
        
        try
        {
            // Process the file and save it to storage
            // Note: You may want to validate the file size, content type, etc. before saving it
            await using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            return Ok($"/images/{guid}/{model.File.FileName}");
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    public class FileInputModel
    {
        [Required] public IFormFile File { get; set; }
    }
}