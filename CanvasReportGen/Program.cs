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

            var started = DateTime.Now;
            
            var outPath = Path.Combine(home.NsDir, "{0}" + $"_{started.Ticks}.csv");

            for (;;) {
                Console.WriteLine("Which report?");
                Console.WriteLine("1: Zero Logins");
                Console.Write("?> ");
                Console.Out.Flush();

                if (int.TryParse(Console.ReadLine(), out var n)) {
                    switch (n) {
                        case 1:
                            await Reports.ZeroLogins(token, string.Format(outPath, "ZeroLogins"));
                            return;
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
