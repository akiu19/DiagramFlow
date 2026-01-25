namespace DiagramFlow.Services
{
    public interface ISystemClipboard
    {
        void SetText(string text);
        string GetText();
        bool ContainsText();
    }

    public class SystemClipboardWrapper : ISystemClipboard
    {
        public void SetText(string text) => System.Windows.Clipboard.SetText(text);
        public string GetText() => System.Windows.Clipboard.GetText();
        public bool ContainsText() => System.Windows.Clipboard.ContainsText();
    }
}
