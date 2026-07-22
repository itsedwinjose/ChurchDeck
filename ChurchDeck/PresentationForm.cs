using System.Drawing;

namespace ChurchDeck;

internal sealed class PresentationForm : Form
{
    private readonly Panel _surface = new() { Dock = DockStyle.Fill, Padding = new Padding(60) };
    private readonly Label _content = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false };

    public PresentationForm()
    {
        Text = "ChurchDeck Presentation";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        DoubleBuffered = true;
        _surface.Controls.Add(_content);
        Controls.Add(_surface);
    }

    public void ShowOnBestScreen()
    {
        var screen = Screen.AllScreens.FirstOrDefault(value => !value.Primary) ?? Screen.PrimaryScreen ?? Screen.FromPoint(Point.Empty);
        Bounds = screen.Bounds;
        WindowState = FormWindowState.Maximized;
        Show();
    }

    public void UpdateDisplay(Color background, Color fontColor, string fontFamily, int fontSize, string text, string? itemFontColor = null, int? itemFontSize = null)
    {
        _surface.BackColor = background;
        _content.ForeColor = string.IsNullOrWhiteSpace(itemFontColor) ? fontColor : ColorTranslator.FromHtml(itemFontColor);
        _content.Font = new Font(fontFamily, itemFontSize ?? fontSize, FontStyle.Bold);
        _content.Text = text;
    }
}
