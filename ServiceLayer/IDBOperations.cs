using NamePronunciationTool.Models;
using System.Collections.Generic;

namespace NamePronunciationTool.ServiceLayer
{
    public interface IDBOperations
    {
        EmployeeData GetEmployeeData(string employeeId);
        string GetEmployeeNamePhoneticData(string employeeAdEntId);
        bool SaveSpeech(string employeeAdEntId, byte[] audioByteArray);
        bool SavePhoneticName(string employeeAdEntId, string phoneticName);
        bool SaveSpeechType(string type, string adEntId);

        List<EmployeeData> GetEmployeeList();
    }
}
