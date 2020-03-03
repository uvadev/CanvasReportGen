using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Npgsql;

namespace CanvasReportGen {
    internal class Database : IAsyncDisposable {

        internal static bool UseSis { get; set; }
        internal static string ConnStr { private get; set; }
        internal static string CurrentYear { private get; set; }

        private NpgsqlConnection _connection;

        private Database() { }

        [CanBeNull]
        internal async Task<StudentInfo> GetInfoBySis(string sis) {
            using var query = new NpgsqlCommand(DatabaseStrings.StudentInfoQuery, _connection);
            
            query.Parameters.AddWithValue("y", CurrentYear);
            query.Parameters.AddWithValue("s", sis);
            query.Prepare();

            await using var reader = await query.ExecuteReaderAsync();
            if (await reader.ReadAsync()) {
                return new StudentInfo(
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

    internal class StudentInfo {
        public string Address { get; }
        public string City { get; }
        public string Zip { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string Grade { get; }

        public StudentInfo(string address, string city, string zip, string firstName, string lastName, string grade) {
            Address = address;
            City = city;
            Zip = zip;
            FirstName = firstName;
            LastName = lastName;
            Grade = grade;
        }
    }
}
