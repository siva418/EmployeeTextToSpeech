using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NamePronunciationTool
{
    public class VoiceList
    {
        private Dictionary<string, string> femaleVoiceList = new Dictionary<string, string> {
            { "Australia","en-AU-NatashaNeural" },
            { "India","en-IN-NeerjaNeural" },
            { "US","en-US-JennyNeural" },      
            { "China","en-US-JennyNeural" }       
        };
        
        private Dictionary<string, string> maleVoiceList = new Dictionary<string, string> {
            { "Australia","en-AU-WilliamNeural" },
            { "India","en-IN-PrabhatNeural" },
            { "US","en-US-GuyNeural" },     
            { "China","en-US-GuyNeural" }     
        };

        public Dictionary<string,string> GetVoiceList(bool isFemale)
        {
            if(isFemale)
            {
                return femaleVoiceList;
            }
            return maleVoiceList;
        }
    }
}
