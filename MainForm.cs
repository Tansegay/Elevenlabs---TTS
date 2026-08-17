using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElevenLabsTTS;

public sealed class MainForm : Form
{
    private readonly TextBox apiKey = new();
    private readonly TextBox voiceId = new();
    private readonly ComboBox model = new();
    private readonly ComboBox format = new();
    private readonly TextBox text = new();
    private readonly Label status = new();
    private readonly Label stats = new();
    private readonly ProgressBar progress = new();
    private readonly Button generate = new();
    private readonly Button openFile = new();
    private readonly Button clear = new();
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(5) };

    public MainForm()
    {
        Text = "ElevenLabs TTS Studio";
        Width = 1200;
        Height = 780;
        MinimumSize = new Size(900, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(9, 11, 16);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);

        BuildUi();
        UpdateStats();
    }

    private Label L(string value) => new()
    {
        Text = value,
        AutoSize = true,
        ForeColor = Color.FromArgb(174, 184, 201),
        Margin = new Padding(0, 0, 0, 6)
    };

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(22),
            BackColor = BackColor
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var titlePanel = new Panel { Dock = DockStyle.Fill };
        var title = new Label
        {
            Text = "ElevenLabs TTS Studio",
            Font = new Font("Segoe UI Semibold", 25),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(0, 0)
        };
        var subtitle = new Label
        {
            Text = "Văn bản → giọng đọc • Windows Native • 1 click",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(150, 161, 181),
            AutoSize = true,
            Location = new Point(3, 42)
        };
        titlePanel.Controls.Add(title);
        titlePanel.Controls.Add(subtitle);
        root.Controls.Add(titlePanel, 0, 0);
        root.SetColumnSpan(titlePanel, 2);

        var left = Card();
        var right = Card();
        root.Controls.Add(left, 0, 1);
        root.Controls.Add(right, 1, 1);

        var leftLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        left.Controls.Add(leftLayout);

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        openFile.Text = "📂 Mở TXT / SRT";
        clear.Text = "Xóa";
        StyleButton(openFile);
        StyleButton(clear);
        openFile.Click += OpenFile_Click;
        clear.Click += (_, _) => { text.Clear(); status.Text = ""; UpdateStats(); };
        toolbar.Controls.Add(openFile);
        toolbar.Controls.Add(clear);
        leftLayout.Controls.Add(toolbar, 0, 0);

        text.Multiline = true;
        text.ScrollBars = ScrollBars.Both;
        text.AcceptsTab = true;
        text.WordWrap = true;
        text.BackColor = Color.FromArgb(11, 14, 20);
        text.ForeColor = Color.White;
        text.BorderStyle = BorderStyle.FixedSingle;
        text.Font = new Font("Segoe UI", 11);
        text.Dock = DockStyle.Fill;
        text.TextChanged += (_, _) => UpdateStats();
        leftLayout.Controls.Add(text, 0, 1);

        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        stats.Text = "0 ký tự • 0 từ";
        stats.ForeColor = Color.FromArgb(137, 148, 166);
        stats.Dock = DockStyle.Fill;
        stats.TextAlign = ContentAlignment.MiddleLeft;
        progress.Dock = DockStyle.Fill;
        progress.Style = ProgressBarStyle.Continuous;
        bottom.Controls.Add(stats, 0, 0);
        bottom.Controls.Add(progress, 1, 0);
        leftLayout.Controls.Add(bottom, 0, 2);

        var settings = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1 };
        settings.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        settings.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        settings.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        settings.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        settings.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        settings.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.Controls.Add(settings);

        apiKey.PlaceholderText = "sk_...";
        apiKey.UseSystemPasswordChar = true;
        AddSetting(settings, 0, "ElevenLabs API Key", apiKey);

        voiceId.PlaceholderText = "Voice ID";
        AddSetting(settings, 1, "Voice ID", voiceId);

        model.DropDownStyle = ComboBoxStyle.DropDownList;
        model.Items.AddRange(["eleven_multilingual_v2", "eleven_v3"]);
        model.SelectedIndex = 0;
        AddSetting(settings, 2, "Model", model);

        format.DropDownStyle = ComboBoxStyle.DropDownList;
        format.Items.AddRange(["mp3_44100_128", "mp3_22050_32"]);
        format.SelectedIndex = 0;
        AddSetting(settings, 3, "Output", format);

        generate.Text = "⚡  TẠO GIỌNG ĐỌC";
        generate.Dock = DockStyle.Fill;
        generate.BackColor = Color.FromArgb(104, 120, 255);
        generate.ForeColor = Color.White;
        generate.FlatStyle = FlatStyle.Flat;
        generate.FlatAppearance.BorderSize = 0;
        generate.Font = new Font("Segoe UI Semibold", 11);
        generate.Click += Generate_Click;
        settings.Controls.Add(generate, 0, 4);

        status.Text = "Sẵn sàng.";
        status.ForeColor = Color.FromArgb(174, 184, 201);
        status.Dock = DockStyle.Top;
        status.AutoSize = false;
        status.Height = 80;
        settings.Controls.Add(status, 0, 5);
    }

    private Panel Card() => new()
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(16),
        BackColor = Color.FromArgb(17, 21, 29),
        BorderStyle = BorderStyle.FixedSingle,
        Margin = new Padding(8)
    };

    private void StyleButton(Button b)
    {
        b.AutoSize = true;
        b.BackColor = Color.FromArgb(30, 39, 53);
        b.ForeColor = Color.White;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.Padding = new Padding(12, 7, 12, 7);
    }

    private void AddSetting(TableLayoutPanel panel, int row, string label, Control control)
    {
        var box = new Panel { Dock = DockStyle.Fill };
        var l = L(label);
        l.Location = new Point(0, 0);
        control.Location = new Point(0, 24);
        control.Width = 300;
        control.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        box.Controls.Add(l);
        box.Controls.Add(control);
        panel.Controls.Add(box, 0, row);
    }

    private void UpdateStats()
    {
        var s = text.Text;
        var words = s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        stats.Text = $"{s.Length:N0} ký tự • {words:N0} từ";
    }

    private async void OpenFile_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Text/SubRip|*.txt;*.srt|Text|*.txt|SubRip|*.srt",
            Multiselect = false
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var content = await File.ReadAllTextAsync(dialog.FileName, Encoding.UTF8);
            if (Path.GetExtension(dialog.FileName).Equals(".srt", StringComparison.OrdinalIgnoreCase))
                content = ParseSrt(content);

            text.Text = content;
            status.Text = $"Đã nạp: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Không đọc được file", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string ParseSrt(string input)
    {
        var lines = input.Replace("\r", "").Replace("\uFEFF", "").Split('\n');
        var output = new List<string>();
        foreach (var line in lines)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^\s*\d+\s*$")) continue;
            if (System.Text.RegularExpressions.Regex.IsMatch(line,
                @"^\s*\d{1,2}:\d{2}:\d{2}[,.]\d{3}\s*-->\s*\d{1,2}:\d{2}:\d{2}[,.]\d{3}"))
                continue;
            output.Add(line);
        }
        return System.Text.RegularExpressions.Regex.Replace(
            string.Join("\n", output), @"\n{3,}", "\n\n").Trim();
    }

    private async void Generate_Click(object? sender, EventArgs e)
    {
        var key = apiKey.Text.Trim();
        var voice = voiceId.Text.Trim();
        var content = text.Text.Trim();

        if (string.IsNullOrWhiteSpace(key)) { Error("Hãy nhập ElevenLabs API Key."); return; }
        if (string.IsNullOrWhiteSpace(voice)) { Error("Hãy nhập Voice ID."); return; }
        if (string.IsNullOrWhiteSpace(content)) { Error("Hãy nhập văn bản."); return; }

        using var save = new SaveFileDialog
        {
            Filter = "MP3 Audio|*.mp3",
            FileName = "elevenlabs-tts.mp3"
        };
        if (save.ShowDialog() != DialogResult.OK) return;

        generate.Enabled = false;
        progress.Value = 15;
        status.Text = "Đang gửi văn bản tới ElevenLabs…";

        try
        {
            var url = $"https://api.elevenlabs.io/v1/text-to-speech/{Uri.EscapeDataString(voice)}?output_format={Uri.EscapeDataString(format.Text)}";

            var payload = new
            {
                text = content,
                model_id = model.Text,
                voice_settings = new
                {
                    stability = 0.5,
                    similarity_boost = 0.75,
                    style = 0.0,
                    use_speaker_boost = true
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.TryAddWithoutValidation("xi-api-key", key);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            progress.Value = 65;

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Error($"ElevenLabs trả về {(int)response.StatusCode}: {error}");
                return;
            }

            await using var input = await response.Content.ReadAsStreamAsync();
            await using var output = File.Create(save.FileName);
            await input.CopyToAsync(output);

            progress.Value = 100;
            status.Text = $"✓ Hoàn tất: {Path.GetFileName(save.FileName)}";

            if (MessageBox.Show("Đã tạo MP3 thành công.\nMở thư mục chứa file?", "ElevenLabs TTS",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{save.FileName}\"",
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Error(ex.Message);
        }
        finally
        {
            generate.Enabled = true;
        }
    }

    private void Error(string message)
    {
        progress.Value = 0;
        status.Text = "✕ " + message;
        MessageBox.Show(message, "ElevenLabs TTS", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
