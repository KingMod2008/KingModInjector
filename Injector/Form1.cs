using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Forms.VisualStyles;
using Timer = System.Windows.Forms.Timer;

namespace KingModInjector
{
    public partial class Form1 : Form
    {
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

        private Label processLabel;
        private ComboBox processComboBox;
        private Button processButton;
        private Button refreshButton;
        private PictureBox processIcon;
        private Timer animationTimer;
        private Button closeButton;
        private Label dllLabel;
        private TextBox dllTextBox;
        private Button dllButton;
        private Button injectButton;
        private Label statusLabel;

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
        }

        private void InitializeCustomUI()
        {
            // Form settings
            this.Size = new Size(800, 600);
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "KingModInjector";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = true;
            this.TransparencyKey = Color.FromArgb(255, 0, 255);
            this.BackColor = Color.FromArgb(30, 30, 30);

            // Add custom title bar
            var titlePanel = new Panel
            {
                Size = new Size(this.Width, 40),
                BackColor = Color.FromArgb(40, 40, 40),
                Dock = DockStyle.Top
            };

            var titleLabel = new Label
            {
                Text = "KingModInjector",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 120, 200),
                AutoSize = true,
                Location = new Point(10, 8)
            };

            // Close button
            closeButton = new Button
            {
                Text = "✕",
                Width = 30,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(this.Width - 40, 0),
                Cursor = Cursors.Hand
            };

            closeButton.Click += (s, e) => this.Close();
            closeButton.MouseEnter += (s, e) => closeButton.BackColor = Color.FromArgb(200, 0, 0);
            closeButton.MouseLeave += (s, e) => closeButton.BackColor = Color.FromArgb(40, 40, 40);

            titlePanel.Controls.Add(titleLabel);
            titlePanel.Controls.Add(closeButton);
            this.Controls.Add(titlePanel);

            // Create main content panel
            var contentPanel = new Panel
            {
                Size = new Size(this.Width - 20, this.Height - 60),
                Location = new Point(10, 50),
                BackColor = Color.FromArgb(30, 30, 30)
            };

            // Process refresh button with animation
            refreshButton = new Button
            {
                Text = "Refresh",
                Width = 100,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Location = new Point(10, 10)
            };

            // Add process icon
            processIcon = new PictureBox
            {
                Size = new Size(32, 32),
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(10, 60)
            };

            // Process selection
            processLabel = new Label
            {
                Text = "Process:",
                AutoSize = true,
                ForeColor = Color.White,
                Location = new Point(50, 65)
            };

            // Process combo box
            processComboBox = new ComboBox
            {
                Width = 300,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Location = new Point(100, 60),
                DrawMode = DrawMode.OwnerDrawFixed,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            processComboBox.DrawItem += ProcessComboBox_DrawItem;
            processComboBox.SelectedIndexChanged += ProcessComboBox_SelectedIndexChanged;

            // DLL selection
            dllLabel = new Label
            {
                Text = "DLL File:",
                AutoSize = true,
                ForeColor = Color.White,
                Location = new Point(50, 120)
            };

            dllTextBox = new TextBox
            {
                Width = 300,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Location = new Point(100, 115)
            };

            dllButton = new Button
            {
                Text = "Browse",
                Width = 80,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(410, 115)
            };
            dllButton.Click += ProcessButton_Click;

            // Inject button
            injectButton = new Button
            {
                Text = "Inject",
                Width = 120,
                Height = 40,
                BackColor = Color.FromArgb(40, 120, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(340, 180)
            };
            injectButton.Click += InjectButton_Click;

            // Status label
            statusLabel = new Label
            {
                Text = "Ready",
                AutoSize = true,
                ForeColor = Color.White,
                Location = new Point(100, 240)
            };

            // Add controls to content panel
            contentPanel.Controls.AddRange(new Control[]
            {
                refreshButton,
                processIcon,
                processLabel,
                processComboBox,
                dllLabel,
                dllTextBox,
                dllButton,
                injectButton,
                statusLabel
            });

            // Add content panel to form
            this.Controls.Add(contentPanel);

            // Add animation timer
            animationTimer = new Timer { Interval = 16 }; // ~60 FPS
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();

            // Load processes initially
            LoadProcesses();
        }

        private void LoadProcesses()
        {
            try
            {
                processComboBox.Items.Clear();
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        var processInfo = new ProcessInfo
                        {
                            ProcessName = process.ProcessName,
                            ProcessId = process.Id,
                            ProcessPath = process.MainModule?.FileName
                        };
                        processComboBox.Items.Add(processInfo);
                    }
                    catch
                    {
                        // Ignore inaccessible processes
                    }
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error loading processes: {ex.Message}";
            }
        }

        private void ProcessComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();
            e.DrawFocusRectangle();

            var processInfo = processComboBox.Items[e.Index] as ProcessInfo;
            if (processInfo == null) return;

            try
            {
                // Get process icon
                Icon icon = null;
                if (!string.IsNullOrEmpty(processInfo.ProcessPath))
                {
                    try
                    {
                        icon = Icon.ExtractAssociatedIcon(processInfo.ProcessPath);
                    }
                    catch
                    {
                        // Try with just the executable name if full path fails
                        try
                        {
                            icon = Icon.ExtractAssociatedIcon($"{processInfo.ProcessName}.exe");
                        }
                        catch
                        {
                            // If both fail, just show text
                        }
                    }
                }

                if (icon != null)
                {
                    // Create a larger icon for the process icon display
                    var largeIcon = icon.ToBitmap();
                    if (processIcon.Image != null)
                    {
                        processIcon.Image.Dispose();
                    }
                    processIcon.Image = largeIcon;
                    e.Graphics.DrawIcon(icon, e.Bounds.Left + 2, e.Bounds.Top + 2);
                }

                // Draw text
                var text = $"{processInfo.ProcessName} ({processInfo.ProcessId})";
                e.Graphics.DrawString(text, e.Font, new SolidBrush(e.ForeColor), 
                    e.Bounds.Left + 32, e.Bounds.Top + 2);
            }
            catch
            {
                // If icon can't be loaded, just draw text
                var text = $"{processInfo.ProcessName} ({processInfo.ProcessId})";
                e.Graphics.DrawString(text, e.Font, new SolidBrush(e.ForeColor), 
                    e.Bounds.Left + 2, e.Bounds.Top + 2);
            }
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // Rotate refresh button icon
            if (refreshButton.Image != null)
            {
                refreshButton.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
            }
        }

        private void ProcessComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (processComboBox.SelectedItem is ProcessInfo processInfo && !string.IsNullOrEmpty(processInfo.ProcessPath))
            {
                try
                {
                    var icon = Icon.ExtractAssociatedIcon(processInfo.ProcessPath);
                    if (icon != null)
                    {
                        if (processIcon.Image != null)
                        {
                            processIcon.Image.Dispose();
                        }
                        processIcon.Image = icon.ToBitmap();
                    }
                }
                catch
                {
                    // If icon can't be loaded, clear the image
                    if (processIcon.Image != null)
                    {
                        processIcon.Image.Dispose();
                        processIcon.Image = null;
                    }
                }
            }
        }

        private void ProcessButton_Click(object sender, EventArgs e)
        {
            using var processDialog = new OpenFileDialog
            {
                Filter = "DLL Files (*.dll)|*.dll|All Files (*.*)|*.*",
                Title = "Select DLL File"
            };

            if (processDialog.ShowDialog() == DialogResult.OK)
            {
                dllTextBox.Text = processDialog.FileName;
            }
        }

        private void InjectButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (processComboBox.SelectedItem == null)
                {
                    statusLabel.Text = "Please select a process first";
                    return;
                }

                if (string.IsNullOrEmpty(dllTextBox.Text))
                {
                    statusLabel.Text = "Please select a DLL file";
                    return;
                }

                var processInfo = processComboBox.SelectedItem as ProcessInfo;
                if (processInfo == null)
                {
                    statusLabel.Text = "Invalid process selection";
                    return;
                }

                var processId = processInfo.ProcessId;
                statusLabel.Text = "Injecting DLL...";

                var hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
                if (hProcess == IntPtr.Zero)
                {
                    throw new Exception("Could not open process.");
                }

                var hKernel32 = GetModuleHandle("kernel32.dll");
                var hLoadLibrary = GetProcAddress(hKernel32, "LoadLibraryA");
                
                // Inject DLL
                var pLoadLibrary = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)256, 0x1000, 0x40);
                WriteProcessMemory(hProcess, pLoadLibrary, System.Text.Encoding.ASCII.GetBytes(dllTextBox.Text), (uint)dllTextBox.Text.Length, out _);
                CreateRemoteThread(hProcess, IntPtr.Zero, 0, hLoadLibrary, pLoadLibrary, 0, IntPtr.Zero);
                CloseHandle(hProcess);

                statusLabel.Text = "DLL injected successfully!";
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error: {ex.Message}";
            }
        }

    }
}
