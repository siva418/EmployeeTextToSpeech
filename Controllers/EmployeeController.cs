using Microsoft.AspNetCore.Mvc;
using NamePronunciationTool.Models;
using NamePronunciationTool.ServiceLayer;

namespace NamePronunciation.Controllers
{
    public class EmployeeController : Controller
    {
        IDBOperations _dbOperations = null;
        public EmployeeController(IDBOperations dBOperations)
        {
            _dbOperations = dBOperations;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult EmployeeSearch(string employeeAdEntId)
        {
            EmployeeData empData = _dbOperations.GetEmployeeData(employeeAdEntId);

            if (empData != null)
            {
                return View(empData);
            }
            else
            {
                return View("Error");
            }
        }

        public IActionResult RecordPronunciation()
        {
            return View();
        }
    }
}
