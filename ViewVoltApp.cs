using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using WinForms = System.Windows.Forms;
using System.Reflection;

namespace ViewVolt
{
    public class ViewVoltApp : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            // Create ribbon tab
            try
            {
                application.CreateRibbonTab("ViewVolt");
            }
            catch (Exception) { }

            // Create ribbon panel
            RibbonPanel ribbonPanel = application.CreateRibbonPanel("ViewVolt", "View Tools");

            // Create Manager button
            PushButtonData managerButtonData = new PushButtonData(
                "ViewVoltManager",
                "View\nManager",
                typeof(ViewVoltCommand).Assembly.Location,
                "ViewVolt.ViewVoltCommand");

            // Create QuickSave button
            PushButtonData quickSaveButtonData = new PushButtonData(
                "ViewVoltQuickSave",
                "Quick\nSave",
                typeof(ViewVoltCommand).Assembly.Location,
                "ViewVolt.QuickSaveCommand");

            // Set icons for buttons
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string assemblyDirectory = Path.GetDirectoryName(assemblyPath);

            // Set large icons (32x32)
            string managerIconPath = Path.Combine(assemblyDirectory, "Resources", "manager-32.png");
            string quickSaveIconPath = Path.Combine(assemblyDirectory, "Resources", "quicksave-32.png");

            if (File.Exists(managerIconPath))
            {
                managerButtonData.LargeImage = new BitmapImage(new Uri(managerIconPath));
            }
            if (File.Exists(quickSaveIconPath))
            {
                quickSaveButtonData.LargeImage = new BitmapImage(new Uri(quickSaveIconPath));
            }

            // Set small icons (16x16)
            string managerSmallIconPath = Path.Combine(assemblyDirectory, "Resources", "manager-32.png");
            string quickSaveSmallIconPath = Path.Combine(assemblyDirectory, "Resources", "quicksave-32.png");

            if (File.Exists(managerSmallIconPath))
            {
                managerButtonData.Image = new BitmapImage(new Uri(managerSmallIconPath));
            }
            if (File.Exists(quickSaveSmallIconPath))
            {
                quickSaveButtonData.Image = new BitmapImage(new Uri(quickSaveSmallIconPath));
            }

            // Set tooltips
            managerButtonData.ToolTip = "Open the View Manager to organize and manage your saved views.";
            quickSaveButtonData.ToolTip = "Quickly save the current view position.";


            // Add buttons to panel
            ribbonPanel.AddItem(managerButtonData);
            ribbonPanel.AddItem(quickSaveButtonData);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class QuickSaveCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                var view3D = uidoc.ActiveView as View3D;

                if (view3D == null)
                {
                    WinForms.MessageBox.Show("Please activate a 3D view first.");
                    return Result.Failed;
                }

                var viewOrientation = view3D.GetOrientation();
                var datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                var name = $"Quick Save {datetime}";

                // Create new view position
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

                // Load existing positions
                var saveFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ViewVoltPositions.xml");

                List<ViewPosition> savedPositions = new List<ViewPosition>();
                if (File.Exists(saveFilePath))
                {
                    var serializer = new XmlSerializer(typeof(List<ViewPosition>));
                    using (var reader = new StreamReader(saveFilePath))
                    {
                        savedPositions = (List<ViewPosition>)serializer.Deserialize(reader);
                    }
                }

                // Add new position
                savedPositions.Add(position);

                // Save back to file
                var serializer2 = new XmlSerializer(typeof(List<ViewPosition>));
                using (var writer = new StreamWriter(saveFilePath))
                {
                    serializer2.Serialize(writer, savedPositions);
                }

                WinForms.MessageBox.Show($"View saved as '{name}'");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show($"Error saving view: {ex.Message}");
                return Result.Failed;
            }
        }
    }
}