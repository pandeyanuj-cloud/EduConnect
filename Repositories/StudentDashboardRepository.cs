using EduConnect.Models;
using Microsoft.Data.SqlClient;

namespace EduConnect.Repositories
{
    public class StudentDashboardRepository
    {
        private readonly string _connectionString;

        public StudentDashboardRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("EduConnectDb") ?? string.Empty;
        }

        public StudentProfileModel? GetProfileByUserId(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            const string query = @"SELECT p.ProfileId, u.UserId, COALESCE(NULLIF(p.StudentName, ''), u.FullName), p.ProfilePhoto, p.Location, p.Standard,
                p.Subjects, p.Budget, p.LearningMode, u.Email, COALESCE(NULLIF(p.Mobile, ''), u.Phone), p.DateOfBirth, p.Gender, p.Board,
                p.SchoolName, p.Latitude, p.Longitude, p.PreferredPriceRange, p.CustomPrice, p.CreatedAt, p.UpdatedAt
                FROM Users u LEFT JOIN StudentProfiles p ON p.UserId = u.UserId WHERE u.UserId = @UserId";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            conn.Open();

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new StudentProfileModel
            {
                ProfileId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                StudentName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                ProfilePhoto = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Location = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Standard = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Subjects = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                Budget = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                LearningMode = reader.IsDBNull(8) ? "Both" : reader.GetString(8),
                Email = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                Mobile = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                DateOfBirth = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                Gender = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                Board = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                SchoolName = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
                Latitude = reader.IsDBNull(15) ? null : reader.GetDecimal(15),
                Longitude = reader.IsDBNull(16) ? null : reader.GetDecimal(16),
                PreferredPriceRange = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
                CustomPrice = reader.IsDBNull(18) ? null : reader.GetDecimal(18),
                CreatedAt = reader.IsDBNull(19) ? DateTime.UtcNow : reader.GetDateTime(19),
                UpdatedAt = reader.IsDBNull(20) ? DateTime.UtcNow : reader.GetDateTime(20)
            };
        }

        public StudentProfileModel SaveProfile(StudentProfileModel profile)
        {
            using var conn = new SqlConnection(_connectionString);

            var existing = GetProfileByUserId(profile.UserId);
            if (existing == null || existing.ProfileId == 0)
            {
                const string insert = @"INSERT INTO StudentProfiles (UserId, StudentName, ProfilePhoto, Location, Standard, Subjects, Budget, LearningMode,
                    Mobile, DateOfBirth, Gender, Board, SchoolName, Latitude, Longitude, PreferredPriceRange, CustomPrice, CreatedAt, UpdatedAt)
                    VALUES (@UserId, @StudentName, @ProfilePhoto, @Location, @Standard, @Subjects, @Budget, @LearningMode,
                    @Mobile, @DateOfBirth, @Gender, @Board, @SchoolName, @Latitude, @Longitude, @PreferredPriceRange, @CustomPrice,
                    SYSUTCDATETIME(), SYSUTCDATETIME())";

                using var insertCmd = new SqlCommand(insert, conn);
                insertCmd.Parameters.AddWithValue("@UserId", profile.UserId);
                insertCmd.Parameters.AddWithValue("@StudentName", (object?)profile.StudentName ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@ProfilePhoto", (object?)profile.ProfilePhoto ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Location", (object?)profile.Location ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Standard", (object?)profile.Standard ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Subjects", (object?)profile.Subjects ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Budget", profile.Budget);
                insertCmd.Parameters.AddWithValue("@LearningMode", (object?)profile.LearningMode ?? "Both");
                AddProfileParameters(insertCmd, profile);
                conn.Open();
                insertCmd.ExecuteNonQuery();
                return GetProfileByUserId(profile.UserId) ?? profile;
            }

            const string update = @"UPDATE StudentProfiles SET StudentName=@StudentName, ProfilePhoto=@ProfilePhoto,
                Location=@Location, Standard=@Standard, Subjects=@Subjects, Budget=@Budget, LearningMode=@LearningMode,
                Mobile=@Mobile, DateOfBirth=@DateOfBirth, Gender=@Gender, Board=@Board, SchoolName=@SchoolName,
                Latitude=@Latitude, Longitude=@Longitude, PreferredPriceRange=@PreferredPriceRange, CustomPrice=@CustomPrice,
                UpdatedAt=SYSUTCDATETIME() WHERE UserId=@UserId";

            using var updateCmd = new SqlCommand(update, conn);
            updateCmd.Parameters.AddWithValue("@UserId", profile.UserId);
            updateCmd.Parameters.AddWithValue("@StudentName", (object?)profile.StudentName ?? DBNull.Value);
            updateCmd.Parameters.AddWithValue("@ProfilePhoto", (object?)profile.ProfilePhoto ?? DBNull.Value);
            updateCmd.Parameters.AddWithValue("@Location", (object?)profile.Location ?? DBNull.Value);
            updateCmd.Parameters.AddWithValue("@Standard", (object?)profile.Standard ?? DBNull.Value);
            updateCmd.Parameters.AddWithValue("@Subjects", (object?)profile.Subjects ?? DBNull.Value);
            updateCmd.Parameters.AddWithValue("@Budget", profile.Budget);
            updateCmd.Parameters.AddWithValue("@LearningMode", (object?)profile.LearningMode ?? "Both");
            AddProfileParameters(updateCmd, profile);
            conn.Open();
            updateCmd.ExecuteNonQuery();
            return GetProfileByUserId(profile.UserId) ?? profile;
        }

        private static void AddProfileParameters(SqlCommand command, StudentProfileModel profile)
        {
            command.Parameters.AddWithValue("@Mobile", (object?)profile.Mobile ?? DBNull.Value);
            command.Parameters.AddWithValue("@DateOfBirth", (object?)profile.DateOfBirth ?? DBNull.Value);
            command.Parameters.AddWithValue("@Gender", (object?)profile.Gender ?? DBNull.Value);
            command.Parameters.AddWithValue("@Board", (object?)profile.Board ?? DBNull.Value);
            command.Parameters.AddWithValue("@SchoolName", (object?)profile.SchoolName ?? DBNull.Value);
            command.Parameters.AddWithValue("@Latitude", (object?)profile.Latitude ?? DBNull.Value);
            command.Parameters.AddWithValue("@Longitude", (object?)profile.Longitude ?? DBNull.Value);
            command.Parameters.AddWithValue("@PreferredPriceRange", (object?)profile.PreferredPriceRange ?? DBNull.Value);
            command.Parameters.AddWithValue("@CustomPrice", (object?)profile.CustomPrice ?? DBNull.Value);
        }

        public List<ClassModel> GetClasses(string? location, string? standard, string? subject, decimal? minFee, decimal? maxFee, string? mode, decimal? minRating)
        {
            var items = new List<ClassModel>();
            using var conn = new SqlConnection(_connectionString);

            var query = @"SELECT ClassId, Subject, Fee, Location, Standard, Rating, Mode, ImageUrl, Description, Materials, ContactNo, Timings
                FROM Classes WHERE 1 = 1";

            var locationTerms = GetLocationTerms(location);
            if (locationTerms.Count > 0)
                query += $" AND ({string.Join(" OR ", locationTerms.Select((_, index) => $"REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(LOWER(Location), ' ', ''), ',', ''), '(', ''), ')', ''), '-', '') LIKE @Location{index}"))})";
            if (!string.IsNullOrWhiteSpace(standard))
                query += " AND Standard LIKE @Standard";
            if (!string.IsNullOrWhiteSpace(subject))
                query += " AND Subject LIKE @Subject";
            if (minFee.HasValue)
                query += " AND Fee >= @MinFee";
            if (maxFee.HasValue)
                query += " AND Fee <= @MaxFee";
            if (!string.IsNullOrWhiteSpace(mode) && mode != "All" && mode != "Both")
                query += " AND (Mode = @Mode OR Mode = 'Both')";
            if (minRating.HasValue)
                query += " AND Rating >= @MinRating";

            query += " ORDER BY Rating DESC, Fee ASC";

            using var cmd = new SqlCommand(query, conn);
            for (var index = 0; index < locationTerms.Count; index++)
                cmd.Parameters.AddWithValue($"@Location{index}", $"%{NormalizeLocationTerm(locationTerms[index])}%");
            if (!string.IsNullOrWhiteSpace(standard)) cmd.Parameters.AddWithValue("@Standard", $"%{standard}%");
            if (!string.IsNullOrWhiteSpace(subject)) cmd.Parameters.AddWithValue("@Subject", $"%{subject}%");
            if (minFee.HasValue) cmd.Parameters.AddWithValue("@MinFee", minFee.Value);
            if (maxFee.HasValue) cmd.Parameters.AddWithValue("@MaxFee", maxFee.Value);
            if (!string.IsNullOrWhiteSpace(mode) && mode != "All" && mode != "Both") cmd.Parameters.AddWithValue("@Mode", mode);
            if (minRating.HasValue) cmd.Parameters.AddWithValue("@MinRating", minRating.Value);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var classItem = new ClassModel
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

                items.Add(classItem);
            }

            return items;
        }

        private static List<string> GetLocationTerms(string? location)
        {
            return (location ?? string.Empty)
                .Split(new[] { ',', ' ', '(', ')', '-', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(term => term.Trim())
                .Where(term => term.Length >= 3)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

            private static string NormalizeLocationTerm(string value)
            {
                return value.Replace(" ", string.Empty)
                .Replace(",", string.Empty)
                .Replace("(", string.Empty)
                .Replace(")", string.Empty)
                .Replace("-", string.Empty)
                .ToLowerInvariant();
            }

        public ClassModel? GetClassById(int classId)
        {
            var list = GetClasses(null, null, null, null, null, null, null);
            return list.FirstOrDefault(c => c.ClassId == classId);
        }

        public List<SavedClassModel> GetSavedClasses(int userId)
        {
            var saved = new List<SavedClassModel>();
            using var conn = new SqlConnection(_connectionString);
            const string query = @"SELECT sc.SavedId, sc.UserId, sc.ClassId, c.Subject, c.Location, c.Fee,
                c.Standard, c.Rating, c.Mode, c.ImageUrl, c.Description, c.Materials
                FROM SavedClasses sc LEFT JOIN Classes c ON c.ClassId = sc.ClassId WHERE sc.UserId = @UserId";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                saved.Add(new SavedClassModel
                {
                    SavedId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    ClassId = reader.GetInt32(2),
                    ClassName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Location = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    Fee = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                    Subject = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Standard = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    Rating = reader.IsDBNull(7) ? 0 : Convert.ToDecimal(reader.GetValue(7)),
                    Mode = reader.IsDBNull(8) ? "Both" : reader.GetString(8),
                    ImageUrl = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                    Description = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                    MaterialProvided = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
                });
            }

            return saved;
        }

        public bool SaveClassForStudent(int userId, int classId)
        {
            using var conn = new SqlConnection(_connectionString);
            const string checkQuery = @"SELECT COUNT(1) FROM SavedClasses WHERE UserId=@UserId AND ClassId=@ClassId";
            using (var checkCmd = new SqlCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("@UserId", userId);
                checkCmd.Parameters.AddWithValue("@ClassId", classId);
                conn.Open();
                var exists = (int)checkCmd.ExecuteScalar() > 0;
                if (exists) return false;
            }

            const string insert = @"INSERT INTO SavedClasses (UserId, ClassId) VALUES (@UserId, @ClassId)";
            using var cmd = new SqlCommand(insert, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@ClassId", classId);
            cmd.ExecuteNonQuery();
            return true;
        }

        public void RemoveSavedClass(int userId, int classId)
        {
            using var conn = new SqlConnection(_connectionString);
            const string query = @"DELETE FROM SavedClasses WHERE UserId=@UserId AND ClassId=@ClassId";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@ClassId", classId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public List<StudentBookingModel> GetBookings(int userId)
        {
            var bookings = new List<StudentBookingModel>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            EnsureStudentBookingsTable(conn);
            const string query = @"SELECT BookingId, UserId, Type, TargetId, TargetName, BookingDate, Status, ContactInfo
                FROM StudentBookings WHERE UserId = @UserId ORDER BY BookingDate DESC";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                bookings.Add(new StudentBookingModel
                {
                    BookingId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    Type = reader.IsDBNull(2) ? "Class" : reader.GetString(2),
                    TargetId = reader.GetInt32(3),
                    TargetName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    BookingDate = reader.IsDBNull(5) ? DateTime.Now : reader.GetDateTime(5),
                    Status = reader.IsDBNull(6) ? "Pending" : reader.GetString(6),
                    ContactInfo = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
                });
            }

            return bookings;
        }

        public StudentBookingModel CreateBooking(StudentBookingModel booking)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            EnsureStudentBookingsTable(conn);
            const string duplicateCheck = @"SELECT TOP 1 BookingId, UserId, Type, TargetId, TargetName, BookingDate, Status, ContactInfo
                FROM StudentBookings WHERE UserId=@UserId AND TargetId=@TargetId AND Type=@Type";
            using var duplicateCmd = new SqlCommand(duplicateCheck, conn);
            duplicateCmd.Parameters.AddWithValue("@UserId", booking.UserId);
            duplicateCmd.Parameters.AddWithValue("@TargetId", booking.TargetId);
            duplicateCmd.Parameters.AddWithValue("@Type", (object?)booking.Type ?? "Class");
            using (var reader = duplicateCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new StudentBookingModel
                    {
                        BookingId = reader.GetInt32(0),
                        UserId = reader.GetInt32(1),
                        Type = reader.IsDBNull(2) ? "Class" : reader.GetString(2),
                        TargetId = reader.GetInt32(3),
                        TargetName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        BookingDate = reader.IsDBNull(5) ? DateTime.Now : reader.GetDateTime(5),
                        Status = reader.IsDBNull(6) ? "Pending" : reader.GetString(6),
                        ContactInfo = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
                    };
                }
            }

            const string query = @"INSERT INTO StudentBookings (UserId, Type, TargetId, TargetName, BookingDate, Status, ContactInfo)
                VALUES (@UserId, @Type, @TargetId, @TargetName, @BookingDate, @Status, @ContactInfo)";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", booking.UserId);
            cmd.Parameters.AddWithValue("@Type", (object?)booking.Type ?? "Class");
            cmd.Parameters.AddWithValue("@TargetId", booking.TargetId);
            cmd.Parameters.AddWithValue("@TargetName", (object?)booking.TargetName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BookingDate", booking.BookingDate == default ? DateTime.Now : booking.BookingDate);
            cmd.Parameters.AddWithValue("@Status", (object?)booking.Status ?? "Pending");
            cmd.Parameters.AddWithValue("@ContactInfo", (object?)booking.ContactInfo ?? DBNull.Value);

            cmd.ExecuteNonQuery();
            return booking;
        }

        private static void EnsureStudentBookingsTable(SqlConnection conn)
        {
            const string query = @"IF OBJECT_ID('dbo.StudentBookings', 'U') IS NULL
                CREATE TABLE dbo.StudentBookings (
                    BookingId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    UserId INT NOT NULL,
                    Type NVARCHAR(50) NOT NULL,
                    TargetId INT NOT NULL,
                    TargetName NVARCHAR(200) NULL,
                    BookingDate DATETIME2 NOT NULL CONSTRAINT DF_StudentBookings_BookingDate DEFAULT SYSUTCDATETIME(),
                    Status NVARCHAR(50) NOT NULL CONSTRAINT DF_StudentBookings_Status DEFAULT 'Pending',
                    ContactInfo NVARCHAR(500) NULL
                );";
            using var cmd = new SqlCommand(query, conn);
            cmd.ExecuteNonQuery();
        }
    }
}
