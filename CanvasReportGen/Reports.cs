using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVACanvasAccess.ApiParts;
using static UVACanvasAccess.Structures.Authentications.EventType;

namespace CanvasReportGen {
    internal static class Reports {
        internal static async Task ZeroLogins(string token, string outPath) {

            Console.WriteLine("Running Zero Logins...");

            var api = new Api(token, "https://uview.instructure.com/api/v1/");
            
            var sb = new StringBuilder("user_id,sis_id");
            
            await foreach (var user in api.StreamUsers()
                                          .Where(u => !u.SisUserId?.Contains("pG") ?? true)
                                          .WhereAwait(async u => !await u.IsTeacher())) {

                if (!await api.StreamUserAuthenticationEvents(user.Id).Where(e => e.Event == Login).AnyAsync()) {
                    sb.Append($"\n{user.Id},{user.SisUserId ?? "?"}");
                }
            }
            
            File.WriteAllText(outPath, sb.ToString());
            Console.WriteLine($"Wrote report to {outPath}");
        }
    }
}
