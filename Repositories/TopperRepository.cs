using EduConnect.Models;
using Microsoft.Data.SqlClient;

namespace EduConnect.Repositories
{
    public class TopperRepository
    {
        private readonly string _connectionString;

        public TopperRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("EduConnectDb") ?? string.Empty;
        }

        public TopperModel? GetProfileForUser(int userId)
        {
            const string query = @"SELECT t.TopperId, a.UserId, t.Name, t.Standard, t.Marks10, t.Marks12,
                t.Marksheet10Url, t.Marksheet12Url, t.PhotoUrl, t.Description, t.PricePerDay, t.PricePerMonth, t.PricePerYear
                FROM Toppers t INNER JOIN TopperAccounts a ON a.TopperId = t.TopperId WHERE a.UserId = @UserId";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? ReadTopper(reader) : null;
        }

        public TopperModel? GetById(int topperId)
        {
            const string query = @"SELECT TopperId, 0, Name, Standard, Marks10, Marks12,
                Marksheet10Url, Marksheet12Url, PhotoUrl, Description, PricePerDay, PricePerMonth, PricePerYear
                FROM Toppers WHERE TopperId = @TopperId";
            return ReadSingle(query, topperId);
        }

        public List<TopperModel> GetAll()
        {
            const string query = @"SELECT TopperId, 0, Name, Standard, Marks10, Marks12,
                Marksheet10Url, Marksheet12Url, PhotoUrl, Description, PricePerDay, PricePerMonth, PricePerYear
                FROM Toppers ORDER BY TopperId DESC";
            var toppers = new List<TopperModel>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) toppers.Add(ReadTopper(reader));
            return toppers;
        }

        public TopperModel Save(int userId, TopperModel profile)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            var existing = GetProfileForUser(userId);
            if (existing == null)
            {
                const string insert = @"INSERT INTO Toppers (Name, Standard, Marks10, Marks12, Marksheet10Url, Marksheet12Url,
                    PhotoUrl, Description, PricePerDay, PricePerMonth, PricePerYear)
                    OUTPUT INSERTED.TopperId VALUES (@Name, @Standard, @Marks10, @Marks12, @Marksheet10Url, @Marksheet12Url,
                    @PhotoUrl, @Description, @PricePerDay, @PricePerMonth, @PricePerYear)";
                using var insertCmd = new SqlCommand(insert, conn, transaction);
                AddParameters(insertCmd, profile);
                profile.TopperId = (int)insertCmd.ExecuteScalar();
                using var accountCmd = new SqlCommand("INSERT INTO TopperAccounts (UserId, TopperId) VALUES (@UserId, @TopperId)", conn, transaction);
                accountCmd.Parameters.AddWithValue("@UserId", userId);
                accountCmd.Parameters.AddWithValue("@TopperId", profile.TopperId);
                accountCmd.ExecuteNonQuery();
            }
            else
            {
                profile.TopperId = existing.TopperId;
                const string update = @"UPDATE Toppers SET Name=@Name, Standard=@Standard, Marks10=@Marks10, Marks12=@Marks12,
                    Marksheet10Url=@Marksheet10Url, Marksheet12Url=@Marksheet12Url, PhotoUrl=@PhotoUrl, Description=@Description,
                    PricePerDay=@PricePerDay, PricePerMonth=@PricePerMonth, PricePerYear=@PricePerYear WHERE TopperId=@TopperId";
                using var updateCmd = new SqlCommand(update, conn, transaction);
                AddParameters(updateCmd, profile);
                updateCmd.Parameters.AddWithValue("@TopperId", profile.TopperId);
                updateCmd.ExecuteNonQuery();
            }
            transaction.Commit();
            profile.UserId = userId;
            return profile;
        }

        public List<TopperEnquiryModel> GetEnquiries(int userId)
        {
            const string query = @"SELECT e.EnquiryId, e.TopperId, e.StudentName, e.StudentPhone, e.StudentEmail, e.StudentStandard, e.CreatedAt
                FROM TopperStudentEnquiries e INNER JOIN TopperAccounts a ON a.TopperId = e.TopperId
                WHERE a.UserId = @UserId ORDER BY e.CreatedAt DESC";
            var enquiries = new List<TopperEnquiryModel>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                enquiries.Add(new TopperEnquiryModel { EnquiryId = reader.GetInt32(0), TopperId = reader.GetInt32(1), StudentName = reader.GetString(2), StudentPhone = reader.GetString(3), StudentEmail = reader.GetString(4), StudentStandard = reader.GetString(5), CreatedAt = reader.GetDateTime(6) });
            return enquiries;
        }

        public void CreateEnquiry(int topperId, int studentUserId, TopperEnquiryModel enquiry)
        {
            const string query = @"INSERT INTO TopperStudentEnquiries (TopperId, StudentUserId, StudentName, StudentPhone, StudentEmail, StudentStandard)
                VALUES (@TopperId, @StudentUserId, @StudentName, @StudentPhone, @StudentEmail, @StudentStandard)";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@TopperId", topperId);
            cmd.Parameters.AddWithValue("@StudentUserId", studentUserId);
            cmd.Parameters.AddWithValue("@StudentName", enquiry.StudentName);
            cmd.Parameters.AddWithValue("@StudentPhone", enquiry.StudentPhone);
            cmd.Parameters.AddWithValue("@StudentEmail", enquiry.StudentEmail);
            cmd.Parameters.AddWithValue("@StudentStandard", enquiry.StudentStandard);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        private TopperModel? ReadSingle(string query, int topperId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@TopperId", topperId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? ReadTopper(reader) : null;
        }

        private static TopperModel ReadTopper(SqlDataReader reader) => new()
        {
            TopperId = reader.GetInt32(0), UserId = reader.GetInt32(1), Name = reader.GetString(2), Standard = reader.GetString(3),
            Marks10 = reader.IsDBNull(4) ? null : reader.GetDecimal(4), Marks12 = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
            Marksheet10Url = reader.IsDBNull(6) ? string.Empty : reader.GetString(6), Marksheet12Url = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            PhotoUrl = reader.IsDBNull(8) ? string.Empty : reader.GetString(8), Description = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
            PricePerDay = reader.IsDBNull(10) ? null : reader.GetDecimal(10), PricePerMonth = reader.IsDBNull(11) ? null : reader.GetDecimal(11), PricePerYear = reader.IsDBNull(12) ? null : reader.GetDecimal(12)
        };

        private static void AddParameters(SqlCommand command, TopperModel profile)
        {
            command.Parameters.AddWithValue("@Name", profile.Name);
            command.Parameters.AddWithValue("@Standard", profile.Standard);
            command.Parameters.AddWithValue("@Marks10", (object?)profile.Marks10 ?? DBNull.Value);
            command.Parameters.AddWithValue("@Marks12", (object?)profile.Marks12 ?? DBNull.Value);
            command.Parameters.AddWithValue("@Marksheet10Url", (object?)profile.Marksheet10Url ?? DBNull.Value);
            command.Parameters.AddWithValue("@Marksheet12Url", (object?)profile.Marksheet12Url ?? DBNull.Value);
            command.Parameters.AddWithValue("@PhotoUrl", (object?)profile.PhotoUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@Description", (object?)profile.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@PricePerDay", (object?)profile.PricePerDay ?? DBNull.Value);
            command.Parameters.AddWithValue("@PricePerMonth", (object?)profile.PricePerMonth ?? DBNull.Value);
            command.Parameters.AddWithValue("@PricePerYear", (object?)profile.PricePerYear ?? DBNull.Value);
        }
    }
}