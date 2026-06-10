namespace CS2Highlights.WinForms;

public class MainForm : Form
{
    public MainForm(SettingsPanel settingsPanel)
    {
        BuildLayout(settingsPanel);
    }

    private void BuildLayout(SettingsPanel settingsPanel)
    {
        Text = "CS2Highlights";
        MinimumSize = new Size(1000, 680);
        Size = new Size(1200, 780);
        StartPosition = FormStartPosition.CenterScreen;

        var tabs = new TabControl { Dock = DockStyle.Fill };

        tabs.TabPages.Add(Placeholder("Dashboard", "Demo scanning — coming in Step 6"));
        tabs.TabPages.Add(Placeholder("Matches",   "Match list — coming in Step 8"));
        tabs.TabPages.Add(Placeholder("Render",    "Render queue — coming in Step 12"));
        tabs.TabPages.Add(Placeholder("Clips",     "Clip gallery — coming in Step 13"));

        var settingsPage = new TabPage("Settings");
        settingsPanel.Dock = DockStyle.Fill;
        settingsPage.Controls.Add(settingsPanel);
        tabs.TabPages.Add(settingsPage);

        Controls.Add(tabs);
    }

    private static TabPage Placeholder(string title, string message)
    {
        var page = new TabPage(title);
        page.Controls.Add(new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 11f),
            ForeColor = SystemColors.GrayText
        });
        return page;
    }
}
