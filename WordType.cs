using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeTextToSpeech
{
    public enum WordType
    {
        Text,
        Normalized = Text,
        Lexical,
        Pronunciation
    }
}
