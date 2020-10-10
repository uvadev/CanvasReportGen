using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using AppUtils;
using Tomlyn.Syntax;

namespace CanvasReportGen {
    internal static class Program {
        
        public static async Task Main(string[] args) {
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

            var started = DateTime.Now;
            
            var outPath = Path.Combine(home.NsDir, "{0}" + $"_{started.Ticks}.csv");

            for (;;) {
                Console.WriteLine("Which report? (* = needs SIS; ! = work in progress)");
                Console.WriteLine("1: Zero Logins");
                Console.WriteLine("2: Last Activity");
                Console.WriteLine("3: Truancy *!");
                //Console.WriteLine("4: Truancy from Logins *!");
                Console.Write("?> ");
                await Console.Out.FlushAsync();

                if (int.TryParse(Console.ReadLine(), out var n)) {
                    switch (n) {
                        case 1:
                            await Reports.ZeroLogins(token, string.Format(outPath, "ZeroLogins"));
                            return;
                        case 2:
                            await Reports.LastActivity(token, string.Format(outPath, "LastActivity"));
                            return;
                        case 3:
                            await Reports.Truancy(token, string.Format(outPath, "Truancy"), config.GetTable("truancy"));
                            return;
                        //case 4:
                        //    await Reports.TruancyFromLogins(token, string.Format(outPath, "TruancyFromLogins"));
                        //    return;
                        default:
                            Console.WriteLine("no\n");
                            break;
                    }
                } else {
                    Console.WriteLine("no\n");
                }
            }
        }
    }
}
