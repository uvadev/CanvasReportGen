using Npgsql;

namespace CanvasReportGen {
    internal static class Util {
        internal static string GetStringOrDefault(this NpgsqlDataReader reader, int ordinal, string @default = "?") {
            return reader.IsDBNull(ordinal) ? @default
                                            : reader.GetString(ordinal);
        }
    }
}
