using Microsoft.AspNetCore.Mvc;
using NamePronunciationTool.Models;
using NamePronunciationTool.ServiceLayer;
using System.Collections.Generic;
using System.Linq;

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

        public IActionResult RecordPronunciation(string employeeAdEntId)
        {
            EmployeeData empData = _dbOperations.GetEmployeeData(employeeAdEntId);
            return View(empData);
        }

        public JsonResult GetEmployeeList(string namePart)
        {
            if (!string.IsNullOrWhiteSpace(namePart))
            {
                List<EmployeeData> empList = _dbOperations.GetEmployeeList();

                var result = empList.Where(e => e.FirstName.ToLower().StartsWith(namePart.Trim().ToLower()) || e.LastName.ToLower().StartsWith(namePart.Trim().ToLower())).ToList();

                return Json(result);
            }
            else
                return Json("");   
        }
    }
}
