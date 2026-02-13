using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace VstsDemoBuilder.Models
{
	public class Email
	{
		public string EmailAddress { get; set; }
		public string AccountName { get; set; }
		public string ErrorLog { get; set; }

		public bool SendEmail(string toEmail, string body, string subject)
		{
            MailMessage newmsg = new MailMessage(VstsDemoBuilder.Infrastructure.AppSettings.Get("EmailFrom"), toEmail)
            {
                //newmsg.From = new MailAddress(VstsDemoBuilder.Infrastructure.AppSettings.Get("from"));
                IsBodyHtml = true,
                Subject = subject,

                //newmsg.To.Add(toEmail);
                Body = body
            };
            SmtpClient smtp = new SmtpClient
            {

                //smtp.Host = Convert.ToString(VstsDemoBuilder.Infrastructure.AppSettings.Get("mailhost"));
                Host = "smtp.gmail.com",
                Port = 587,
                //smtp.Port = Convert.ToInt16(VstsDemoBuilder.Infrastructure.AppSettings.Get("port"));
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential
          (Convert.ToString(VstsDemoBuilder.Infrastructure.AppSettings.Get("EmailUsername")), Convert.ToString(VstsDemoBuilder.Infrastructure.AppSettings.Get("EmailPassword"))),

                EnableSsl = Convert.ToBoolean(VstsDemoBuilder.Infrastructure.AppSettings.Get("EmailEnableSSL"))
            };
            try
			{
				smtp.Send(newmsg);
			}
			catch (Exception)
			{
				return false;
			}
			return true;
		}
	}
}

