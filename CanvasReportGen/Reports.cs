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

        internal static async Task Truancy(string token, string outPath) {

            if (!Database.UseSis) {
                Console.WriteLine("Please enable SIS to run the Truancy report.");
                return;
            }
            
            Console.WriteLine("Running Truancy...");

            var api = new Api(token, "https://uview.instructure.com/api/v1/");
            
            var sb = new StringBuilder("user_id,sis_id,last_access,first_name,last_name,grade,phone,district,address," +
                                       "city,state,zip,mother_name,father_name,guardian_name,mother_email,father_email,guardian_email," +
                                       "mother_cell,father_cell,guardian_cell,dob,entry_date,gender,school,residence_district_code," +
                                       "residence_district_name");

            await using var enumerationDb = await Database.Connect();
            await using var dataDb = await Database.Connect();

            await foreach (var sis in enumerationDb.GetTruancyCheckStudents()) {
                try {
                    var user = await api.StreamUsers(sis)
                                        .FirstOrDefaultAsync(u => u.SisUserId == sis);
                    if (user == null) {
                        Console.WriteLine($"Warning: User with sis `{sis}` does not seem to exist in Canvas.");
                        sb.Append($"\n?,{sis},indeterminate,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?");
                        continue;
                    }
                    
                    var mostRecent = await api.StreamUserPageViews(user.Id)
                                              .Where(pv => pv.Links?.RealUser == null ||
                                                           pv.Links.RealUser.Value == user.Id) // ignore masqueraded views 
                                              .Where(pv => pv.Url.Length > "https://".Length) // ignore weird blank requests
                                              .Where(pv => !string.IsNullOrWhiteSpace(pv.UserAgent)) // ignore weird blank user agents
                                              .FirstOrDefaultAsync();

                    if (mostRecent != default && mostRecent.CreatedAt.AddDays(8) >= DateTime.Now) {
                        continue;
                    }
                    
                    var dtStr = mostRecent?.CreatedAt.ToString("yyyy-MM-dd'T'HH':'mm':'ssK") ?? "never";
                    var data = await dataDb.GetTruancyInfo(sis);
                    
                    sb.Append($"\n{user.Id},{sis},{dtStr},{data.FirstName},{data.LastName},{data.Grade},{data.Phone}," +
                              $"{data.District},{data.Address},{data.City},{data.State},{data.Zip},{data.MotherName}," +
                              $"{data.FatherName},{data.GuardianName},{data.MotherEmail},{data.FatherEmail}," +
                              $"{data.GuardianEmail},{data.MotherCell},{data.FatherCell},{data.GuardianCell}," +
                              $"{data.DateOfBirth},{data.EntryDate},{data.Gender},{data.School},{data.ResidenceDistrictCode}," +
                              $"{data.ResidenceDistrictName}");
                } catch (Exception e) {
                    Console.WriteLine($"Warning: exception during user with sis `{sis}`\n{e}");
                }
            }
            
            File.WriteAllText(outPath, sb.ToString());
            Console.WriteLine($"Wrote report to {outPath} at {DateTime.Now:HH':'mm':'ss}");
        }
        
        internal static async Task TruancyFromLogins(string token, string outPath) {

            if (!Database.UseSis) {
                Console.WriteLine("Please enable SIS to run the Truancy report.");
                return;
            }
            
            Console.WriteLine("Running Truancy from Logins...");

            var api = new Api(token, "https://uview.instructure.com/api/v1/");
            
            var sb = new StringBuilder("user_id,sis_id,last_access,first_name,last_name,grade,phone,district,address," +
                                       "city,state,zip,mother_name,father_name,mother_email,father_email,mother_cell,father_cell");

            await using var enumerationDb = await Database.Connect();
            await using var dataDb = await Database.Connect();

            await foreach (var sis in enumerationDb.GetTruancyCheckStudents()) {
                try {
                    var user = await api.StreamUsers(sis)
                                        .FirstOrDefaultAsync(u => u.SisUserId == sis);
                    if (user == null) {
                        Console.WriteLine($"Warning: User with sis `{sis}` does not seem to exist in Canvas.");
                        sb.Append($"\n?,{sis},indeterminate,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?");
                        continue;
                    }

                    var mostRecent = await api.StreamUserAuthenticationEvents(user.Id)
                                              .Where(e => e.Event == Login)
                                              .FirstOrDefaultAsync();

                    if (mostRecent != default && mostRecent.CreatedAt.AddDays(8) >= DateTime.Now) {
                        continue;
                    }
                    
                    var dtStr = mostRecent?.CreatedAt.ToString("yyyy-MM-dd'T'HH':'mm':'ssK") ?? "never";
                    var data = await dataDb.GetTruancyInfo(sis);
                    
                    sb.Append($"\n{user.Id},{sis},{dtStr},{data.FirstName},{data.LastName},{data.Grade},{data.Phone}," +
                              $"{data.District},{data.Address},{data.City},{data.State},{data.Zip},{data.MotherName}," +
                              $"{data.FatherName},{data.MotherEmail},{data.FatherEmail},{data.MotherCell},{data.FatherCell}");
                } catch (Exception e) {
                    Console.WriteLine($"Warning: exception during user with sis `{sis}`\n{e}");
                }
            }
            
            File.WriteAllText(outPath, sb.ToString());
            Console.WriteLine($"Wrote report to {outPath}");
        }
    }
}
