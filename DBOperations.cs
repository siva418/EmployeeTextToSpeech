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