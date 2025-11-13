using DelEdAllotment.Data;
using DelEdAllotment.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using static System.Net.WebRequestMethods;

namespace DelEdAllotment.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmailController(AppDbContext context)
        {
            _context = context;
        }

        private string Encrypt(string clearText)
        {
            string EncryptionKey = "MA3KV2SPBANIG85C4GRTV5";
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[]
                { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76,
                  0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }

        [HttpPost("send-admitcard-emails")]
        public async Task<IActionResult> SendAdmitCardEmails()
        {
            try
            {
                const string session = "2025-26";
                const int batchSize = 2;

                int totalSuccess = 0;
                int totalFailed = 0;
                bool hasMore = true;

                // ✅ 2. AWS SES credentials
                string FROM = "info@ukdeled.com";
                string FROMNAME = " D.El.Ed. Admission Test Examination, 2025";
                string SMTP_USERNAME = "AKIAVPTQXZJTH37MZ3CO";
                string SMTP_PASSWORD = "BE2OamjqDFxItg//OkQ5oPplgz1mKGq7l8KDmJdty8Zi";
                string HOST = "email-smtp.us-east-1.amazonaws.com";
                int PORT = 587;

                using var smtpClient = new SmtpClient(HOST, PORT)
                {
                    Credentials = new NetworkCredential(SMTP_USERNAME, SMTP_PASSWORD),
                    EnableSsl = true
                };

                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // Keep looping until all emails are sent
                while (hasMore)
                {
                    // 🔹 Get or create log record
                    var log = await _context.EmailLogs
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefaultAsync(x => x.Session == session);

                    long lastSentRegNo = 0;
                    if (log != null)
                        lastSentRegNo = log.LastSentRegNo;

                    // ✅ 1. Get top batch candidates
                    var candidates = await _context.registrationBackups
                        .Where(r => r.Session == "2025-26" && r.Email != null && r.Email != "" && r.RegistrationNo > lastSentRegNo)
                        .OrderBy(r => r.RegistrationNo)
                        .Take(batchSize)
                        .ToListAsync();

                    if (!candidates.Any())
                    {
                        if (log != null)
                        {
                            log.Status = "Completed";
                            await _context.SaveChangesAsync();
                        }
                        hasMore = false;
                        break;
                    }

                    int success = 0;
                    int failed = 0;

                    // ✅ 3. Loop through each candidate and send email
                    foreach (var candidate in candidates)
                    {
                        try
                        {
                            string encryptedRegNo = Encrypt(candidate.RegistrationNo.ToString());
                            string lg = "/Bm1kUJ/FWdc7GiCk/IpCQ==";
                            string em = "/Bm1kUJ/FWdc7GiCk/IpCQ==";
                            string admitCardUrl = $"https://ukdeled.com/pdfviewer.aspx?id={WebUtility.UrlEncode(encryptedRegNo)}&lg={WebUtility.UrlEncode(lg)}&em={WebUtility.UrlEncode(em)}";
                            string subject = "डी.एल. एड. (D.EI.Ed.) प्रशिक्षण प्रवेश परीक्षा- प्रवेश पत्र";

                            string body = $@"
प्रिय {candidate.Name},
<br><br>
द्विवर्षीय डी.एल.एड. (D.EI.Ed.) प्रशिक्षण हेतु प्रवेश परीक्षा 2025 के प्रवेश पत्र उत्तराखण्ड विद्यालयी शिक्षा परिषद्, रामनगर (नैनीताल) द्वारा निर्गत कर दिए गए हैं।
<br><br>
अपना प्रवेश पत्र डाउनलोड करने के लिए कृपया <a href='https://ukdeled.com'>D.El.Ed. पोर्टल</a> पर जाएँ और अपनी पंजीकरण संख्या एवं पासवर्ड का उपयोग करें या वैकल्पिक रूप से अपना नाम और जन्म तिथि दर्ज करें।
<br><br>
आप अपना प्रवेश पत्र नीचे दिए गए लिंक पर क्लिक कर भी डाउनलोड कर सकते हैं:<br>
<a href='{admitCardUrl}'>डाउनलोड करें → डी.एल. एड. (D.EI.Ed.) प्रशिक्षण प्रवेश परीक्षा 2025 - प्रवेश पत्र</a>
<br><br>
<b>परीक्षा की तिथि :</b> 22 नवम्बर 2025 (शनिवार)<br>
<b>परीक्षा का समय :</b> 10:00 AM to 12:30 PM
<br><br>
प्रवेश पत्र से संबंधित किसी भी प्रश्न के लिए अभ्यर्थी हेल्पडेस्क न० 8090125342 या 7518245342 पर कॉल कर सकते हैं या info@ukdeled.com पर ईमेल लिख सकते हैं।
<br><br>
धन्यवाद<br>
<b>द्विवर्षीय डी.एल. एड. (D.EI.Ed.) प्रशिक्षण प्रवेश परीक्षा 2025</b><br>
उत्तराखंड विद्यालयी शिक्षा परिषद, रामनगर, नैनीताल<br>
<a href='https://ukdeled.com'>www.ukdeled.com</a>
";

                            var message = new MailMessage
                            {
                                From = new MailAddress(FROM, FROMNAME),
                                Subject = subject,
                                Body = body,
                                IsBodyHtml = true
                            };

                            message.To.Add(candidate.Email);
                            message.Bcc.Add("deled2k25@gmail.com");

                            await smtpClient.SendMailAsync(message);
                            success++;
                            Console.WriteLine($"✅ Email sent to RegNo: {candidate.RegistrationNo}, Email: {candidate.Email}");
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            Console.WriteLine($"❌ Failed to send to RegNo: {candidate.RegistrationNo}, Email: {candidate.Email}: {ex.Message}");
                        }
                    }

                    totalSuccess += success;
                    totalFailed += failed;

                    var lastCandidate = candidates.Last();
                    
                    // Create a new log entry for each batch
                    var newLog = new EmailLog
                    {
                        Session = session,
                        LastSentRegNo = (int)lastCandidate.RegistrationNo,
                        TotalSent = success,
                        LastSentAt = DateTime.Now,
                        Status = "InProgress"
                    };
                    _context.EmailLogs.Add(newLog);

                    await _context.SaveChangesAsync();
                    Console.WriteLine($"📊 Progress: Batch sent: {success}, Last RegNo: {lastCandidate.RegistrationNo}");
                }
                
                // Mark the last entry as completed
                var finalLog = await _context.EmailLogs
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync(x => x.Session == session);
                if (finalLog != null)
                {
                    finalLog.Status = "Completed";
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    Message = "✅ All emails sent successfully!",
                    TotalSuccess = totalSuccess,
                    TotalFailed = totalFailed
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }
    
    }
}
