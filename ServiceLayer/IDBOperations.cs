using NamePronunciationTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NamePronunciationTool.ServiceLayer
{
    public interface IDBOperations
    {
        EmployeeData GetEmployeeData(string employeeId);
        string GetEmployeeNamePhoneticData(string employeeAdEntId);
        bool SaveSpeech(string employeeAdEntId, byte[] audioByteArray);
        bool SavePhoneticName(string employeeAdEntId, string phoneticName);


    }
}
