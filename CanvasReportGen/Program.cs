using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using AppUtils;
using Tomlyn.Syntax;

namespace CanvasReportGen {
    internal static class Program {
        
        public static async Task Main(string[] args) {
            var selected = -1;
            if (args.Length > 0) {
                int.TryParse(args[0], out selected);
            }
            
            var home = new AppHome("canvas_report_gen");
            
            Console.WriteLine($"Using config path: {home.ConfigPath}");
            
            if (!home.ConfigPresent()) {
                Console.WriteLine("Need to generate a config file.");

                home.CreateConfig(new DocumentSyntax {
                    Tables = {
                        new TableSyntax("tokens") {
                            Items = {
                                {"token", "PUT_TOKEN_HERE"}
                            }
                        },
                        new TableSyntax("sis") {
                            Items = {
                                {"use_sis", false},
                                {"current_year", "2021"},
                                {"conn_str", "PUT_CONN_STR_HERE"}
                            }
                        },
                        new TableSyntax("truancy") {
                            Items = {
                                {"sis_id_year", "2020"},
                                {"subaccounts", new string[] {}}
                            }
                        }
                    }
                });

                Console.WriteLine("Created a new config file. Please go put in your token.");
                return;
            }

            Console.WriteLine("Found config file.");

            var config = home.GetConfig();

            Debug.Assert(config != null, nameof(config) + " != null");

            var token = config.GetTable("tokens")
                              .Get<string>("token");

            var sisTable = config.GetTable("sis");
            
            Database.UseSis = sisTable.Get<bool>("use_sis");

            if (Database.UseSis) {
                Database.ConnStr = sisTable.Get<string>("conn_str");
                Database.CurrentYear = sisTable.Get<string>("current_year");
            }

            var emailTable = config.GetTable("email");

            var started = DateTime.Now;
            
            var outPath = Path.Combine(home.NsDir, "{0}" + $"_{started.Ticks}.csv");
            
            for (;;) {
                Console.WriteLine("Which report? (* = needs SIS)");
                Console.WriteLine("1: Zero Logins");
                Console.WriteLine("2: Last Activity");
                Console.WriteLine("3: *Truancy");
                Console.WriteLine("4: *Truancy (Short Interval)");
                Console.Write("?> ");
                await Console.Out.FlushAsync();

                if (selected > -1 || int.TryParse(Console.ReadLine(), out selected)) {
                    switch (selected) {
                        case 1:
                            await Reports.ZeroLogins(token, string.Format(outPath, "ZeroLogins"));
                            Mailman.SendReport(emailTable, "ZeroLogins", outPath, started);
                            return;
                        case 2:
                            await Reports.LastActivity(token, string.Format(outPath, "LastActivity"));
                            Mailman.SendReport(emailTable, "LastActivity", outPath, started);
                            return;
                        case 3:
                            await Reports.Truancy(token, 
                                                  string.Format(outPath, "Truancy"),
                                                  config.GetTable("truancy"));
                            Mailman.SendReport(emailTable, "Truancy", outPath, started);
                            return;
                        case 4:
                            await Reports.Truancy(token, 
                                                  string.Format(outPath, "TruancyShort"),
                                                  config.GetTable("truancy"), 
                                                  true);
                            Mailman.SendReport(emailTable, "TruancyShort", outPath, started);
                            return;
                        case 0:
                            File.WriteAllText(string.Format(outPath, "Dummy"), "dummy1,dummy2\na,b\n");
                            Mailman.SendReport(emailTable, "Dummy", outPath, started);
                            return;
                        default:
                            Console.WriteLine("Please choose a report.\n");
                            break;
                    }
                } else {
                    Console.WriteLine("Please choose a report.\n");
                }
            }
        }
    }
}
