using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ContactUs
{
    private const string ToMail = "100wodud@gmail.com";
    private const string Subject = "[Color Block Zone] Contact Us";

    public void Contact() => Contact(string.Empty);

    public void Contact(string feedback)
    {
        string body = string.IsNullOrEmpty(feedback)
            ? GetInformation().ToString()
            : feedback + "\n\n" + GetInformation();

        OpenMailClient(ToMail, EscapeURL(Subject), EscapeURL(body + "\n\n"));
    }

    private static StringBuilder GetInformation()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Version_Platform: {Application.version}_{UnityEngine.Device.Application.platform}");
        builder.AppendLine($"Level: {PlayerData.ClearLevel}\n");
        builder.AppendLine("Device Model: " + SystemInfo.deviceModel);
        builder.AppendLine("Operating System: " + SystemInfo.operatingSystem);
        builder.AppendLine("Graphics Device Name: " + SystemInfo.graphicsDeviceName);
        return builder;
    }

    private static string EscapeURL(string url) => UnityWebRequest.EscapeURL(url).Replace("+", "%20");

    private static void OpenMailClient(string toMail, string subject, string body)
        => Application.OpenURL($"mailto:{toMail}?subject={subject}&body={body}");
}
