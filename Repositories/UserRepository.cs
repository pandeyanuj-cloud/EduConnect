using Microsoft.Data.SqlClient;
using EduConnect.Models;

namespace EduConnect.Repositories
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("EduConnectDb") ?? string.Empty;
        }

        public bool EmailExists(string email)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            string query = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Email", email);
            conn.Open();
            int count = (int)cmd.ExecuteScalar();
            return count > 0;
        }

        public void CreateUser(UserModel user)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            string query = "INSERT INTO Users (FullName, Email, PasswordHash, Phone, Role) OUTPUT INSERTED.UserId VALUES (@FullName, @Email, @PasswordHash, @Phone, @Role)";
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@FullName", user.FullName);
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@Phone", user.Phone);
            cmd.Parameters.AddWithValue("@Role", user.Role);
            conn.Open();
            user.UserId = (int)cmd.ExecuteScalar();
        }

        public UserModel? GetUserByEmail(string email)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            string query = "SELECT UserId, FullName, Email, PasswordHash, Phone, Role FROM Users WHERE Email = @Email";
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Email", email);
            conn.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new UserModel
                {
                    UserId = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Email = reader.GetString(2),
                    PasswordHash = reader.GetString(3),
                    Phone = reader.GetString(4),
                    Role = reader.GetString(5)
                };
            }
            return null;
        }

        public UserModel? GetUserById(int userId)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            const string query = "SELECT UserId, FullName, Email, PasswordHash, Phone, Role FROM Users WHERE UserId = @UserId";
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            conn.Open();
            using SqlDataReader reader = cmd.ExecuteReader();

            if (!reader.Read()) return null;

            return new UserModel
            {
                UserId = reader.GetInt32(0),
                FullName = reader.GetString(1),
                Email = reader.GetString(2),
                PasswordHash = reader.GetString(3),
                Phone = reader.GetString(4),
                Role = reader.GetString(5)
            };
        }

        public bool UpdatePassword(int userId, string passwordHash)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            const string query = "UPDATE Users SET PasswordHash = @PasswordHash WHERE UserId = @UserId";
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmd.Parameters.AddWithValue("@UserId", userId);
            conn.Open();
            return cmd.ExecuteNonQuery() == 1;
        }
    }
}