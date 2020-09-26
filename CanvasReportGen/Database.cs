using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Npgsql;
using System.Collections.Generic;

namespace CanvasReportGen {
    internal class Database : IAsyncDisposable {

        internal static bool UseSis { get; set; }
        internal static string ConnStr { private get; set; }
        internal static string CurrentYear { private get; set; }

        private NpgsqlConnection _connection;

        private Database() { }

        [CanBeNull]
        internal async Task<BasicStudentInfo> GetInfoBySis(string sis) {
            using var query = new NpgsqlCommand(DatabaseStrings.StudentInfoQuery, _connection);
            
            query.Parameters.AddWithValue("y", CurrentYear);
            query.Parameters.AddWithValue("s", sis);
            query.Prepare();

            await using var reader = await query.ExecuteReaderAsync();
            if (await reader.ReadAsync()) {
                return new BasicStudentInfo(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5)
                );
            }

            return null;
        }

        internal async IAsyncEnumerable<string> GetTruancyCheckStudents() {
            using var query = new NpgsqlCommand(DatabaseStrings.TruancyEntryDateQuery, _connection);

            query.Parameters.AddWithValue("y", CurrentYear);
            query.Prepare();

            await using var reader = await query.ExecuteReaderAsync();
            while (await reader.ReadAsync()) {
                yield return reader.GetString(0);
            } 
        }

        internal async Task<TruancyStudentInfo> GetTruancyInfo(string sis) {
            using var query = new NpgsqlCommand(DatabaseStrings.TruancyInfoQuery, _connection);
            
            query.Parameters.AddWithValue("y", CurrentYear);
            query.Parameters.AddWithValue("s", sis);
            await query.PrepareAsync();
            
            await using var reader = await query.ExecuteReaderAsync();
            if (await reader.ReadAsync()) {
                return new TruancyStudentInfo(
                    reader.GetStringOrDefault(0),
                    reader.GetStringOrDefault(1),
                    reader.GetStringOrDefault(2),
                    reader.GetStringOrDefault(3),
                    reader.GetStringOrDefault(4),
                    reader.GetStringOrDefault(5),
                    reader.GetStringOrDefault(6),
                    reader.GetStringOrDefault(7),
                    reader.GetStringOrDefault(8),
                    reader.GetStringOrDefault(9),
                    reader.GetStringOrDefault(10),
                    reader.GetStringOrDefault(11),
                    reader.GetStringOrDefault(12),
                    reader.GetStringOrDefault(13),
                    reader.GetStringOrDefault(14),
                    reader.GetDateTimeStringOrDefault(15),
                    reader.GetDateTimeStringOrDefault(16),
                    reader.GetStringOrDefault(17),
                    reader.GetStringOrDefault(18)
                );
            }

            return null;
        }

        internal static async Task<Database> Connect() {
            var d = new Database {
                _connection = new NpgsqlConnection(ConnStr)
            };
            await d._connection.OpenAsync();
            return d;
        }

        public ValueTask DisposeAsync() {
            return _connection.DisposeAsync();
        }
    }

    internal class BasicStudentInfo {
        public string Address { get; }
        public string City { get; }
        public string Zip { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string Grade { get; }

        public BasicStudentInfo(string address, string city, string zip, string firstName, string lastName, string grade) {
            Address = address;
            City = city;
            Zip = zip;
            FirstName = firstName;
            LastName = lastName;
            Grade = grade;
        }
    }
    
    internal class TruancyStudentInfo {
        public string FirstName { get; }
        public string LastName { get; }
        public string Grade { get; }
        public string Phone { get; }
        public string District { get; }
        public string Address { get; }
        public string City { get; }
        public string State { get; }
        public string Zip { get; }
        public string MotherName { get; }
        public string FatherName { get; }
        public string MotherEmail { get; }
        public string FatherEmail { get; }
        public string MotherCell { get; }
        public string FatherCell { get; }
        public string DateOfBirth { get; }
        public string EntryDate { get; }
        public string Gender { get; }
        public string School { get; }

        public TruancyStudentInfo(string firstName, string lastName, string grade, string phone, string district, string address, string city, string state, string zip, string motherName, string fatherName, string motherEmail, string fatherEmail, string motherCell, string fatherCell, string dob, string entryDate, string gender, string school) {
            FirstName = firstName;
            LastName = lastName;
            Grade = grade;
            Phone = phone;
            District = district;
            Address = address;
            City = city;
            State = state;
            Zip = zip;
            MotherName = motherName;
            FatherName = fatherName;
            MotherEmail = motherEmail;
            FatherEmail = fatherEmail;
            MotherCell = motherCell;
            FatherCell = fatherCell;
            DateOfBirth = dob;
            EntryDate = entryDate;
            Gender = gender;
            School = school;
        }
    }
}
