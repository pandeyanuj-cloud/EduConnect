using Microsoft.Data.SqlClient;
using EduConnect.Models;

namespace EduConnect.Repositories
{
    public class ClassRepository
    {
        private readonly string _connectionString;

        public ClassRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("EduConnectDb");
        }

        public List<ClassModel> GetAllClasses()
        {
            var classes = new List<ClassModel>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT ClassId, Subject, Fee, Location, ContactNo, Timings FROM Classes";
                SqlCommand cmd = new SqlCommand(query, conn);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    classes.Add(new ClassModel
                    {
                        ClassId = reader.GetInt32(0),
                        Subject = reader.GetString(1),
                        Fee = reader.GetDecimal(2),
                        Location = reader.GetString(3),
                        ContactNumber = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        ClassTimings = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                    });
                }
            }
            return classes;
        }
    }
}