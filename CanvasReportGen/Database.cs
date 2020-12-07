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
            await query.PrepareAsync();

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
                    reader.GetStringOrDefault(reader.GetOrdinal("first_name")),
                    reader.GetStringOrDefault(reader.GetOrdinal("last_name")),
                    reader.GetStringOrDefault(reader.GetOrdinal("grade")),
                    reader.GetStringOrDefault(reader.GetOrdinal("phone")),
                    reader.GetStringOrDefault(reader.GetOrdinal("cell")),
                    reader.GetStringOrDefault(reader.GetOrdinal("district")),
                    reader.GetStringOrDefault(reader.GetOrdinal("address")),
                    reader.GetStringOrDefault(reader.GetOrdinal("city")),
                    reader.GetStringOrDefault(reader.GetOrdinal("state")),
                    reader.GetStringOrDefault(reader.GetOrdinal("zip")),
                    reader.GetStringOrDefault(reader.GetOrdinal("mother_name")),
                    reader.GetStringOrDefault(reader.GetOrdinal("father_name")),
                    reader.GetStringOrDefault(reader.GetOrdinal("guardian_name")),
                    reader.GetStringOrDefault(reader.GetOrdinal("mother_email")),
                    reader.GetStringOrDefault(reader.GetOrdinal("father_email")),
                    reader.GetStringOrDefault(reader.GetOrdinal("guardian_email")),
                    reader.GetStringOrDefault(reader.GetOrdinal("mother_cell")),
                    reader.GetStringOrDefault(reader.GetOrdinal("father_cell")),
                    reader.GetStringOrDefault(reader.GetOrdinal("guardian_cell")),
                    reader.GetDateTimeStringOrDefault(reader.GetOrdinal("dob")),
                    reader.GetDateTimeStringOrDefault(reader.GetOrdinal("entry_date")),
                    reader.GetStringOrDefault(reader.GetOrdinal("gender")),
                    reader.GetStringOrDefault(reader.GetOrdinal("school")),
                    reader.GetStringOrDefault(reader.GetOrdinal("residence_district_name")),
                    Convert.ToUInt16(reader.GetDouble(reader.GetOrdinal("age"))),
                    reader.GetStringOrDefault(reader.GetOrdinal("ethnicity")),
                    reader.GetStringOrDefault(reader.GetOrdinal("language")),
                    reader.GetStringOrDefault(reader.GetOrdinal("guardian_relationship")),
                    reader.GetStringOrDefault(reader.GetOrdinal("sped"))
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
        public string Cell { get; }
        public string District { get; }
        public string Address { get; }
        public string City { get; }
        public string State { get; }
        public string Zip { get; }
        public string MotherName { get; }
        public string FatherName { get; }
        public string GuardianName { get; }
        public string MotherEmail { get; }
        public string FatherEmail { get; }
        public string GuardianEmail { get; }
        public string MotherCell { get; }
        public string FatherCell { get; }
        public string GuardianCell { get; }
        public string DateOfBirth { get; }
        public string EntryDate { get; }
        public string Gender { get; }
        public string School { get; }
        public string ResidenceDistrictName { get; }
        public ushort Age { get; }
        public string Ethnicity { get; }
        public string Language { get; }
        public string GuardianRelationship { get; }
        public string Sped { get; }

        public TruancyStudentInfo(
            string firstName, string lastName, string grade, string phone, string cell, string district, string address,
            string city, string state, string zip, string motherName, string fatherName, string guardianName, 
            string motherEmail, string fatherEmail, string guardianEmail, string motherCell, string fatherCell, 
            string guardianCell, string dob, string entryDate, string gender, string school, string residenceDistrictName,
            ushort age, string ethnicity, string language, string guardianRelationship, string sped
        ) {
            FirstName = firstName;
            LastName = lastName;
            Grade = grade;
            Phone = phone;
            Cell = cell;
            District = district;
            Address = address;
            City = city;
            State = state;
            Zip = zip;
            MotherName = motherName;
            FatherName = fatherName;
            GuardianName = guardianName;
            MotherEmail = motherEmail;
            FatherEmail = fatherEmail;
            GuardianEmail = guardianEmail;
            MotherCell = motherCell;
            FatherCell = fatherCell;
            GuardianCell = guardianCell;
            DateOfBirth = dob;
            EntryDate = entryDate;
            Gender = gender;
            School = school;
            ResidenceDistrictName = residenceDistrictName;
            Age = age;
            Ethnicity = ethnicity;
            Language = language;
            GuardianRelationship = guardianRelationship;
            Sped = sped;
        }
    }
}
