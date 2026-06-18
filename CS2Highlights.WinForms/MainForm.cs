namespace CS2Highlights.WinForms;

public class MainForm : Form
{
    public MainForm(DashboardPanel dashboardPanel, MatchesPanel matchesPanel, SettingsPanel settingsPanel, RenderPanel renderPanel)
    {
        BuildLayout(dashboardPanel, matchesPanel, settingsPanel, renderPanel);
    }

    private void BuildLayout(DashboardPanel dashboardPanel, MatchesPanel matchesPanel, SettingsPanel settingsPanel, RenderPanel renderPanel)
    {
        Text = "CS2Highlights";
        MinimumSize = new Size(1000, 680);
        Size = new Size(1200, 780);
        StartPosition = FormStartPosition.CenterScreen;

        var tabs = new TabControl { Dock = DockStyle.Fill };

        var dashboardPage = new TabPage("Dashboard");
        dashboardPanel.Dock = DockStyle.Fill;
        dashboardPage.Controls.Add(dashboardPanel);

        var matchesPage = new TabPage("Matches");
        matchesPanel.Dock = DockStyle.Fill;
        matchesPage.Controls.Add(matchesPanel);

        var settingsPage = new TabPage("Settings");
        settingsPanel.Dock = DockStyle.Fill;
        settingsPage.Controls.Add(settingsPanel);

        tabs.TabPages.Add(dashboardPage);
        tabs.TabPages.Add(matchesPage);
        var renderPage = new TabPage("Render");
        renderPanel.Dock = DockStyle.Fill;
        renderPage.Controls.Add(renderPanel);
        tabs.TabPages.Add(renderPage);
        tabs.TabPages.Add(Placeholder("Clips",  "Clip gallery — coming in Step 13"));
        tabs.TabPages.Add(settingsPage);

        Controls.Add(tabs);

        // When a demo is parsed on Dashboard, switch to Matches tab and refresh it
        dashboardPanel.MatchParsed += (_, _) =>
        {
            matchesPanel.LoadMatches();
            tabs.SelectedTab = matchesPage;
        };
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
