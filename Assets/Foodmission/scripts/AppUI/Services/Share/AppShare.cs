namespace eu.foodmission.platform
{
    /// <summary>
    /// "Share the app" content: localized invitation followed by the web page with the store download links.
    /// </summary>
    public static class AppShare
    {
        public const string DownloadUrl = "https://www.foodmission.eu/platform/";

        public static ShareContent Build(string message, string subject)
        {
            string text = string.IsNullOrWhiteSpace(message)
                ? DownloadUrl
                : message.Trim() + "\n" + DownloadUrl;
            return new ShareContent
            {
                Text = text,
                Subject = subject
            };
        }
    }
}
