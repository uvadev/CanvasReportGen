using Npgsql;

namespace CanvasReportGen {
    internal static class Util {
        internal static string GetStringOrDefault(this NpgsqlDataReader reader, int ordinal, string @default = "?") {
            return reader.IsDBNull(ordinal) ? @default
                                            : reader.GetString(ordinal);
        }
        
        internal static string GetDateTimeStringOrDefault(this NpgsqlDataReader reader, int ordinal, string @default = "?") {
            return reader.IsDBNull(ordinal) ? @default
                                            : reader.GetDateTime(ordinal).ToString("yyyy-MM-dd'T'HH':'mm':'ssK");
        }
    }
}
