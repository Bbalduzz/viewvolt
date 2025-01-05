using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace ViewVolt
{
    [Serializable]
    public class ViewPosition
    {
        public string Name { get; set; }
        public double EyeX { get; set; }
        public double EyeY { get; set; }
        public double EyeZ { get; set; }
        public double UpX { get; set; }
        public double UpY { get; set; }
        public double UpZ { get; set; }
        public double ForwardX { get; set; }
        public double ForwardY { get; set; }
        public double ForwardZ { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ViewManagerForm : WinForms.Form
    {
        private UIDocument uidoc;
        private WinForms.ListView listView;
        private WinForms.TextBox searchBox;
        private List<ViewPosition> savedPositions;
        private string saveFilePath;

        public ViewManagerForm(UIDocument uidoc)
        {
            this.uidoc = uidoc;
            this.savedPositions = new List<ViewPosition>();
            this.saveFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ViewVoltPositions.xml");
            InitializeComponent();
            LoadSavedPositions();
        }

        private void InitializeComponent()
        {
            // Form settings
            this.Text = "ViewVolt";
            this.Size = new Drawing.Size(600, 800);  // Set default size
            this.MinimumSize = new Drawing.Size(350, 400);  // Set minimum size
            this.AutoSize = false;  // Disable auto-size
            this.BackColor = Drawing.Color.FromArgb(240, 240, 240);
            this.Font = new Drawing.Font("Segoe UI", 9F);
            this.Padding = new WinForms.Padding(15);

            // Main container panel (adds background to content)
            var mainContainer = new WinForms.Panel
            {
                AutoSize = true,
                AutoSizeMode = WinForms.AutoSizeMode.GrowAndShrink,
                BackColor = Drawing.Color.White,
                Padding = new WinForms.Padding(15),
                Dock = WinForms.DockStyle.Fill
            };

            // Search container
            var searchContainer = new WinForms.Panel
            {
                AutoSize = true,
                Dock = WinForms.DockStyle.Top,
                Height = 45,
                BackColor = Drawing.Color.White,
                Margin = new WinForms.Padding(0, 0, 0, 10)
            };

            // Search box with border
            var searchBoxContainer = new WinForms.Panel
            {
                Dock = WinForms.DockStyle.Fill,
                BackColor = Drawing.Color.FromArgb(245, 245, 245),
                Padding = new WinForms.Padding(10, 8, 10, 8)
            };

            // Search box
            searchBox = new WinForms.TextBox
            {
                Dock = WinForms.DockStyle.Fill,
                Font = new Drawing.Font("Segoe UI", 10F),
                BorderStyle = WinForms.BorderStyle.None,
                BackColor = Drawing.Color.FromArgb(245, 245, 245),
            };
            searchBox.TextChanged += SearchBox_TextChanged;

            // Content panel (contains ListView)
            var contentPanel = new WinForms.Panel
            {
                Height = 200,
                Dock = WinForms.DockStyle.Fill,
                BackColor = Drawing.Color.White,
                Padding = new WinForms.Padding(0, 10, 0, 10)
            };

            // ListView for saved positions
            listView = new WinForms.ListView
            {
                Dock = WinForms.DockStyle.Fill,
                View = WinForms.View.Details,
                FullRowSelect = true,
                GridLines = false,
                BorderStyle = WinForms.BorderStyle.None,
                BackColor = Drawing.Color.White,
                ShowItemToolTips = true
            };

            // Configure ListView columns
            listView.Columns.Add("Name", -2);
            listView.Columns.Add("Date", 120);

            // Style the ListView
            listView.Font = new Drawing.Font("Segoe UI", 9F);
            listView.Items.Clear();

            // Button panel
            var buttonPanel = new WinForms.Panel
            {
                AutoSize = true,
                Dock = WinForms.DockStyle.Bottom,
                BackColor = Drawing.Color.White,
                Padding = new WinForms.Padding(0, 10, 0, 0)
            };

            // Create custom button style
            var buttonStyle = new Action<WinForms.Button>((btn) =>
            {
                btn.FlatStyle = WinForms.FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.Height = 38;
                btn.Font = new Drawing.Font("Segoe UI", 9F);
                btn.Cursor = WinForms.Cursors.Hand;
                btn.Padding = new WinForms.Padding(5);
            });

            // Save button
            var saveButton = new WinForms.Button
            {
                Text = "Save Current View",
                Width = 130,
                BackColor = Drawing.Color.FromArgb(0, 120, 212),
                ForeColor = Drawing.Color.White
            };
            buttonStyle(saveButton);
            saveButton.Click += (s, e) => SaveCurrentView();

            // Load button
            var loadButton = new WinForms.Button
            {
                Text = "Load View",
                Width = 110,
                BackColor = Drawing.Color.FromArgb(245, 245, 245),
                ForeColor = Drawing.Color.Black,
                Margin = new WinForms.Padding(10, 0, 0, 0)
            };
            buttonStyle(loadButton);
            loadButton.Click += (s, e) => LoadSelectedView();

            // Delete button
            var deleteButton = new WinForms.Button
            {
                Text = "Delete",
                Width = 80,
                BackColor = Drawing.Color.FromArgb(245, 245, 245),
                ForeColor = Drawing.Color.Black,
                Margin = new WinForms.Padding(10, 0, 0, 0)
            };
            buttonStyle(deleteButton);
            deleteButton.Click += (s, e) => DeleteSelectedView();

            // Rename button
            var renameButton = new WinForms.Button
            {
                Text = "Rename",
                Width = 80,
                BackColor = Drawing.Color.FromArgb(245, 245, 245),
                ForeColor = Drawing.Color.Black,
                Margin = new WinForms.Padding(10, 0, 0, 0)
            };
            buttonStyle(renameButton);
            renameButton.Click += (s, e) => RenameSelectedView();

            // Context menu for right-click
            var contextMenu = new WinForms.ContextMenuStrip();
            var renameMenuItem = new WinForms.ToolStripMenuItem("Rename");
            var deleteMenuItem = new WinForms.ToolStripMenuItem("Delete");

            renameMenuItem.Click += (s, e) => RenameSelectedView();
            deleteMenuItem.Click += (s, e) => DeleteSelectedView();

            contextMenu.Items.AddRange(new WinForms.ToolStripItem[] {
                renameMenuItem,
                deleteMenuItem
            });

            listView.ContextMenuStrip = contextMenu;

            // Flow panel for buttons
            var flowPanel = new WinForms.FlowLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                FlowDirection = WinForms.FlowDirection.LeftToRight,
                AutoSize = true,
                Height = 80,
                Padding = new WinForms.Padding(15, 0, 15, 0)
            };
            flowPanel.Controls.AddRange(new WinForms.Control[] {
                saveButton,
                loadButton,
                deleteButton,
            });

            // Build the layout hierarchy
            buttonPanel.Controls.Add(flowPanel);
            searchBoxContainer.Controls.Add(searchBox);
            searchContainer.Controls.Add(searchBoxContainer);
            contentPanel.Controls.Add(listView);

            mainContainer.Controls.Add(buttonPanel);
            mainContainer.Controls.Add(contentPanel);
            mainContainer.Controls.Add(searchContainer);

            this.Controls.Add(mainContainer);

            // Form properties
            this.FormBorderStyle = WinForms.FormBorderStyle.Sizable;
            this.MaximizeBox = false;

            // Handle form resize
            this.Resize += (s, e) => {
                if (listView.Items.Count > 0)
                {
                    listView.Columns[0].Width = listView.Width - listView.Columns[1].Width - 4;
                }
            };
            this.StartPosition = WinForms.FormStartPosition.CenterScreen;

            // Subscribe to events
            this.Load += Form_Load;
            listView.DoubleClick += (s, e) => LoadSelectedView();
        }

        private void Form_Load(object sender, EventArgs e)
        {
            UpdateListView();
            CenterToScreen();
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            UpdateListView();
        }

        private void SaveCurrentView()
        {
            var view3D = uidoc.ActiveView as View3D;
            if (view3D == null)
            {
                ShowNotification("Please activate a 3D view first.", true);
                return;
            }

            var viewOrientation = view3D.GetOrientation();
            var name = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter name for this view position:",
                "Save View Position",
                "View " + (savedPositions.Count + 1));

            if (string.IsNullOrEmpty(name)) return;

            var position = new ViewPosition
            {
                Name = name,
                EyeX = viewOrientation.EyePosition.X,
                EyeY = viewOrientation.EyePosition.Y,
                EyeZ = viewOrientation.EyePosition.Z,
                UpX = viewOrientation.UpDirection.X,
                UpY = viewOrientation.UpDirection.Y,
                UpZ = viewOrientation.UpDirection.Z,
                ForwardX = viewOrientation.ForwardDirection.X,
                ForwardY = viewOrientation.ForwardDirection.Y,
                ForwardZ = viewOrientation.ForwardDirection.Z,
                CreatedAt = DateTime.Now
            };

            savedPositions.Add(position);
            SavePositionsToFile();
            UpdateListView();
            // ShowNotification("View position saved successfully");
        }

        private void LoadSelectedView()
        {
            if (listView.SelectedItems.Count == 0)
            {
                ShowNotification("Please select a view to load", true);
                return;
            }

            var view3D = uidoc.ActiveView as View3D;
            if (view3D == null)
            {
                ShowNotification("Please activate a 3D view first", true);
                return;
            }

            var position = savedPositions[listView.SelectedIndices[0]];
            var eyePosition = new XYZ(position.EyeX, position.EyeY, position.EyeZ);
            var upDirection = new XYZ(position.UpX, position.UpY, position.UpZ);
            var forwardDirection = new XYZ(position.ForwardX, position.ForwardY, position.ForwardZ);

            var viewOrientation = new ViewOrientation3D(eyePosition, upDirection, forwardDirection);

            using (Transaction trans = new Transaction(uidoc.Document, "Set View Position"))
            {
                trans.Start();
                try
                {
                    view3D.SetOrientation(viewOrientation);
                    view3D.SaveOrientation();
                    trans.Commit();
                    uidoc.RefreshActiveView();
                    // ShowNotification("View position loaded successfully");
                }
                catch (Exception ex)
                {
                    trans.RollBack();
                    ShowNotification("Failed to set view orientation: " + ex.Message, true);
                }
            }
        }

        private void DeleteSelectedView()
        {
            if (listView.SelectedItems.Count == 0)
            {
                ShowNotification("Please select a view to delete", true);
                return;
            }

            if (WinForms.MessageBox.Show(
                "Are you sure you want to delete this view position?",
                "Confirm Delete",
                WinForms.MessageBoxButtons.YesNo,
                WinForms.MessageBoxIcon.Question) == WinForms.DialogResult.Yes)
            {
                savedPositions.RemoveAt(listView.SelectedIndices[0]);
                SavePositionsToFile();
                UpdateListView();
                ShowNotification("View position deleted");
            }
        }

        private void UpdateListView()
        {
            listView.Items.Clear();
            var searchText = searchBox.Text.ToLower();

            foreach (var position in savedPositions)
            {
                if (string.IsNullOrEmpty(searchText) || position.Name.ToLower().Contains(searchText))
                {
                    var item = new ListViewItem(position.Name);
                    item.SubItems.Add(position.CreatedAt.ToString("g"));
                    listView.Items.Add(item);
                }
            }

            if (listView.Items.Count > 0)
            {
                listView.Columns[0].Width = -2;
                listView.Columns[1].Width = -2;
            }
        }

        private void SavePositionsToFile()
        {
            var serializer = new XmlSerializer(typeof(List<ViewPosition>));
            using (var writer = new StreamWriter(saveFilePath))
            {
                serializer.Serialize(writer, savedPositions);
            }
        }

        private void LoadSavedPositions()
        {
            if (!File.Exists(saveFilePath)) return;

            try
            {
                var serializer = new XmlSerializer(typeof(List<ViewPosition>));
                using (var reader = new StreamReader(saveFilePath))
                {
                    savedPositions = (List<ViewPosition>)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                ShowNotification("Failed to load saved positions: " + ex.Message, true);
            }
        }

        private void RenameSelectedView()
        {
            if (listView.SelectedItems.Count == 0)
            {
                ShowNotification("Please select a view to rename", true);
                return;
            }

            var selectedIndex = listView.SelectedIndices[0];
            var currentPosition = savedPositions[selectedIndex];

            // Show input dialog with current name
            var newName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter new name for the view:",
                "Rename View",
                currentPosition.Name);

            // Check if user cancelled or entered empty name
            if (string.IsNullOrWhiteSpace(newName) || newName == currentPosition.Name)
                return;

            // Check if name already exists
            if (savedPositions.Any(p => p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                ShowNotification("A view with this name already exists", true);
                return;
            }

            // Update the name
            currentPosition.Name = newName;

            // Save changes
            SavePositionsToFile();

            // Update the ListView
            UpdateListView();

            // Select the renamed item
            foreach (WinForms.ListViewItem item in listView.Items)
            {
                if (item.Text == newName)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    break;
                }
            }

            // ShowNotification("View renamed successfully");
        }

        private void ShowNotification(string message, bool isError = false)
        {
            WinForms.MessageBox.Show(
                message,
                isError ? "Error" : "Success",
                WinForms.MessageBoxButtons.OK,
                isError ? WinForms.MessageBoxIcon.Error : WinForms.MessageBoxIcon.Information
            );
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class ViewVoltCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;

            using (var form = new ViewManagerForm(uidoc))
            {
                form.ShowDialog();
            }

            return Result.Succeeded;
        }
    }
}