using EduConnect.Models;
using Microsoft.Data.SqlClient;

namespace EduConnect.Repositories
{
    public class OwnerRepository
    {
        private readonly string _connectionString;

        public OwnerRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("EduConnectDb") ?? string.Empty;
        }

        public List<ClassModel> GetClasses(int ownerId)
        {
            EnsureSupportTables();
            var classes = new List<ClassModel>();
            using var conn = new SqlConnection(_connectionString);
            const string query = @"SELECT c.ClassId, c.Subject, c.Fee, c.Location, c.Standard, c.Rating, c.Mode, c.ImageUrl, c.Description, c.Materials,
                c.ContactNo, c.Timings
                FROM Classes c INNER JOIN ClassOwnerClasses oc ON oc.ClassId = c.ClassId
                WHERE oc.UserId = @UserId ORDER BY c.ClassId DESC";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", ownerId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                classes.Add(ReadClass(reader));
            return classes;
        }

        public ClassModel CreateClass(int ownerId, ClassModel classItem)
        {
            EnsureSupportTables();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            const string insert = @"INSERT INTO Classes (Subject, Fee, Location, Standard, Rating, Mode, ImageUrl, Description, Materials, ContactNo, Timings)
                OUTPUT INSERTED.ClassId VALUES (@Subject, @Fee, @Location, @Standard, @Rating, @Mode, @ImageUrl, @Description, @Materials, @ContactNo, @Timings)";
            using var insertCmd = new SqlCommand(insert, conn, transaction);
            AddClassParameters(insertCmd, classItem);
            classItem.ClassId = (int)insertCmd.ExecuteScalar();

            using var linkCmd = new SqlCommand("INSERT INTO ClassOwnerClasses (UserId, ClassId) VALUES (@UserId, @ClassId)", conn, transaction);
            linkCmd.Parameters.AddWithValue("@UserId", ownerId);
            linkCmd.Parameters.AddWithValue("@ClassId", classItem.ClassId);
            linkCmd.ExecuteNonQuery();
            transaction.Commit();
            return classItem;
        }

        public ClassModel? UpdateClass(int ownerId, ClassModel classItem)
        {
            EnsureSupportTables();
            using var conn = new SqlConnection(_connectionString);
            const string query = @"UPDATE c SET Subject=@Subject, Fee=@Fee, Location=@Location, Standard=@Standard,
                Rating=@Rating, Mode=@Mode, ImageUrl=@ImageUrl, Description=@Description, Materials=@Materials,
                ContactNo=@ContactNo, Timings=@Timings
                FROM Classes c INNER JOIN ClassOwnerClasses oc ON oc.ClassId = c.ClassId
                WHERE c.ClassId=@ClassId AND oc.UserId=@UserId";
            using var cmd = new SqlCommand(query, conn);
            AddClassParameters(cmd, classItem);
            cmd.Parameters.AddWithValue("@ClassId", classItem.ClassId);
            cmd.Parameters.AddWithValue("@UserId", ownerId);
            conn.Open();
            return cmd.ExecuteNonQuery() == 0 ? null : classItem;
        }

        public bool DeleteClass(int ownerId, int classId)
        {
            EnsureSupportTables();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            using var unlinkCmd = new SqlCommand("DELETE FROM ClassOwnerClasses WHERE UserId=@UserId AND ClassId=@ClassId", conn, transaction);
            unlinkCmd.Parameters.AddWithValue("@UserId", ownerId);
            unlinkCmd.Parameters.AddWithValue("@ClassId", classId);
            if (unlinkCmd.ExecuteNonQuery() == 0)
            {
                transaction.Rollback();
                return false;
            }

            using var deleteCmd = new SqlCommand("DELETE FROM Classes WHERE ClassId=@ClassId", conn, transaction);
            deleteCmd.Parameters.AddWithValue("@ClassId", classId);
            deleteCmd.ExecuteNonQuery();
            transaction.Commit();
            return true;
        }

        public List<OwnerEnquiryModel> GetEnquiries(int ownerId)
        {
            EnsureSupportTables();
            var enquiries = new List<OwnerEnquiryModel>();
            using var conn = new SqlConnection(_connectionString);
            const string query = @"SELECT b.BookingId, COALESCE(NULLIF(p.StudentName, ''), u.FullName), u.Email,
                COALESCE(NULLIF(p.Mobile, ''), u.Phone), c.Subject, b.BookingDate, b.Status,
                p.Standard, p.Board, p.Budget, p.PreferredPriceRange, p.CustomPrice
                FROM StudentBookings b INNER JOIN Classes c ON c.ClassId = b.TargetId
                INNER JOIN ClassOwnerClasses oc ON oc.ClassId = c.ClassId
                INNER JOIN Users u ON u.UserId = b.UserId
                LEFT JOIN StudentProfiles p ON p.UserId = b.UserId
                WHERE oc.UserId = @UserId AND b.Type = 'Class' ORDER BY b.BookingDate DESC";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", ownerId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                enquiries.Add(new OwnerEnquiryModel
                {
                    BookingId = reader.GetInt32(0),
                    StudentName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    StudentEmail = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    StudentPhone = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Subject = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    EnquiryDate = reader.IsDBNull(5) ? DateTime.Now : reader.GetDateTime(5),
                    Status = reader.IsDBNull(6) ? "Pending" : reader.GetString(6),
                    StudentStandard = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    StudentBoard = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                    YearlyBudget = GetYearlyBudget(reader, 9, 10, 11)
                });
            }
            return enquiries;
        }

        private static string GetYearlyBudget(SqlDataReader reader, int budgetOrdinal, int priceRangeOrdinal, int customPriceOrdinal)
        {
            if (!reader.IsDBNull(customPriceOrdinal) && reader.GetDecimal(customPriceOrdinal) > 0)
                return $"₹{reader.GetDecimal(customPriceOrdinal).ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("en-IN"))}/year";

            if (!reader.IsDBNull(budgetOrdinal) && reader.GetDecimal(budgetOrdinal) > 0)
                return $"₹{reader.GetDecimal(budgetOrdinal).ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("en-IN"))}/year";

            var priceRange = reader.IsDBNull(priceRangeOrdinal) ? string.Empty : reader.GetString(priceRangeOrdinal);
            return string.Equals(priceRange, "Custom", StringComparison.OrdinalIgnoreCase) ? string.Empty : priceRange;
        }

        public List<OwnerReviewModel> GetReviews(int ownerId)
        {
            EnsureSupportTables();
            var reviews = new List<OwnerReviewModel>();
            using var conn = new SqlConnection(_connectionString);
            const string query = @"SELECT r.ReviewId, u.FullName, r.Rating, r.Comment, c.Subject, r.CreatedAt
                FROM Reviews r INNER JOIN Classes c ON c.ClassId = r.ClassId
                INNER JOIN ClassOwnerClasses oc ON oc.ClassId = c.ClassId
                INNER JOIN Users u ON u.UserId = r.UserId
                WHERE oc.UserId = @UserId ORDER BY r.CreatedAt DESC";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", ownerId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                reviews.Add(new OwnerReviewModel
                {
                    ReviewId = reader.GetInt32(0),
                    StudentName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Rating = reader.IsDBNull(2) ? 0 : Convert.ToDecimal(reader.GetValue(2)),
                    Comment = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Subject = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    CreatedAt = reader.IsDBNull(5) ? DateTime.Now : reader.GetDateTime(5)
                });
            }
            return reviews;
        }

        private void EnsureSupportTables()
        {
            using var conn = new SqlConnection(_connectionString);
            const string query = @"IF OBJECT_ID('dbo.ClassOwnerClasses', 'U') IS NULL
                CREATE TABLE ClassOwnerClasses (UserId INT NOT NULL, ClassId INT NOT NULL, CONSTRAINT PK_ClassOwnerClasses PRIMARY KEY (UserId, ClassId));
                IF OBJECT_ID('dbo.Reviews', 'U') IS NULL
                CREATE TABLE Reviews (ReviewId INT IDENTITY(1,1) PRIMARY KEY, UserId INT NOT NULL, ClassId INT NOT NULL, Rating DECIMAL(3,1) NOT NULL, Comment NVARCHAR(1000) NULL, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
                IF OBJECT_ID('dbo.StudentBookings', 'U') IS NULL
                CREATE TABLE dbo.StudentBookings (
                    BookingId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    UserId INT NOT NULL,
                    Type NVARCHAR(50) NOT NULL,
                    TargetId INT NOT NULL,
                    TargetName NVARCHAR(200) NULL,
                    BookingDate DATETIME2 NOT NULL CONSTRAINT DF_StudentBookings_BookingDate DEFAULT SYSUTCDATETIME(),
                    Status NVARCHAR(50) NOT NULL CONSTRAINT DF_StudentBookings_Status DEFAULT 'Pending',
                    ContactInfo NVARCHAR(500) NULL
                );
                IF COL_LENGTH('dbo.Classes', 'ContactNo') IS NULL
                    ALTER TABLE dbo.Classes ADD ContactNo NVARCHAR(20) NULL;
                IF COL_LENGTH('dbo.Classes', 'Timings') IS NULL
                    ALTER TABLE dbo.Classes ADD Timings NVARCHAR(200) NULL;";
            using var cmd = new SqlCommand(query, conn);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        private static void AddClassParameters(SqlCommand command, ClassModel classItem)
        {
            command.Parameters.AddWithValue("@Subject", classItem.Subject);
            command.Parameters.AddWithValue("@Fee", classItem.Fee);
            command.Parameters.AddWithValue("@Location", classItem.Location);
            command.Parameters.AddWithValue("@Standard", classItem.Standard);
            command.Parameters.AddWithValue("@Rating", classItem.Rating);
            command.Parameters.AddWithValue("@Mode", classItem.Mode);
            command.Parameters.AddWithValue("@ImageUrl", (object?)classItem.ImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@Description", (object?)classItem.Description ?? DBNull.Value);
            command.Parameters.Add("@Materials", System.Data.SqlDbType.NVarChar, -1).Value =
                (object?)classItem.MaterialProvided ?? DBNull.Value;
            command.Parameters.Add("@ContactNo", System.Data.SqlDbType.NVarChar, 20).Value =
                (object?)classItem.ContactNumber ?? DBNull.Value;
            command.Parameters.Add("@Timings", System.Data.SqlDbType.NVarChar, 200).Value =
                (object?)classItem.ClassTimings ?? DBNull.Value;
        }

        private static ClassModel ReadClass(SqlDataReader reader)
        {
            return new ClassModel
            {
                ClassId = reader.GetInt32(0),
                Subject = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Fee = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
                Location = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Standard = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Rating = reader.IsDBNull(5) ? 0 : Convert.ToDecimal(reader.GetValue(5)),
                Mode = reader.IsDBNull(6) ? "Both" : reader.GetString(6),
                ImageUrl = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                Description = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                MaterialProvided = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                ContactNumber = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                ClassTimings = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
            };
        }
    }
}
