using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AppUtils;
using Newtonsoft.Json.Linq;
using Tomlyn.Model;
using UVACanvasAccess.ApiParts;
using UVACanvasAccess.Structures.Accounts;
using UVACanvasAccess.Structures.Courses;
using UVACanvasAccess.Structures.CustomGradebookColumns;
using UVACanvasAccess.Structures.Users;
using UVACanvasAccess.Util;
using static UVACanvasAccess.ApiParts.Api.CourseEnrollmentType;
using static UVACanvasAccess.Structures.Authentications.EventType;

namespace CanvasReportGen {
    internal static class Reports {

        private static bool IsParent(User u) => u.SisUserId?.ToLowerInvariant().Contains("pg") ?? false;
        
        private static readonly Regex CourseSisIdPattern = new Regex("^(?<year>\\d{4})~.+");

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

        internal static async Task Truancy(string token, string outPath, TomlTable truancyConfig, bool shortInterval = false) {

            if (!Database.UseSis) {
                Console.WriteLine("Please enable SIS to run the Truancy report.");
                return;
            }

            if (truancyConfig == null) {
                Console.WriteLine("Truancy config table is null.");
                return;
            }

            var sisIdYear = truancyConfig.Get<string>("sis_id_year");
            var subaccountsFilter = truancyConfig.Get<TomlArray>("subaccounts").Cast<string>().ToArray();
            
            Console.WriteLine("Running Truancy...");

            var api = new Api(token, "https://uview.instructure.com/api/v1/");
            
            var sb = new StringBuilder("user_id,sis_id,last_access,first_name,last_name,grade,phone,cell,address," +
                                       "city,state,zip,mother_name,father_name,guardian_name,mother_email,father_email,guardian_email," +
                                       "mother_cell,father_cell,guardian_cell,dob,entry_date,gender,school," +
                                       "district_of_residence,age,ethnicity,language,guardian_relationship,has_sped,has_504,failing_courses");

            await using var enumerationDb = await Database.Connect();
            await using var dataDb = await Database.Connect();

            await foreach (var sis in enumerationDb.GetTruancyCheckStudents()) {
                try {
                    var user = await api.StreamUsers(sis)
                                        .FirstOrDefaultAsync(u => u.SisUserId == sis);
                    if (user == null) {
                        Console.WriteLine($"Warning: User with sis `{sis}` does not seem to exist in Canvas.");
                        sb.Append($"\n?,{sis},indeterminate,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?");
                        continue;
                    }
                    
                    var mostRecent = await api.StreamUserPageViews(user.Id)
                                              .Where(pv => pv.Links?.RealUser == null ||
                                                           pv.Links.RealUser.Value == user.Id) // ignore masqueraded views 
                                              .Where(pv => pv.Url.Length > "https://".Length) // ignore weird blank requests
                                              .Where(pv => !string.IsNullOrWhiteSpace(pv.UserAgent)) // ignore weird blank user agents
                                              .FirstOrDefaultAsync();

                    if (!shortInterval) {
                        if (mostRecent != default && mostRecent.CreatedAt.AddDays(8) >= DateTime.Now) {
                            continue;
                        }
                    } else {
                        if (mostRecent != default &&
                            mostRecent.CreatedAt.AddDays(2) >= DateTime.Now ||
                            mostRecent.CreatedAt.AddDays(8) < DateTime.Now) {
                            continue;
                        }
                    }

                    var failingCourses = new LinkedList<Course>();

                    await foreach (var e in api.StreamUserEnrollments(user.Id, StudentEnrollment.Yield())) {
                        var course = await api.GetCourse(e.CourseId, includes: Api.IndividualLevelCourseIncludes.Term);

                        if (course.Term?.EndAt != null && course.Term.EndAt < DateTime.Now) {
                            continue;
                        }
                        
                        // Concluded enrollments are sometimes reported by the api as active, so we have to try our best
                        // to disregard "active" enrollments in courses from past years.
                        
                        if ("active" != e.EnrollmentState) {
                            continue;
                        }
                        
                        // Disregard if the course is unpublished...
                        if ("available" != course.WorkflowState) {
                            continue;
                        }
                        
                        // Disregard if no SIS
                        if (string.IsNullOrWhiteSpace(course.SisCourseId)) {
                            continue;
                        }

                        // Disregard if the SIS doesn't follow the standard course format with the current year
                        var m = CourseSisIdPattern.Match(course.SisCourseId);
                        if (!m.Success || m.Groups["year"].Value != sisIdYear) {
                            continue;
                        }

                        var subaccountNames = await GetAccountSet(api, await api.GetAccount(course.AccountId))
                                                   .ThenApply(s => s.Select(a => a?.Name));

                        if (!subaccountsFilter.Intersect(subaccountNames).Any()) {
                            continue;
                        }
                        
                        var grade = e.Grades?.CurrentGrade;
                        var score = e.Grades?.CurrentScore;

                        if (string.IsNullOrWhiteSpace(grade) && string.IsNullOrWhiteSpace(score)) {
                            failingCourses.AddLast(course);
                        } else if ("F" == grade) {
                            failingCourses.AddLast(course);
                        } else if (double.TryParse(score, out var nScore) && nScore <= 66) {
                            failingCourses.AddLast(course);
                        }
                    }

                    if (!failingCourses.Any()) {
                        continue;
                    }

                    var dtStr = mostRecent?.CreatedAt.ToString("yyyy-MM-dd'T'HH':'mm':'ssK") ?? "never";
                    var data = await dataDb.GetTruancyInfo(sis);
                    var failingCourseNames = "\"" + string.Join(";", failingCourses.Select(c => c.Name).Distinct()) + "\"";
                    
                    sb.Append($"\n{user.Id},{sis},{dtStr},{data.FirstName},{data.LastName},{data.Grade},{data.Phone}," +
                              $"{data.Cell},{data.Address},{data.City},{data.State},{data.Zip},{data.MotherName}," +
                              $"{data.FatherName},{data.GuardianName},{data.MotherEmail},{data.FatherEmail}," +
                              $"{data.GuardianEmail},{data.MotherCell},{data.FatherCell},{data.GuardianCell}," +
                              $"{data.DateOfBirth},{data.EntryDate},{data.Gender},{data.School}," +
                              $"{data.ResidenceDistrictName},{data.Age},{data.Ethnicity},{data.Language}," +
                              $"\"{data.GuardianRelationship}\",{data.HasSped},{data.Has504},{failingCourseNames}");
                } catch (Exception e) {
                    Console.WriteLine($"Warning: exception during user with sis `{sis}`\n{e}");
                }
            }
            
            File.WriteAllText(outPath, sb.ToString());
            Console.WriteLine($"Wrote report to {outPath} at {DateTime.Now:HH':'mm':'ss}");
        }

        private static async Task<HashSet<Account>> GetAccountSet(Api api, Account a) {
            var set = new HashSet<Account> { a };
            while (a.ParentAccountId.HasValue) {
                a = await api.GetAccount(a.ParentAccountId.Value);
                set.Add(a);
            }

            return set;
        }

        internal static async Task GradeLevels(string token, string outPath, bool syncToCanvas = false) {
            if (!Database.UseSis) {
                Console.WriteLine("Please enable SIS to run the Grade Levels report.");
                return;
            }
            
            Console.WriteLine("Running Grade Levels..");

            var api = new Api(token, "https://uview.instructure.com/api/v1/");
            
            var sb = new StringBuilder("user_id,sis_id,grade");
            
            var updateDict = new Dictionary<ulong, HashSet<Api.ColumnEntryUpdate>>();

            //await using var db = await Database.Connect();
            await using (var db = await Database.Connect()) {
                await foreach (var (sis, grade) in db.GetGradeLevels()) {
                    try {
                        var user = await api.GetUserBySis(sis);
                        if (user == null) {
                            Console.WriteLine($"Warning: user with sis {sis} not in canvas.");
                            continue;
                        }
                        sb.Append($"\n{user.Id},{sis},{grade}");

                        if (!syncToCanvas) {
                            continue;
                        }

                        try {
                            await api.StoreCustomJson("academy.uview.tech", "meta", JObject.FromObject(new {
                                gradeLevel = grade,
                                canvasId = user.Id,
                                sisId = sis,
                                updatedOn = DateTime.Now.ToIso8601Date()
                            }));
                        } catch (Exception e) {
                            Console.WriteLine($"Warning: exception during userdata sync for user with sis `{sis}`\n{e}");
                        }
                        
                        try {
                            await foreach (var e in api.StreamUserEnrollments(user.Id,
                                                                              StudentEnrollment.Yield(), 
                                                                              Api.CourseEnrollmentState.Active.Yield())) {
                                updateDict.GetOrConstruct(e.CourseId).Add(new Api.ColumnEntryUpdate {
                                    UserId = user.Id,
                                    ColumnId = await api.GetOrCreateColumn(e.CourseId, "Grade_Level")
                                                        .ThenApply(col => col.Id),
                                    Content = grade
                                });
                            }
                        } catch (Exception e) {
                            Console.WriteLine($"Warning: exception during column update setup for user with sis `{sis}`\n{e}");
                        }
                    } catch (Exception e) {
                        Console.WriteLine($"Warning: exception during user with sis `{sis}`\n{e}");
                    }
                }
            }
            
            if (syncToCanvas) {
                try {
                    foreach (var (courseId, updates) in updateDict) {
                        await api.UpdateCustomColumnEntries(courseId, updates);
                    }
                } catch (Exception e) {
                    Console.WriteLine($"Warning: exception during bulk update\n{e}");
                }
            }
            
            File.WriteAllText(outPath, sb.ToString());
            Console.WriteLine($"Wrote report to {outPath} at {DateTime.Now:HH':'mm':'ss}");
        }

        private static async Task<CustomColumn> GetOrCreateColumn(this Api api, ulong courseId, string name) {
            var old = await api.StreamCustomGradebookColumns(courseId)
                               .FirstOrDefaultAsync(col => name == col.Title);
            
            if (old != default) {
                return old;
            }
            
            return await api.CreateCustomColumn(courseId, name);
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
