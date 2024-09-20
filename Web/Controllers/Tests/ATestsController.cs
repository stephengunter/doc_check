using ApplicationCore.DataAccess;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers.Tests;

public class ATestsController : BaseTestController
{
   private readonly DefaultContext _defaultContext;
   public ATestsController(DefaultContext defaultContext)
   {
      _defaultContext = defaultContext;
   }
   [HttpGet]
   public async Task<ActionResult> Index()
   {
      return Ok();
   }


   [HttpGet("ex")]
   public ActionResult Ex()
   {
      throw new Exception("Test Throw Exception");
   }
}
