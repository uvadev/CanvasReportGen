using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVACanvasAccess.ApiParts;
using UVACanvasAccess.Structures.Users;
using static UVACanvasAccess.Structures.Authentications.EventType;

namespace CanvasReportGen {
    internal static class Reports {

        private static bool IsParent(User u) => u.SisUserId?.ToLowerInvariant().Contains("pg") ?? false;

        internal static async Task ZeroLogins(string token, string outPath) {

            Console.WriteLine("Running Zero Logins...");

            var api = new Api(token, "https://uview.instructure.com/api/v1/");
            
            var sb = new StringBuilder("user_id,sis_id,address,city,zip,first_name,last_name,grade");

            await using var db = Database.UseSis ? await Database.Connect() 
                                                 : null;
            
            await foreach (var user in api.StreamUsers()
                                          .Where(u => !IsParent(u))
                                          .WhereAwait(async u => !await u.IsTeacher())) {
                try {
                    if (await api.StreamUserAuthenticationEvents(user.Id)
                                 .Where(e => e.Event == Login)
                                 .AnyAsync()) { continue; }
                    
                    sb.Append($"\n{user.Id},{user.SisUserId ?? "?"}");

                    if (Database.UseSis && user.SisUserId != null) {
                        var data = await db.GetInfoBySis(user.SisUserId);
                        if (data != null) {
                            sb.Append($",{data.Address},{data.City},{data.Zip},{data.FirstName},{data.LastName},{data.Grade}");
                        } else {
                            sb.Append(",?,?,?,?,?,?");
                        }
                    } else {
                        sb.Append(",?,?,?,?,?,?");
                    }
                } catch (Exception e) {
                    Console.WriteLine($"Warning: exception during user {user.Id}\n{e}");
                }
            }
            
            File.WriteAllText(outPath, sb.ToString());
            Console.WriteLine($"Wrote report to {outPath}");
        }

        internal static async Task LastActivity(string token, string outPath) {

            Console.WriteLine("Running Last Activity...");
            
            var api = new Api(token, "https://uview.instructure.com/api/v1/");

            var sb = new StringBuilder("user_id,sis_id,date,url,address,city,zip,first_name,last_name,grade");
            
            await using var db = Database.UseSis ? await Database.Connect() 
                                                 : null;

            await foreach (var user in api.StreamUsers()
                                          .Where(u => !IsParent(u))
                                          .WhereAwait(async u => !await u.IsTeacher())) {
                try {
                    var first = await api.StreamUserPageViews(user.Id)
                                         .SkipWhile(pv => (pv.Links?.RealUser.HasValue ?? false) &&
                                                           pv.Links.RealUser.Value != user.Id) // ignore masqueraded views 
                                         .FirstAsync();

                    sb.Append($"\n{user.Id},{user.SisUserId ?? "?"},{first.CreatedAt},\"{first.Url}\"");

                    if (Database.UseSis && user.SisUserId != null) {
                        var data = await db.GetInfoBySis(user.SisUserId);
                        if (data != null) {
                            sb.Append($",{data.Address},{data.City},{data.Zip},{data.FirstName},{data.LastName},{data.Grade}");
                        } else {
                            sb.Append(",?,?,?,?,?,?");
                        }
                    } else {
                        sb.Append(",?,?,?,?,?,?");
                    }
                    
                } catch (InvalidOperationException) {
                    Console.WriteLine($"Note: No activity for {user.Id}");
                } catch (Exception e) {
                    Console.WriteLine($"Warning: exception during user {user.Id}\n{e}");
                }
            }
            
            File.WriteAllText(outPath, sb.ToString());
            Console.WriteLine($"Wrote report to {outPath}");
        }
    }
}
