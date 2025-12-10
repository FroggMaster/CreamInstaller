using System;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CreamInstaller.Forms;

namespace CreamInstaller.Utility;

internal static class ExceptionHandler
{
    internal static string FormatException(this Exception e)
    {
        StringBuilder output = new();
        int stackDepth = 0;
        while (e is not null)
        {
            if (stackDepth > 10)
                break;
            if (output.Length > 0)
                _ = output.Append("\n\n");
            string[] stackTrace = e.StackTrace?.Split('\n');
            if (stackTrace is not null && stackTrace.Length > 0)
            {
                _ = output.Append(CultureInfo.CurrentCulture, $"[{e.HResult & 0x0000FFFF}] {e.GetType()}: {e.Message}");
                foreach (string line in stackTrace)
                {
                    int atNum = line.IndexOf("at ", StringComparison.Ordinal);
                    int inNum = line.IndexOf("in ", StringComparison.Ordinal);
                    int ciNum = line.LastIndexOf(@"CreamInstaller\", StringComparison.Ordinal);
                    int lineNum = line.LastIndexOf(":line ", StringComparison.Ordinal);
                    if (atNum != -1)
                        _ = output.Append("\n    " + (inNum != -1 ? line[atNum..(inNum - 1)] : line[atNum..]) +
                                          (inNum != -1
                                              ? "\n        " + (ciNum != -1
                                                  ? "in " + (lineNum != -1
                                                      ? line[ciNum..lineNum] + "\n            on " +
                                                        line[(lineNum + 1)..]
                                                      : line[ciNum..])
                                                  : line[inNum..])
                                              : null));
                }
            }

            e = e.InnerException;
            stackDepth++;
        }

        return output.ToString();
    }

    internal static string GetUserFriendlyMessage(this Exception e)
    {
        return e switch
        {
            UnauthorizedAccessException => "Access denied. Try running as administrator or check file permissions.",
            System.IO.FileNotFoundException fnf => $"File not found: {fnf.FileName ?? "Unknown file"}",
            System.IO.DirectoryNotFoundException => "Directory not found. The game may have been moved or uninstalled.",
            System.IO.IOException io when io.Message.Contains("being used") => "A file is in use by another program. Close any running games and try again.",
            System.IO.IOException => "A file operation failed. Check that the disk is not full and files are accessible.",
            System.Net.Http.HttpRequestException => "Network error. Check your internet connection and try again.",
            TaskCanceledException => "The operation timed out. The server may be slow or unavailable.",
            OperationCanceledException => "The operation was cancelled.",
            InvalidOperationException => $"Invalid operation: {e.Message}",
            _ => null
        };
    }

    internal static bool HandleException(this Exception e, Form form = null, string caption = null,
        string acceptButtonText = "Retry",
        string cancelButtonText = "Cancel")
    {
        caption ??= Program.Name + " encountered an exception";

        string userFriendlyMessage = e.GetUserFriendlyMessage();
        string technicalDetails = e.FormatException();

        string outputString;
        if (userFriendlyMessage is not null)
        {
            outputString = userFriendlyMessage;
            if (!string.IsNullOrWhiteSpace(technicalDetails))
                outputString += "\n\n--- Technical Details ---\n" + technicalDetails;
        }
        else
        {
            outputString = !string.IsNullOrWhiteSpace(technicalDetails)
                ? technicalDetails
                : e?.ToString() ?? "Unknown exception";
        }

        using DialogForm dialogForm = new(form ?? Form.ActiveForm);
        return dialogForm.Show(SystemIcons.Error, outputString, acceptButtonText, cancelButtonText, caption) is
            DialogResult.OK;
    }

    internal static void HandleFatalException(this Exception e)
    {
        e.HandleException(caption: Program.Name + " encountered a fatal exception", acceptButtonText: "OK",
            cancelButtonText: null);
        Application.Exit();
    }
}

public class CustomMessageException : Exception
{
    public CustomMessageException(string message) : base(message) => Message = message;

    public override string Message { get; }

    public override string ToString() => Message;
}