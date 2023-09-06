using System.Net;
using System.Net.Mail;
using UAManagedCore;
using FTOptix.UI;

public class EmailSenderLogic : FTOptix.NetLogic.BaseNetLogic {
    [FTOptix.NetLogic.ExportMethod]
    public void SendEmail(string mailToAddress, string mailSubject, string mailBody) {
        if (!InitializeAndValidateSMTPParameters())
            return;

        if (!ValidateEmail(mailToAddress, mailSubject, mailBody))
            return;

        var fromAddress = new MailAddress(senderAddress, "Name"); // Email Sender
        var toAddress = new MailAddress(mailToAddress, "Name"); // Email Reciver

        var smtpClient = new SmtpClient {
            Host = smtpHostname,
            Port = smtpPort,
            EnableSsl = enableSSL,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromAddress.Address, senderPassword)
        };

        using (var message = new MailMessage(fromAddress, toAddress) {
            // Create the message.
            Subject = mailSubject,
            Body = mailBody
        }) {
            // Add reply-to address
            message.ReplyToList.Add(mailToAddress);

            try {
                // Send email message
                smtpClient.Send(message);
            } catch (SmtpException e) {
                Log.Error($"Email failed to send : {e.StatusCode}");
            }
        }

    }

    private bool InitializeAndValidateSMTPParameters() {
        senderAddress = (string)GetVariableValue("SenderEmailAddress").Value;
        if (string.IsNullOrEmpty(senderAddress)) {
            Log.Error("Invalid Sender Email address");
            return false;
        }

        senderPassword = (string)GetVariableValue("SenderEmailPassword").Value;
        if (string.IsNullOrEmpty(senderPassword)) {
            Log.Error("Invalid sender password");
            return false;
        }

        smtpHostname = (string)GetVariableValue("SMTPHostname").Value;
        if (string.IsNullOrEmpty(smtpHostname)) {
            Log.Error("Invalid SMTP hostname");
            return false;
        }

        smtpPort = (int)GetVariableValue("SMTPPort").Value;
        enableSSL = (bool)GetVariableValue("EnableSSL").Value;

        return true;
    }


    private UAVariable GetVariableValue(string variableName) {
        var variable = LogicObject.Get<UAVariable>(variableName);
        if (variable == null) {
            Log.Error($"{variableName} not found");
            return null;
        }
        return variable;
    }

    private bool ValidateEmail(string recieverEmail, string emailSubject, string emailBody) {

        if (string.IsNullOrEmpty(emailSubject)) {
            Log.Error("Email subject is empty or malformed");
            return false;
        }

        if (string.IsNullOrEmpty(emailBody)) {
            Log.Error("Email body is empty or malformed");
            return false;
        }

        if (string.IsNullOrEmpty(recieverEmail)) {
            Log.Error("RecieverEmail is empty or null");
            return false;
        }
        return true;
    }

    string senderAddress;
    string senderPassword;
    string smtpHostname;
    int smtpPort;
    bool enableSSL;
}
