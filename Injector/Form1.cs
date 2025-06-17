using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Linq;

namespace KingModInjector
{
    public partial class Form1 : Form
    {
        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        public Form1()
        {
            InitializeCustomUI();
        }

        private void InitializeCustomUI()
        {
            // Form settings
            this.Size = new Size(600, 400);
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "KingMod Injector";
            this.MaximizeBox = false;
            this.MinimizeBox = false;  // Remove minimize button
            this.ShowInTaskbar = true; // Show in taskbar

            // Process refresh button
            refreshButton = new Button
            {
                Text = "Refresh",
                Width = 80,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };

            // Process selection
            processLabel = new Label
            {
                Text = "Process:",
                AutoSize = true,
                ForeColor = Color.White
            };
            processComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                Width = 300,
                AutoCompleteMode = AutoCompleteMode.Suggest,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            processComboBox.DrawItem += ProcessComboBox_DrawItem;
            processComboBox.MeasureItem += ProcessComboBox_MeasureItem;
            processButton = new Button
            {
                Text = "Browse",
                Width = 80
            };

            // DLL selection
            dllLabel = new Label
            {
                Text = "DLL:",
                AutoSize = true,
                ForeColor = Color.White
            };
            dllTextBox = new TextBox
            {
                Width = 200
            };
            dllButton = new Button
            {
                Text = "Browse",
                Width = 80
            };

            // Inject button
            injectButton = new Button
            {
                Text = "Inject",
                Width = 100,
                BackColor = Color.FromArgb(40, 120, 200),
                ForeColor = Color.White
            };

            // Layout
            var flowLayoutPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(30, 30, 30),
                Width = 500
            };

            var processPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true
            };
            processPanel.Controls.Add(processLabel);
            processPanel.Controls.Add(processComboBox);
            processPanel.Controls.Add(refreshButton);
            processPanel.Controls.Add(processButton);

            var dllPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true
            };
            dllPanel.Controls.Add(dllLabel);
            dllPanel.Controls.Add(dllTextBox);
            dllPanel.Controls.Add(dllButton);

            // Add status label
            statusLabel = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = Color.White,
                Padding = new Padding(0, 10, 0, 0)
            };

            flowLayoutPanel.Controls.Add(processPanel);
            flowLayoutPanel.Controls.Add(dllPanel);
            flowLayoutPanel.Controls.Add(injectButton);
            flowLayoutPanel.Controls.Add(statusLabel);

            this.Controls.Add(flowLayoutPanel);

            // Event handlers
            refreshButton.Click += RefreshButton_Click;
            processButton.Click += ProcessButton_Click;
            dllButton.Click += DllButton_Click;
            injectButton.Click += InjectButton_Click;

            // Load processes on form load
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
                        // Ignore processes we can't access
                    }
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error loading processes: {ex.Message}";
            }
        }

        private void ProcessComboBox_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = 24;
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
                    // Resize icon to 16x16
                    var resizedIcon = Icon.FromHandle(icon.Handle).ToBitmap();
                    e.Graphics.DrawImage(resizedIcon, e.Bounds.Left + 2, e.Bounds.Top + 4, 16, 16);
                }

                // Draw text
                var text = $"{processInfo.ProcessName} ({processInfo.ProcessId})";
                e.Graphics.DrawString(text, e.Font, new SolidBrush(e.ForeColor), 
                    e.Bounds.Left + 24, e.Bounds.Top + 4);
            }
            catch
            {
                // If icon can't be loaded, just draw text
                var text = $"{processInfo.ProcessName} ({processInfo.ProcessId})";
                e.Graphics.DrawString(text, e.Font, new SolidBrush(e.ForeColor), 
                    e.Bounds.Left + 2, e.Bounds.Top + 2);
            }
        }

        private void ProcessButton_Click(object sender, EventArgs e)
        {
            using var processDialog = new OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select Process"
            };
            if (processDialog.ShowDialog() == DialogResult.OK)
            {
                LoadProcesses();
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadProcesses();
        }

        private void DllButton_Click(object sender, EventArgs e)
        {
            using var dllDialog = new OpenFileDialog
            {
                Filter = "Dynamic Link Library (*.dll)|*.dll|All Files (*.*)|*.*",
                Title = "Select DLL"
            };
            if (dllDialog.ShowDialog() == DialogResult.OK)
            {
                dllTextBox.Text = dllDialog.FileName;
            }
        }

        private void InjectButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(processComboBox.Text) || string.IsNullOrEmpty(dllTextBox.Text))
            {
                statusLabel.Text = "Error: Please select both process and DLL.";
                return;
            }

            try
            {
                // Extract process ID from the selected item
                var selectedItem = processComboBox.SelectedItem.ToString();
                var processId = int.Parse(selectedItem.Split(' ')[1].Trim('(', ')'));

                var process = Process.GetProcessById(processId);
                if (process == null)
                {
                    statusLabel.Text = "Error: Process not found.";
                    return;
                }

                var hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
                if (hProcess == IntPtr.Zero)
                {
                    statusLabel.Text = "Error: Could not open process.";
                    return;
                }

                var hKernel32 = GetModuleHandle("kernel32.dll");
                var hLoadLibrary = GetProcAddress(hKernel32, "LoadLibraryA");

                var pLoadLibrary = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)256, 0x1000, 0x40);
                WriteProcessMemory(hProcess, pLoadLibrary, System.Text.Encoding.ASCII.GetBytes(dllTextBox.Text), (uint)dllTextBox.Text.Length, out _);

                CreateRemoteThread(hProcess, IntPtr.Zero, 0, hLoadLibrary, pLoadLibrary, 0, IntPtr.Zero);
                CloseHandle(hProcess);

                statusLabel.Text = "Injection successful!";
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error: {ex.Message}";
            }
        }

        private Label processLabel;
        private ComboBox processComboBox;
        private Button processButton;
        private Button refreshButton;
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
    }
}
