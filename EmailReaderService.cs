using MailKit.Net.Imap;
using MailKit.Security;
using MimeKit;

namespace MyWorkflow;
public class EmailReaderService
{
    public async Task<List<MimeMessage>> GetEmailsAsync(string host, int port, string username, string password)
    {
        var emails = new List<MimeMessage>();

        using (var client = new ImapClient())
        {
            await client.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(username, password);

            var inbox = client.Inbox;
            await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly);

            for (int i = 0; i < inbox.Count; i++)
            {
                var message = await inbox.GetMessageAsync(i);
                emails.Add(message);
            }

            await client.DisconnectAsync(true);
        }

        return emails;
    }
}
