using System;
using System.Collections.Generic;
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

        internal static V GetOrConstruct<K, V>(this Dictionary<K, V> dict, K key) where V: new() {
            if (dict.TryGetValue(key, out var r)) {
                return r;
            }

            r = new V();
            dict[key] = r;
            return r;
        }

        internal static V GetOrCompute<K, V>(this Dictionary<K, V> dict, K key, Func<V> fn) {
            if (dict.TryGetValue(key, out var r)) {
                return r;
            }

            r = fn();
            dict[key] = r;
            return r;
        }
    }
}
