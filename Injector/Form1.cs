using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Forms.VisualStyles;
using System.IO;
using Timer = System.Windows.Forms.Timer;

namespace KingModInjector
{
    public partial class Form1 : Form
    {
        // Win32 API Imports for Window Dragging
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        // Win32 API Imports for DLL Injection
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, uint size, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;

        // UI Controls
        private Label processLabel;
        private Label titleLabel;
        private ComboBox processComboBox;
        private PictureBox processIcon;
        private Button closeButton;
        private Label dllLabel;
        private TextBox dllTextBox;
        private Button dllButton;
        private Button injectButton;
        private Label statusLabel;
        private PictureBox logoPictureBox;
        private Timer processRefreshTimer;
        private Timer animationTimer;
        private double pulseCounter = 0;

        // Theme Colors
        private readonly Color backColor = Color.FromArgb(25, 25, 25);
        private readonly Color midColor = Color.FromArgb(45, 45, 45);
        private readonly Color accentColor = Color.FromArgb(220, 20, 60); // Crimson Red
        private readonly Color glowColor = Color.FromArgb(255, 80, 100);
        private readonly Color textColor = Color.White;

        private class ProcessInfo
        {
            public string ProcessName { get; set; }
            public int ProcessId { get; set; }
            public string ProcessPath { get; set; }

            public override string ToString()
            {
                return $"{ProcessName} ({ProcessId})";
            }
        }

        public Form1()
        {
            InitializeComponent();
            InitializeCustomUI();
            InitializeProcessRefreshTimer();
            InitializeAnimationTimer();
            LoadProcesses();
        }

        private void InitializeCustomUI()
        {
            // Form Settings
            this.Size = new Size(600, 450);
            this.BackColor = backColor;
            this.ForeColor = textColor;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "KingMod Injector";
            this.Icon = new Icon("app.ico");

            // Custom Title Bar
            var titlePanel = new Panel { Size = new Size(this.Width, 35), BackColor = midColor, Dock = DockStyle.Top };
            titlePanel.MouseDown += TitleBar_MouseDown;

            titleLabel = new Label { Text = "KingMod Injector", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = accentColor, AutoSize = true, Location = new Point(10, 5) };
            titleLabel.MouseDown += TitleBar_MouseDown;

            closeButton = CreateGlowButton("✕", new Size(30, 30), new Point(this.Width - 40, 2));
            closeButton.Click += (s, e) => this.Close();

            titlePanel.Controls.Add(titleLabel);
            titlePanel.Controls.Add(closeButton);
            this.Controls.Add(titlePanel);

            // Logo
            logoPictureBox = new PictureBox
            {
                Size = new Size(100, 100),
                Location = new Point(250, 50),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = Image.FromFile("Kingmod.png")
            };
            this.Controls.Add(logoPictureBox);

            // Process Selection
            processLabel = new Label { Text = "Select Process:", Font = new Font("Segoe UI", 11), AutoSize = true, Location = new Point(40, 170) };
            processComboBox = new ComboBox { Width = 300, Location = new Point(40, 200), DropDownStyle = ComboBoxStyle.DropDownList, DrawMode = DrawMode.OwnerDrawFixed, BackColor = midColor, ForeColor = textColor, Font = new Font("Segoe UI", 10) };
            processComboBox.DrawItem += ProcessComboBox_DrawItem;
            processComboBox.SelectedIndexChanged += ProcessComboBox_SelectedIndexChanged;
            processIcon = new PictureBox { Size = new Size(48, 48), Location = new Point(350, 188), SizeMode = PictureBoxSizeMode.StretchImage };

            // DLL Selection
            dllLabel = new Label { Text = "Select DLL:", Font = new Font("Segoe UI", 11), AutoSize = true, Location = new Point(40, 250) };
            dllTextBox = new TextBox { Width = 300, Location = new Point(40, 280), BackColor = midColor, ForeColor = textColor, Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.FixedSingle };
            dllButton = CreateGlowButton("Browse...", new Size(100, 30), new Point(350, 278));
            dllButton.Click += DllButton_Click;

            // Inject Button
            injectButton = CreateGlowButton("Inject DLL", new Size(150, 40), new Point(225, 340));
            injectButton.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            injectButton.Click += InjectButton_Click;

            // Status Label
            statusLabel = new Label { Text = "Status: Ready", Font = new Font("Segoe UI", 10), AutoSize = true, Location = new Point(40, 410), ForeColor = Color.Gray };

            // Add Controls to Form
            this.Controls.AddRange(new Control[] { processLabel, processComboBox, processIcon, dllLabel, dllTextBox, dllButton, injectButton, statusLabel });
        }

        private Button CreateGlowButton(string text, Size size, Point location)
        {
            var button = new Button
            {
                Text = text,
                Size = size,
                Location = location,
                BackColor = accentColor,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.MouseEnter += (s, e) => button.BackColor = glowColor;
            button.MouseLeave += (s, e) => button.BackColor = accentColor;
            return button;
        }

        private void InitializeProcessRefreshTimer()
        {
            processRefreshTimer = new Timer();
            processRefreshTimer.Interval = 5000; // Refresh every 5 seconds
            processRefreshTimer.Tick += (s, e) => LoadProcesses();
            processRefreshTimer.Start();
        }

        private void InitializeAnimationTimer()
        {
            animationTimer = new Timer { Interval = 16 }; // ~60 FPS
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            pulseCounter += 0.1;
            float glowFactor = 0.6f + 0.4f * (float)Math.Sin(pulseCounter);
            int red = (int)(accentColor.R * glowFactor);
            int green = (int)(accentColor.G * glowFactor);
            int blue = (int)(accentColor.B * glowFactor);
            titleLabel.ForeColor = Color.FromArgb(Math.Min(255, red), Math.Min(255, green), Math.Min(255, blue));
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void LoadProcesses()
        {
            var selectedProcess = processComboBox.SelectedItem as ProcessInfo;

            var processes = Process.GetProcesses()
                .Where(p => p.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(p.MainWindowTitle))
                .Select(p =>
                {
                    try { return new ProcessInfo { ProcessName = p.ProcessName, ProcessId = p.Id, ProcessPath = p.MainModule.FileName }; }
                    catch { return null; }
                })
                .Where(p => p != null)
                .OrderBy(p => p.ProcessName)
                .ToList();

            processComboBox.Items.Clear();
            processComboBox.Items.AddRange(processes.ToArray());

            if (selectedProcess != null)
            {
                var stillRunning = processes.FirstOrDefault(p => p.ProcessId == selectedProcess.ProcessId);
                if (stillRunning != null) { processComboBox.SelectedItem = stillRunning; }
            }
        }



        private void ProcessComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();
            var processInfo = (ProcessInfo)processComboBox.Items[e.Index];
            try
            {
                using (var icon = Icon.ExtractAssociatedIcon(processInfo.ProcessPath))
                {
                    if (icon != null) { e.Graphics.DrawIcon(icon, e.Bounds.Left + 2, e.Bounds.Top + 2); }
                }
                var text = $"{processInfo.ProcessName} ({processInfo.ProcessId})";
                e.Graphics.DrawString(text, e.Font, new SolidBrush(e.ForeColor), e.Bounds.Left + 32, e.Bounds.Top + 2);
            }
            catch
            {
                var text = $"{processInfo.ProcessName} ({processInfo.ProcessId})";
                e.Graphics.DrawString(text, e.Font, new SolidBrush(e.ForeColor), e.Bounds.Left + 2, e.Bounds.Top + 2);
            }
        }

        private void ProcessComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (processComboBox.SelectedItem is ProcessInfo processInfo && !string.IsNullOrEmpty(processInfo.ProcessPath))
            {
                try
                {
                    using (var icon = Icon.ExtractAssociatedIcon(processInfo.ProcessPath))
                    {
                        if (icon != null) { processIcon.Image = icon.ToBitmap(); }
                    }
                }
                catch { processIcon.Image = null; }
            }
            else { processIcon.Image = null; }
        }

        private void DllButton_Click(object sender, EventArgs e)
        {
            using var dllDialog = new OpenFileDialog { Filter = "DLL Files (*.dll)|*.dll|All Files (*.*)|*.*", Title = "Select DLL File" };
            if (dllDialog.ShowDialog() == DialogResult.OK) { dllTextBox.Text = dllDialog.FileName; }
        }

        private void InjectButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (!(processComboBox.SelectedItem is ProcessInfo processInfo)) { statusLabel.Text = "Status: Please select a process."; return; }
                if (string.IsNullOrEmpty(dllTextBox.Text)) { statusLabel.Text = "Status: Please select a DLL file."; return; }

                statusLabel.Text = $"Status: Injecting into {processInfo.ProcessName}...";
                var hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processInfo.ProcessId);
                if (hProcess == IntPtr.Zero) throw new Exception("Could not open process.");

                var hKernel32 = GetModuleHandle("kernel32.dll");
                var hLoadLibrary = GetProcAddress(hKernel32, "LoadLibraryA");

                var pLoadLibrary = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)dllTextBox.Text.Length + 1, 0x1000, 0x40);
                WriteProcessMemory(hProcess, pLoadLibrary, System.Text.Encoding.Default.GetBytes(dllTextBox.Text), (uint)dllTextBox.Text.Length + 1, out _);
                CreateRemoteThread(hProcess, IntPtr.Zero, 0, hLoadLibrary, pLoadLibrary, 0, IntPtr.Zero);
                CloseHandle(hProcess);

                statusLabel.Text = "Status: DLL injected successfully!";
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Status: Error - {ex.Message}";
            }
        }
    }
}
