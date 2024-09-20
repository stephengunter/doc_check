using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Authorization;

namespace Web.Controllers;

[ApiController]
[Route("[controller]")]
public abstract class BaseController : ControllerBase
{
   protected string RemoteIpAddress => HttpContext.Connection.RemoteIpAddress is null ? "" : HttpContext.Connection.RemoteIpAddress.ToString();
   
   

   protected string TempPath(IWebHostEnvironment environment)
     => Path.Combine(environment.WebRootPath, "temp");

   protected string GetTempPath(IWebHostEnvironment environment, string folder)
     => Path.Combine(TempPath(environment), folder);
   protected void AddErrors(Dictionary<string, string> errors)
   {
      if (errors.Count > 0)
      {
         foreach (var kvp in errors)
         {
            ModelState.AddModelError(kvp.Key, kvp.Value);
         }
      }
   }
}


[EnableCors("Api")]
[Route("api/[controller]")]
[Authorize]
public abstract class BaseApiController : BaseController
{
   
}

[EnableCors("Global")]
[Route("tests/[controller]")]
public abstract class BaseTestController : BaseController
{

}



