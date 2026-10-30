using Microsoft.AspNetCore.Mvc;
using EduConnect.Repositories;

namespace EduConnect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClassesController : ControllerBase
    {
        private readonly ClassRepository _repo;

        public ClassesController(ClassRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public IActionResult GetClasses()
        {
            var classes = _repo.GetAllClasses();
            return Ok(classes);
        }
    }
}