using Microsoft.Extensions.Configuration;
using NamePronunciationTool.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NamePronunciationTool.ServiceLayer
{
    public class DBOperationService : IDBOperations
    {

        public IConfiguration _configuration;
        public DBOperationService(IConfiguration configuration)
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

        public EmployeeData GetEmployeeData(string employeeAdEntId)
        {
            NpgsqlConnection conn = new NpgsqlConnection("host=20.55.122.15;port=5433;database=yugabyte;user id=yugabyte;password=Hackathon22!");
            bool result = false;
            EmployeeData employeeData = null;

            try
            {
                conn.Open();

                NpgsqlCommand empPrepCmd = new NpgsqlCommand("SELECT * FROM public.employee_data WHERE \"AD_ENT_ID\" = @ADENTID", conn);
                empPrepCmd.Parameters.Add("@ADENTID", NpgsqlTypes.NpgsqlDbType.Text);

                empPrepCmd.Parameters["@ADENTID"].Value = employeeAdEntId;
                NpgsqlDataReader reader = empPrepCmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        employeeData = new EmployeeData();
                        employeeData.ADENTID = (string)reader["AD_ENT_ID"];
                        employeeData.EmployeeId = (string)reader["Employee_ID"];
                        employeeData.FirstName = (string)reader["First_Name"];
                        employeeData.LastName = (string)reader["Last_Name"];
                        employeeData.ADENTID = (string)reader["Middle_Name"];
                        employeeData.PreferredName = (string)reader["Preferred_Name"];
                        employeeData.ADENTID = (string)reader["Email_ID"];
                        employeeData.ADENTID = (string)reader["Preferred_Name"];
                        employeeData.Country = (string)reader["Country"];
                    }
                }
            }
            catch (Exception ex)
            {
                result = false;
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                {
                    conn.Close();
                }
            }

            employeeData.NamePhonetic = GetEmployeeNamePhoneticData(employeeAdEntId);

            return employeeData;
        }

        public string GetEmployeeNamePhoneticData(string employeeAdEntId)
        {
            NpgsqlConnection conn = new NpgsqlConnection("host=20.55.122.15;port=5433;database=yugabyte;user id=yugabyte;password=Hackathon22!");
            bool result = false;
            string phoneticName = string.Empty;

            try
            {
                conn.Open();

                NpgsqlCommand empPrepCmd = new NpgsqlCommand("SELECT * FROM public.emp_name_pronunciation_data WHERE \"AD_ENT_ID\" = @ADENTID", conn);
                empPrepCmd.Parameters.Add("@ADENTID", NpgsqlTypes.NpgsqlDbType.Text);

                empPrepCmd.Parameters["@ADENTID"].Value = employeeAdEntId;
                NpgsqlDataReader reader = empPrepCmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        phoneticName = (string)reader["Speech_Phonetic"];
                    }
                }
            }
            catch (Exception ex)
            {
                result = false;
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                {
                    conn.Close();
                }
            }

            return phoneticName;
        }

        public bool SaveSpeech(string employeeAdEntId, byte[] audioByteArray)
        {
            NpgsqlConnection conn = new NpgsqlConnection("host=20.55.122.15;port=5433;database=yugabyte;user id=yugabyte;password=Hackathon22!");
            bool result = false;
            bool hasSpeech = false;

            try
            {
                conn.Open();

                NpgsqlCommand empPrepCmd = new NpgsqlCommand("SELECT * FROM public.emp_name_pronunciation_data WHERE \"AD_ENT_ID\" = @ADENTID", conn);
                empPrepCmd.Parameters.Add("@ADENTID", NpgsqlTypes.NpgsqlDbType.Text);

                empPrepCmd.Parameters["@ADENTID"].Value = employeeAdEntId;
                NpgsqlDataReader reader = empPrepCmd.ExecuteReader();

                if (reader.HasRows)
                {
                    hasSpeech = true;
                }
            }
            catch (Exception ex)
            {
                result = false;
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                {
                    conn.Close();
                }
            }

            try
            {
                conn.Open();

                if (hasSpeech)
                {
                    NpgsqlCommand empUpdateCmd = new NpgsqlCommand("UPDATE public.emp_name_pronunciation_data SET \"Custom_Speech\" = '" + audioByteArray + "' WHERE \"AD_ENT_ID\" = '" + employeeAdEntId + "';", conn);
                    int updatedRows = empUpdateCmd.ExecuteNonQuery();
                }
                else
                {
                    NpgsqlCommand empInsertCmd = new NpgsqlCommand("INSERT INTO public.emp_name_pronunciation_data (\"AD_ENT_ID\", \"Speech_Type\", \"Custom_Speech\") VALUES ('" + employeeAdEntId + "', 'Custom', '" + audioByteArray + "');", conn);
                    int insertedRows = empInsertCmd.ExecuteNonQuery();
                }

                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                {
                    conn.Close();
                }
            }

            return result;
        }

        public bool SavePhoneticName(string employeeAdEntId, string phoneticName)
        {
            NpgsqlConnection conn = new NpgsqlConnection("host=20.55.122.15;port=5433;database=yugabyte;user id=yugabyte;password=Hackathon22!");
            bool result = false;
            bool hasSpeech = false;

            try
            {
                conn.Open();

                NpgsqlCommand empPrepCmd = new NpgsqlCommand("SELECT * FROM public.emp_name_pronunciation_data WHERE \"AD_ENT_ID\" = @ADENTID", conn);
                empPrepCmd.Parameters.Add("@ADENTID", NpgsqlTypes.NpgsqlDbType.Text);

                empPrepCmd.Parameters["@ADENTID"].Value = employeeAdEntId;
                NpgsqlDataReader reader = empPrepCmd.ExecuteReader();

                if (reader.HasRows)
                {
                    hasSpeech = true;
                }
            }
            catch (Exception ex)
            {
                result = false;
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                {
                    conn.Close();
                }
            }

            try
            {
                conn.Open();

                if (hasSpeech)
                {
                    NpgsqlCommand empUpdateCmd = new NpgsqlCommand("UPDATE public.emp_name_pronunciation_data SET \"Speech_Phonetic\" = '" + phoneticName + "' WHERE \"AD_ENT_ID\" = '" + employeeAdEntId + "';", conn);
                    int updatedRows = empUpdateCmd.ExecuteNonQuery();
                }
                else
                {
                    NpgsqlCommand empInsertCmd = new NpgsqlCommand("INSERT INTO public.emp_name_pronunciation_data (\"AD_ENT_ID\", \"Speech_Type\", \"Speech_Phonetic\") VALUES ('" + employeeAdEntId + "', 'Standard', '" + phoneticName + "');", conn);
                    int insertedRows = empInsertCmd.ExecuteNonQuery();
                }

                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                {
                    conn.Close();
                }
            }

            return result;
        }
    }
}
