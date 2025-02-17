﻿using Microsoft.Extensions.Configuration;
using NamePronunciationTool.Models;
using Npgsql;
using System;
using System.Collections.Generic;

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
                        employeeData.MiddleName = (string)reader["Middle_Name"];
                        employeeData.PreferredName = (string)reader["Preferred_Name"];
                        employeeData.EmailId = (string)reader["Email_ID"];
                        employeeData.PreferredName = (string)reader["Preferred_Name"];
                        employeeData.Country = (string)reader["Country"];
                        employeeData.SpeechType = GetEmployeeSpeechType(employeeData.ADENTID);
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

        public List<EmployeeData> GetEmployeeList()
        {
            NpgsqlConnection conn = new NpgsqlConnection("host=20.55.122.15;port=5433;database=yugabyte;user id=yugabyte;password=Hackathon22!");
            bool result = false;
            List<EmployeeData> employeesData = null;

            try
            {
                conn.Open();

                NpgsqlCommand empPrepCmd = new NpgsqlCommand("SELECT * FROM public.employee_data", conn);
                
                NpgsqlDataReader reader = empPrepCmd.ExecuteReader();

                if (reader.HasRows)
                {
                    employeesData = new List<EmployeeData>();

                    while (reader.Read())
                    {
                        EmployeeData employeeData = new EmployeeData();
                        employeeData.ADENTID = (string)reader["AD_ENT_ID"];
                        employeeData.EmployeeId = (string)reader["Employee_ID"];
                        employeeData.FirstName = (string)reader["First_Name"];
                        employeeData.LastName = (string)reader["Last_Name"];
                        employeeData.MiddleName = (string)reader["Middle_Name"];
                        employeeData.PreferredName = (string)reader["Preferred_Name"];
                        employeeData.EmailId = (string)reader["Email_ID"];
                        employeeData.PreferredName = (string)reader["Preferred_Name"];
                        employeeData.Country = (string)reader["Country"];
                        employeeData.SpeechType = GetEmployeeSpeechType(employeeData.ADENTID);
                        employeeData.NamePhonetic = GetEmployeeNamePhoneticData(employeeData.ADENTID);
                        employeesData.Add(employeeData);
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

            return employeesData;
        }

        public string GetEmployeeSpeechType(string employeeAdEntId)
        {
            NpgsqlConnection conn = new NpgsqlConnection("host=20.55.122.15;port=5433;database=yugabyte;user id=yugabyte;password=Hackathon22!");
            bool result = false;
            string speechType = string.Empty;

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
                        speechType = (string)reader["Speech_Type"];
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

            return string.IsNullOrWhiteSpace(speechType) ? "Standard" : speechType;
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
                    NpgsqlCommand empUpdateCmd = new NpgsqlCommand("UPDATE public.emp_name_pronunciation_data SET \"Custom_Speech\" = '" + audioByteArray + "',\"Speech_Type\" = 'Custom' WHERE \"AD_ENT_ID\" = '" + employeeAdEntId + "';", conn);
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
                    NpgsqlCommand empUpdateCmd = new NpgsqlCommand("UPDATE public.emp_name_pronunciation_data SET \"Speech_Phonetic\" = '" + phoneticName + "',\"Speech_Type\"='Standard' WHERE \"AD_ENT_ID\" = '" + employeeAdEntId + "';", conn);
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

        public bool SaveSpeechType(string type, string adEntId)
        {
            NpgsqlConnection conn = new NpgsqlConnection("host=20.55.122.15;port=5433;database=yugabyte;user id=yugabyte;password=Hackathon22!");
            bool result = false;
            bool hasSpeech = false;

            try
            {
                conn.Open();

                NpgsqlCommand empPrepCmd = new NpgsqlCommand("SELECT * FROM public.emp_name_pronunciation_data WHERE \"AD_ENT_ID\" = @ADENTID", conn);
                empPrepCmd.Parameters.Add("@ADENTID", NpgsqlTypes.NpgsqlDbType.Text);

                empPrepCmd.Parameters["@ADENTID"].Value = adEntId;
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
                    NpgsqlCommand empUpdateCmd = new NpgsqlCommand("UPDATE public.emp_name_pronunciation_data SET \"Speech_Type\" = '" + type + "',\"Speech_Type\"='Standard' WHERE \"AD_ENT_ID\" = '" + adEntId + "';", conn);
                    int updatedRows = empUpdateCmd.ExecuteNonQuery();
                }
                else
                {
                    NpgsqlCommand empInsertCmd = new NpgsqlCommand("INSERT INTO public.emp_name_pronunciation_data (\"AD_ENT_ID\", \"Speech_Type\", \"Speech_Phonetic\") VALUES ('" + adEntId + "', '"+type+"', '');", conn);
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
