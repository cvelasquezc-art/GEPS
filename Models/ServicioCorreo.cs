using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace GEPS.Models
{
    public class ServicioCorreo
    {
        public static bool Enviar(string destinatario, string asunto, string cuerpo)
        {
            try
            {
                string host = ConfigurationManager.AppSettings["SmtpHost"];
                int puerto = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
                string correo = ConfigurationManager.AppSettings["SmtpCorreo"];
                string clave = ConfigurationManager.AppSettings["SmtpClave"];

                var mensaje = new MailMessage();
                mensaje.From = new MailAddress(correo, "GEPS – Gestión de Semilleros");
                mensaje.To.Add(destinatario);
                mensaje.Subject = asunto;
                mensaje.Body = cuerpo;
                mensaje.IsBodyHtml = true;

                var smtp = new SmtpClient(host, puerto);
                smtp.Credentials = new NetworkCredential(correo, clave);
                smtp.EnableSsl = true;
                smtp.Send(mensaje);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
    
