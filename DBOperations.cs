using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NamePronunciation.ServiceLayer
{
    public class DBOperations
    {
        public IConfiguration _configuration;
        public DBOperations(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string DBConnString
        {
            get
            {
                return _configuration.GetValue<string>("connectionString");
            }
        }
        
    }
}