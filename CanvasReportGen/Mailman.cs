using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AppUtils;
using MailKit.Net.Smtp;
using MimeKit;
using Tomlyn.Model;

namespace CanvasReportGen {
    
    internal class Mailman : IDisposable {
        private readonly SmtpClient _client;
        private readonly string _senderEmail, _senderName, _oopsEmail, _oopsName;
        private readonly List<(string Name, string Email)> _recipients;
        private static readonly TextInfo TextInfo = new CultureInfo("en-US", false).TextInfo;

        private Mailman(TomlTable emailTable) {
            _client = new SmtpClient();
            _client.Connect(emailTable.Get<string>("server"), Convert.ToInt32(emailTable.Get<long>("port")));
            _client.AuthenticationMechanisms.Remove("XOAUTH2");
            _client.Authenticate(emailTable.Get<string>("login"), emailTable.Get<string>("pass"));
            _senderEmail = emailTable.Get<string>("login");
            _senderName = emailTable.Get<string>("sender_name");
            _oopsEmail = emailTable.Get<string>("oops_email");
            _oopsName = emailTable.Get<string>("oops_name");

            _recipients = emailTable.Get<TomlTableArray>("recipients")
                                    .Select(r => (r.Get<string>("name"), r.Get<string>("email")))
                                    .ToList();
        }

        private void SendOops(string reportName, string path, DateTime started, string what) {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_senderName, _senderEmail));
            msg.To.Add(new MailboxAddress(_oopsName, _oopsEmail));
            msg.Subject = $"OOPS: Failure sending {reportName}";
            msg.Body = new BodyBuilder {
                TextBody = $"Report Type: {reportName}\nPath: {path}\nStarted: {started:u}\nWhat: {what}"
            }.ToMessageBody();
            _client.Send(msg);
        }

        private void SendCsv(string reportName, string path, DateTime started) {
            if (!File.Exists(path)) {
                SendOops(reportName, path, started, "The file didn't exist.");
                return;
            }
            
            var msg = new MimeMessage();
            
            msg.From.Add(new MailboxAddress(_senderName, _senderEmail));
            msg.Subject = $"Automated Report: {reportName} [{DateTime.Today:d}]";
            
            foreach (var (name, email) in _recipients) {
                msg.To.Add(new MailboxAddress(name, email));
            }

            var body = new BodyBuilder {
                TextBody = $"The CSV \"{Path.GetFileName(path)}\" is attached.\n" +
                           $"Report Started: {started:g} (server time)\n" +
                           $"Report Finished: {DateTime.Now:g} (server time)\n\n" +
                           $"For issues with the report contents, please email <sosborne@uview.academy>\n" +
                           $"If the report is not attached, or other such issues, please email <mmorris@uview.academy>"
            };

            body.Attachments.Add(path);
            msg.Body = body.ToMessageBody();
            
            _client.Send(msg);
        }

        public void Dispose() {
            _client?.Dispose();
        }

        internal static void SendReport(TomlTable emailTable, string reportName, string path, DateTime started) {
            path = string.Format(path, reportName);
            if (!emailTable.GetOr("send_email", false)) {
                return;
            }
            using var mailman = new Mailman(emailTable);
            mailman.SendCsv(reportName, path, started);
        }
    }
}
