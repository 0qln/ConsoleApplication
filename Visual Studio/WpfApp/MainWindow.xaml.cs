using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using System.Xml.Linq;
using System.IO;
using System.IO.Pipes;
using System.Windows.Controls.Primitives;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Data;
using System.Diagnostics;
using System.Security.Policy;
using Microsoft.VisualBasic.FileIO;
using System.Threading;
using System.Reflection.Metadata;
using Microsoft.Win32;
using WpfCustomControls;

namespace ConsoleWindow {

    public partial class MainWindow : Window {

        private static string executionSymbol = "";
        private static readonly string pipeName = "ContentPipe"; // Do not change unless you change the dll assembly!
        private static NamedPipeClientStream ?pipeClient;
        private static DispatcherTimer timer = new();
        private static readonly TimeSpan intervall = TimeSpan.FromMilliseconds(200);
        private static StreamReader ?reader;

        private static readonly double dropDownMenuHeight = 22;
        private static readonly double maximizedFix = 5;
        private static readonly double systemButton_size = 30;
        private static readonly double arrowButton_size = 15;
        private static readonly double qSettingsHeight = 30;
        private static readonly (double, double) iconSize = (22, 22);
        private static readonly List<UIElement> clientButtonUnfoldMenus = new();
        private static readonly List<ConsoleWindow> consoles = new();
        private static ConsoleWindow MainConsole = new(arrowButton_size);

        private static WindowHandle windowHandle;
        private static readonly string iconPath = @"D:\Programmmieren\__DebugLibrary\VSCode\ConsoleWindow\Zeichnung.png";

        private static ProgramRunType runType = ProgramRunType.StandAlone;

        private static DropDownMenu fileMenu = new();
        private static DropDownMenu saveMenu = new();
        private static DropDownMenu loadMenu = new();

        private static DropDownMenu viewMenu = new();

        private static DropDownMenu editMenu = new();

        private static DropDownMenu windowMenu = new();


        public MainWindow() {
            InitializeComponent();
            WindowChrome.SetWindowChrome(this, new WindowChrome());

            Loaded += (object sender, RoutedEventArgs e) => {
                MigrateWinResourcesToAppResources();
                CreateMainConsole();

                ApplicationStartup();
                MainConsole.WriteLine($"Started ConsoleApplication as Type: {runType}.");
                if (runType == ProgramRunType.DebugLibrary) StartTimer_();



                windowHandle = new WindowHandle(Application.Current)
                .SetParentWindow(ParentGrid)
                .AddIcon(iconPath)
                .CreateClientButton("File", fileMenu)
                .CreateClientButton("Edit", editMenu)
                .CreateClientButton("View", viewMenu)
                .CreateClientButton("Window", windowMenu);

                GenerateClientbuttonMenus();

                windowHandle.ActivateAllClientButtons();

                windowHandle.SetWindowChromActiveAll();
            };
        }


        private static string ?ApplicationDirectory => System.IO.Path.GetDirectoryName(Environment.ProcessPath);

        private static void ApplicationStartup() {
            // Read the startupInformation
            string path = ApplicationDirectory + "\\startArguments.txt";
            if (!File.Exists(path)) {
                //MainConsole.WriteLine($"'{path}' was not found.");
                return;
            }
            //MainConsole.WriteLine($"'{path}' was found.");

            string argument = "";
            using (StreamReader reader = new(path)) {
                argument = File.ReadAllText(path);
            }
            File.Delete(path);

            if (argument == "DebugLibrary") {
                runType = ProgramRunType.DebugLibrary;
            }
        }

        private static void MigrateWinResourcesToAppResources() {
            ResourceDictionary windowResources = Application.Current.MainWindow.Resources;
            foreach (DictionaryEntry entry in windowResources) {
                if (entry.Value is Style style) {
                    Style newStyle = new(style.TargetType);

                    // Copy the setters and triggers from the existing style to the new style
                    foreach (SetterBase setter in style.Setters) {
                        if (setter is Setter s) {
                            newStyle.Setters.Add(new Setter(s.Property, s.Value));
                        }
                    }
                    foreach (TriggerBase trigger in style.Triggers) {
                        newStyle.Triggers.Add(trigger);
                    }

                    Application.Current.Resources.Add(entry.Key, newStyle);
                }
            }
        }

        private void CreateMainConsole() {
            // Safely Create a new ConsoleWindow
            var newConsole = new ConsoleWindow(arrowButton_size);
            newConsole.AddQuickSettings(qSettingsHeight);

            ParentGrid.Children.Add(newConsole.UIElement);
            Grid.SetRow(newConsole.UIElement, ParentGrid.RowDefinitions.Count - 1);
            Grid.SetColumn(newConsole.UIElement, ParentGrid.ColumnDefinitions.Count - 1);

            MainConsole = newConsole;
            consoles.Add(newConsole);
        }
        private void CreateNewConsole() {
            // Safely Create a new ConsoleWindow
            var newConsole = new ConsoleWindow(arrowButton_size);
            newConsole.AddQuickSettings(qSettingsHeight);

            ParentGrid.Children.Add(newConsole.UIElement);
            RowDefinition rowDefinition = new() { Height = new GridLength(100) };
            ParentGrid.RowDefinitions.Add(rowDefinition);

            Grid.SetRow(newConsole.UIElement, ParentGrid.RowDefinitions.Count - 1);
            Grid.SetColumn(newConsole.UIElement, ParentGrid.ColumnDefinitions.Count - 1);

            consoles.Add(newConsole);
        }

        private void GenerateClientbuttonMenus() {
            // First Degree
            // Initilization 
            fileMenu.Instanciate(windowHandle.GetClientButton("File").Item1);
            editMenu.Instanciate(windowHandle.GetClientButton("Edit").Item1);
            viewMenu.Instanciate(windowHandle.GetClientButton("View").Item1);
            windowMenu.Instanciate(windowHandle.GetClientButton("Window").Item1);

            clientButtonUnfoldMenus.Add(fileMenu.UIElement);
            clientButtonUnfoldMenus.Add(viewMenu.UIElement);
            clientButtonUnfoldMenus.Add(editMenu.UIElement);
            clientButtonUnfoldMenus.Add(windowMenu.UIElement);

            foreach (var menu in clientButtonUnfoldMenus) {
                if (!MainCanvas.Children.Contains(menu)) {
                    MainCanvas.Children.Add(menu);
                }
            }

            // Adding settings
            fileMenu.AddOption(new DropDownMenu.MenuOption(dropDownMenuHeight, fileMenu).SetName("Load"));
            fileMenu.AddOption(new DropDownMenu.MenuOption(dropDownMenuHeight, fileMenu).SetName("Safe"));
            viewMenu.AddOption(new DropDownMenu.MenuOption(dropDownMenuHeight, viewMenu).SetName("Increase Font Size").SetKeyboardShortcut("Strg + Mousewheel Up").AddCommand(MainConsole.IncreaseConsoleFont));
            viewMenu.AddOption(new DropDownMenu.MenuOption(dropDownMenuHeight, viewMenu).SetName("Decrease Font Size").SetKeyboardShortcut("Strg + Mousewheel Down").AddCommand(MainConsole.DecreaseConsoleFont));
            editMenu.AddOption(new DropDownMenu.MenuOption(dropDownMenuHeight, editMenu).SetName("Search and Replace").SetKeyboardShortcut("Strg + F"));
            windowMenu.AddOption(new DropDownMenu.MenuOption(dropDownMenuHeight, windowMenu).SetName("Add new Console").SetKeyboardShortcut("Strg + N").AddCommand(CreateNewConsole));


            // Second degree
            // Initilization 
            saveMenu.Instanciate(fileMenu.GetOption("Safe"), fileMenu);
            loadMenu.Instanciate(fileMenu.GetOption("Load"), fileMenu);

            clientButtonUnfoldMenus.Add(saveMenu.UIElement);
            clientButtonUnfoldMenus.Add(loadMenu.UIElement);

            foreach (var menu in clientButtonUnfoldMenus) {
                if (!MainCanvas.Children.Contains(menu)) {
                    MainCanvas.Children.Add(menu);
                }
            }

            // Adding settings
            saveMenu.AddOption(new DropDownMenu.MenuOption(dropDownMenuHeight, saveMenu).SetName("Save to default file").SetKeyboardShortcut("Strg + S + D").AddCommand(MainConsole.SaveDefault));
            saveMenu.AddOption(new DropDownMenu.MenuOption(dropDownMenuHeight, saveMenu).SetName("Save to specific file").SetKeyboardShortcut("Strg + S + F").AddCommand(MainConsole.SaveSpecific));
            loadMenu.AddOption(new DropDownMenu.MenuOption(dropDownMenuHeight, loadMenu).SetName("Load from default file").SetKeyboardShortcut("Strg + O + D").AddCommand(MainConsole.Load));
            fileMenu.GetOption("Safe").AddDropdownMenu(saveMenu);
            fileMenu.GetOption("Load").AddDropdownMenu(loadMenu);
        }

        #region System


        private static void StartTimer_() {
            // Initiate the PipeClient
            pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.In);
            pipeClient.Connect();

            // Initialize the reader
            reader = new StreamReader(pipeClient);

            //declare executionSymbol
            executionSymbol = reader.ReadLine();

            bool reading = false;
            timer = new() {
                Interval = intervall
            };
            timer.Tick += async (sender, e) => {
                if (reading) {
                    return;
                }
                try {
                    reading = true;

                    // Empty the Pipe
                    string message = await reader.ReadLineAsync();
                    while (message != null) {
                        // Check for xMessage
                        if (message.Contains(executionSymbol)) {
                            string executionMessage = "";
                            string parameter = "";
                            // Find the indexes of the two instances of executionSymbol
                            int startIndex = message.IndexOf(executionSymbol, 0);
                            int endIndex = message.IndexOf(executionSymbol, startIndex + executionSymbol.Length);

                            // If both instances of executionSymbol were found
                            if (startIndex >= 0 && endIndex > startIndex) {
                                // extract the executionMessage between them
                                executionMessage = message.Substring(startIndex + executionSymbol.Length, endIndex - startIndex - executionSymbol.Length);
                                // Extract the parameter
                                parameter = message.Substring(endIndex + executionSymbol.Length, message.Length - (2 * executionSymbol.Length) - executionMessage.Length);
                                
                                // Execute the executionMessage command
                                Execute(executionMessage, parameter);
                            }
                        }
                        else {
                            // Print out the message on a new line
                            MainConsole.WriteLine(message);
                            MainConsole.ScrollToEnd();
                        }

                        // Get next message for next iteration
                        message = await reader.ReadLineAsync();
                    }

                    reading = false;
                }
                catch (Exception ex) {
                    MainConsole.WriteLine("Internal Error while trying to read messages from the PipingSystem: ");
                    MainConsole.WriteLine(ex.ToString());
                    MainConsole.ScrollToEnd();
                }
            };
            timer.Start();
        }
        /// <summary>
        /// Executes the <paramref name="command"/>.
        /// </summary>
        /// <param name="command"></param>
        private static void Execute(string command, string parameter) {
            switch (command) {

                case "Kill":
                    ShutDown();
                    break;

                case "Clear":
                    MainConsole.ClearConsole();
                    break;

                case "DeleteLine":
                    MainConsole.DeleteLine();
                    break;

                case "DeleteLineWithIndexOf":
                    MainConsole.DeleteLine(int.Parse(parameter));
                    break;

                default: break;
            }
        }
        /// <summary>
        /// Shuts down the Application
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        private static void ShutDown() {
            if (runType == ProgramRunType.DebugLibrary) {
                try {
                    timer.Stop();
                    pipeClient.Close();
                    pipeClient.Dispose();
                }
                catch (IOException ex) {
                    Application.Current.Shutdown();
                }
            }

            Application.Current.Shutdown();
        }

        public string ShowFolderPickerDialog() {
            var openFileDialog = new OpenFileDialog {
                Title = "Select a folder",
                Filter = "Folders|*.000|All Files|*.*",
                FileName = "Folder Selection",
                CheckFileExists = false,
                CheckPathExists = true,
                ReadOnlyChecked = true,
                Multiselect = false,
                ValidateNames = false,
                DereferenceLinks = true,
                ShowReadOnly = false,
                AddExtension = false,
                RestoreDirectory = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                CustomPlaces = null,
            };

            // Set the FolderPicker option
            openFileDialog.ShowDialog(this);

            // Get the selected folder path
            string selectedFolderPath = System.IO.Path.GetDirectoryName(openFileDialog.FileName);
            if (selectedFolderPath != null && Directory.Exists(selectedFolderPath)) {
                return selectedFolderPath;
            }

            return null;
        }
        #endregion

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e) {
            ParentGrid.Height = MainCanvas.ActualHeight;
            ParentGrid.Width = MainCanvas.ActualWidth;
        }



        class QuickSettings {
            private Grid grid = new();
            public UIElement UIElement => grid;

            private StackPanel stackPanel = new();

            private TextBlock lineCounter = new();
            public TextBlock LineCounter { get => lineCounter; }

            private Button clearAllTextButton = new();
            private Button deleteLineButton = new();
            private Button fillConsoleButton = new();

            private ConsoleWindow console;


            public QuickSettings(ConsoleWindow console) {
                this.console = console;

                grid.Margin = new Thickness(3, 3, 3, 3);
                grid.Children.Add(stackPanel);
                stackPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1f1f1f"));
                stackPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                stackPanel.VerticalAlignment = VerticalAlignment.Stretch;
                stackPanel.Orientation = Orientation.Horizontal;

                clearAllTextButton.Style = Application.Current.Resources["QuickSettingsButtonStyle"] as Style;
                clearAllTextButton.Content = "Clear All Lines";
                clearAllTextButton.Click += ClearAllTextButton_Click1;
                deleteLineButton.Style = Application.Current.Resources["QuickSettingsButtonStyle"] as Style;
                deleteLineButton.Content = "Delete Line";
                deleteLineButton.Click += DeleteLineButton_Click1;
                fillConsoleButton.Style = Application.Current.Resources["QuickSettingsButtonStyle"] as Style;
                fillConsoleButton.Content = "Fill Console";
                fillConsoleButton.Click += FillConsoleButton_Click;

                lineCounter.Text = "Lines: ";
                lineCounter.VerticalAlignment = VerticalAlignment.Center;
                lineCounter.HorizontalAlignment = HorizontalAlignment.Left;
                lineCounter.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#808080"));

                stackPanel.Children.Add(lineCounter);
                stackPanel.Children.Add(clearAllTextButton);
                stackPanel.Children.Add(deleteLineButton);
                stackPanel.Children.Add(fillConsoleButton);
            }

            private void FillConsoleButton_Click(object sender, RoutedEventArgs e) {
                console.FillConsole();
            }

            private void DeleteLineButton_Click1(object sender, RoutedEventArgs e) {
                console.DeleteLine();
            }

            private void ClearAllTextButton_Click1(object sender, RoutedEventArgs e) {
                console.ClearConsole();
            }
        }

        class ConsoleWindow {
            private Border border = new();
            public UIElement UIElement => border;

            public string ?defaultPath;

            private List<string> content = new();
            private Grid parentGrid = new();
            private Grid topGrid = new();
            private Grid bottomGrid = new();
            private ConsoleText ?text;
            public TextBox Text => text.UIElement;
            private ConsoleScrollbar ?scrollbar;
            private QuickSettings ?settings;
            private double arrowButtonWidth;


            public ConsoleWindow(double pArrowButtonWidht) {
                arrowButtonWidth = pArrowButtonWidht;
                text = new ConsoleText(this);
                scrollbar = new ConsoleScrollbar(text, pArrowButtonWidht);
                InitGrid();

                border.Child = parentGrid;
                border.BorderBrush = Helper.Color("#707070");
                border.BorderThickness = new Thickness(1);
                border.CornerRadius = new CornerRadius(0);
            }
            private void InitGrid() {
                //TopGrid
                InitTopGrid();


                //BottomGrid
                InitBottomGrid();


                //ParentGrid
                InitParentGrid();


                //Fill ParentGrid
                parentGrid.Children.Add(topGrid);
                Grid.SetColumn(topGrid, 0);
                Grid.SetRow(topGrid, 0);

                parentGrid.Children.Add(bottomGrid);
                Grid.SetColumn(bottomGrid, 0);
                Grid.SetRow(bottomGrid, 1);


                // Add the console to the parent grid
                topGrid.Children.Add(text.UIElement);
                Grid.SetColumn(text.UIElement, 0);
                Grid.SetRow(text.UIElement, 0);

                // Add the Scrolbar to the parent grid
                topGrid.Children.Add(scrollbar.UIElement);
                Grid.SetColumn(scrollbar.UIElement, 1);
                Grid.SetRow(scrollbar.UIElement, 0);
            }
            private void InitParentGrid() {
                ColumnDefinition c = new() {
                    Width = new GridLength(1, GridUnitType.Star)
                };

                // Top/ConsoleAndScrollbar Grid
                RowDefinition r1 = new() {
                    Height = new GridLength(1, GridUnitType.Star)
                };

                // Bottom/qSettings Grid
                RowDefinition r2 = new() {
                    Height = new GridLength(0, GridUnitType.Pixel)
                };

                parentGrid.ColumnDefinitions.Add(c);
                parentGrid.RowDefinitions.Add(r1);
                parentGrid.RowDefinitions.Add(r2);
            }
            private void InitBottomGrid() {
                ColumnDefinition c1 = new() {
                    Width = new GridLength(1, GridUnitType.Star)
                };

                RowDefinition r1 = new() {
                    Height = new GridLength(1, GridUnitType.Star)
                };

                bottomGrid.ColumnDefinitions.Add(c1);
                bottomGrid.RowDefinitions.Add(r1);
            }
            private void InitTopGrid() {
                ColumnDefinition c1 = new() {
                    Width = new GridLength(1, GridUnitType.Star)
                };

                ColumnDefinition c2 = new() {
                    Width = new GridLength(arrowButtonWidth, GridUnitType.Pixel)
                };

                RowDefinition r1 = new() {
                    Height = new GridLength(1, GridUnitType.Star)
                };

                topGrid.ColumnDefinitions.Add(c1);
                topGrid.ColumnDefinitions.Add(c2);
                topGrid.RowDefinitions.Add(r1);
            }

            public void AddQuickSettings(double height) {
                settings = new QuickSettings(this);
                parentGrid.RowDefinitions[1].Height = new GridLength(height);
                parentGrid.Children.Add(settings.UIElement);
                Grid.SetColumn(settings.UIElement, 0);
                Grid.SetRow(settings.UIElement, 1);
            }


            public void DecreaseConsoleFont() {
                text.UIElement.FontSize--;
            }
            public void IncreaseConsoleFont() {
                text.UIElement.FontSize++;
            }

            public string ContentToString {
                get {
                    var stringBuilder = new StringBuilder();
                    foreach (var s in content) {
                        stringBuilder.Append(s);
                        stringBuilder.Append('\n');
                    }
                    return stringBuilder.ToString();
                }
            }

            public void SaveDefault() {
                if (defaultPath == null) {
                    MessageBox.Show("No default path has been selected.");
                }

                File.WriteAllLines(defaultPath, content);
            }
            public void SaveSpecific() {
                try {
                    string selectedFolderPath = ((MainWindow)Application.Current.MainWindow).ShowFolderPickerDialog();
                    if (selectedFolderPath != null) {
                        string filename = selectedFolderPath + "\\log.txt";
                        using (FileStream stream = File.Create(filename)) {
                            stream.Close();
                        }
                        File.WriteAllLines(filename, content);
                    }
                }
                catch (Exception e) {
                    WriteLine(e.ToString());
                }
            }

            public void Load() {
                Load(defaultPath);
            }
            public void Load(string filename) {
                if (filename == null) throw new ArgumentException("Filename was null.");
                if (!File.Exists(filename)) throw new ArgumentException("File does not exist.");

                content = File.ReadAllLines(filename).ToList();
            }

            /// <summary>
            /// May require the use of
            /// Dispatcher.Invoke(() => { WriteLine(message) });
            /// to update the UI on the main thread, if it causes exceptions.
            /// </summary>
            /// <param name="messageStr"></param>
            public void WriteLine(string messageStr) {
                text.UIElement.Text += messageStr + "\n";
                content.Add(messageStr);
            }
            public void FillConsole() {
                for (int i = 0; i < 100; i++) {
                    WriteLine(i.ToString());
                }
            }
            public void DeleteLine() {
                try {
                    content.RemoveAt(content.Count - 1);
                    text.UIElement.Text = ContentToString;
                }
                catch {
                    // Handle the exception
                }
            }
            public void DeleteLine(int index) {
                try {
                    content.RemoveAt(index);
                    text.UIElement.Text = ContentToString;
                }
                catch {
                    // Handle the exception
                }
            }
            public void ClearConsole() {
                content.Clear();
                text.UIElement.Text = "";
            }
            public void ScrollToEnd() {
                text.UIElement.ScrollToEnd();
            }

            private void ConsoleBox_LayoutUpdated(object sender, EventArgs e) {
                // return if one of the needed components is not yet loaded
                if (!text.UIElement.IsLoaded) return;
                if (topGrid == null) return;

                // Adjust the Slider hight and position
                scrollbar.AdjustSlider();
            }
            private void ConsoleBox_TextChanged(object sender, TextChangedEventArgs e) {
                try {
                    settings.LineCounter.Text = content.Count.ToString();
                }
                catch { }
            }

            class ConsoleText {
                private TextBox textBox;
                public TextBox UIElement => textBox;
                private ConsoleWindow console;
                public ConsoleText(ConsoleWindow console) {
                    this.console = console;
                    textBox = new() {
                        Style = Application.Current.Resources["ConsoleStyle"] as Style
                    };
                    textBox.LayoutUpdated += TextBox_LayoutUpdated;
                    textBox.TextChanged += TextBox_TextChanged;
                }

                private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
                    try {
                        console.settings.LineCounter.Text = console.content.Count.ToString();
                    }
                    catch { }
                }

                private void TextBox_LayoutUpdated(object sender, EventArgs e) {
                    // Adjust the Slider hight and position
                    // return if one of the needed components is not yet loaded
                    if (!textBox.IsLoaded) return;
                    if (console.scrollbar.UIElement == null) return;

                    console.scrollbar.AdjustSlider();
                }

                public int VisibleLines => 1 + textBox.GetLastVisibleLineIndex() - textBox.GetFirstVisibleLineIndex(); 
            }

            class ConsoleScrollbar {
                private Grid grid = new();
                private Button upArrow = new();
                private Button downArrow = new();
                private Rectangle slider = new();
                private ConsoleText textBox;

                private DispatcherTimer arrowTimer = new();
                private double activationThreshhold = 0.5;
                private double arrowTickRate = 0.1;
                private double arrowWidth;

                public UIElement UIElement => grid;


                public ConsoleScrollbar(ConsoleText textBox, double arrowWidth) {
                    this.textBox = textBox;
                    this.arrowWidth = arrowWidth;
                    InitGrid();


                    upArrow.Content = "↑";
                    upArrow.Style = (Style)Application.Current.Resources["ArrowButtonStyle"];
                    upArrow.PreviewMouseDown += UpArrow_PreviewMouseDown;
                    upArrow.PreviewMouseUp += UpArrow_PreviewMouseUp;
                    upArrow.Click += UpArrow_Click;

                    downArrow.Content = "↓";
                    downArrow.Style = (Style)Application.Current.Resources["ArrowButtonStyle"];
                    downArrow.PreviewMouseDown += DownArrow_PreviewMouseDown;
                    downArrow.PreviewMouseUp += DownArrow_PreviewMouseUp;
                    downArrow.Click += DownArrow_Click;


                    slider.HorizontalAlignment = HorizontalAlignment.Center;
                    slider.VerticalAlignment = VerticalAlignment.Top;
                    slider.Fill = Helper.Color("#2e2e2e");
                    slider.MouseLeftButtonDown += Slider_MouseLeftButtonDown;
                    slider.MouseLeftButtonUp += Slider_MouseLeftButtonUp;
                    slider.Width = arrowWidth;


                    Helper.SetChildInGrid(grid, upArrow, 0, 0);
                    Helper.SetChildInGrid(grid, downArrow, 2, 0);
                    Helper.SetChildInGrid(grid, slider, 1, 0);
                }
                private void InitGrid() {
                    grid.VerticalAlignment = VerticalAlignment.Stretch;

                    ColumnDefinition c1 = new() {
                        Width = new GridLength(1, GridUnitType.Star)
                    };

                    RowDefinition r1 = new() { Height = new GridLength(arrowWidth, GridUnitType.Pixel) };
                    RowDefinition r2 = new() { Height = new GridLength(1, GridUnitType.Star) };
                    RowDefinition r3 = new() { Height = new GridLength(arrowWidth, GridUnitType.Pixel) };

                    grid.ColumnDefinitions.Add(c1);
                    grid.RowDefinitions.Add(r1);
                    grid.RowDefinitions.Add(r2);
                    grid.RowDefinitions.Add(r3);
                }


                // Arrow Buttons
                public void UpArrow_Click(object sender, RoutedEventArgs e) {
                    textBox.UIElement.LineUp();
                }
                public void UpArrow_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
                    arrowTimer = new() { Interval = TimeSpan.FromSeconds(activationThreshhold) };
                    arrowTimer.Tick += UpArrowTick;
                    arrowTimer.Start();
                }
                public void UpArrowTick(object sender, EventArgs e) {
                    if (Mouse.LeftButton == MouseButtonState.Pressed) {
                        textBox.UIElement.LineUp();
                        arrowTimer.Interval = TimeSpan.FromSeconds(arrowTickRate);
                    }
                    else {
                        arrowTimer.Interval = TimeSpan.FromSeconds(activationThreshhold);
                    }
                }
                public void UpArrow_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
                    if (arrowTimer != null) {
                        arrowTimer.Stop();
                        arrowTimer = null;
                    }
                }

                public void DownArrow_Click(object sender, RoutedEventArgs e) {
                    textBox.UIElement.LineDown();
                }
                public void DownArrow_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
                    arrowTimer = new() { Interval = TimeSpan.FromSeconds(activationThreshhold) };
                    arrowTimer.Tick += DownArrowTick;
                    arrowTimer.Start();
                }
                public void DownArrowTick(object sender, EventArgs e) {
                    if (Mouse.LeftButton == MouseButtonState.Pressed) {
                        textBox.UIElement.LineDown();
                        arrowTimer.Interval = TimeSpan.FromSeconds(arrowTickRate);
                    }
                    else {
                        arrowTimer.Interval = TimeSpan.FromSeconds(activationThreshhold);
                    }
                }
                public void DownArrow_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
                    if (arrowTimer != null) {
                        arrowTimer.Stop();
                        arrowTimer = null;
                    }
                }


                // Slider
                private double offsetOnStart = 0;
                private bool isScrolling = false;
                public void Slider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
                    ((System.Windows.Shapes.Rectangle)sender).CaptureMouse();
                    CompositionTarget.Rendering += CompositionTarget_Rendering_Silder;

                    isScrolling = true;
                    double mousePos = Mouse.GetPosition(Application.Current.MainWindow).Y; // Mouse Position Relative to the window
                    double minHandleHeight_relToTheWindow = systemButton_size + arrowButton_size - maximizedFix;
                    double mousePosDelta = mousePos - (slider.Margin.Top + minHandleHeight_relToTheWindow); // Mouse Position Relative to the handle 
                    offsetOnStart = mousePosDelta;
                }
                public void Slider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
                    ((System.Windows.Shapes.Rectangle)sender).ReleaseMouseCapture();
                    CompositionTarget.Rendering -= CompositionTarget_Rendering_Silder;

                    isScrolling = false;
                }
                public void CompositionTarget_Rendering_Silder(object sender, EventArgs e) {
                    double mousePos = Mouse.GetPosition(Application.Current.MainWindow).Y; // Mouse Position Relative to the window
                    double minHandleHeight_relToTheWindow = systemButton_size + arrowButton_size - maximizedFix;
                    double newY = (mousePos - offsetOnStart) - (minHandleHeight_relToTheWindow); // The New Height of the handle

                    double minHeight = 0;
                    double maxHeight = grid.RowDefinitions.ElementAt(1).ActualHeight - (slider.ActualHeight);
                    if (newY < minHeight) {
                        newY = minHeight;
                    }
                    if (newY > maxHeight) {
                        newY = maxHeight;
                    }
                    slider.Margin = new Thickness(0, newY, 0, 0);

                    int newLineIndex = (int)(newY / maxHeight * (textBox.UIElement.LineCount));
                    try {
                        textBox.UIElement.ScrollToLine(newLineIndex);
                    }
                    catch {
                        if (newY == minHeight) {
                            newLineIndex = 0;
                        }
                        if (newY == maxHeight) {
                            newLineIndex = textBox.UIElement.LineCount - 1;
                        }

                        textBox.UIElement.ScrollToLine(newLineIndex);
                    }
                }
                public void AdjustSlider() {
                    double visLines = textBox.VisibleLines;
                    double totalLines = textBox.UIElement.LineCount;
                    double scrollBarHeight = grid.RowDefinitions[1].ActualHeight;
                    double newHeight = scrollBarHeight * (visLines / totalLines);
                    slider.Height = newHeight;

                    if (!isScrolling) {
                        double newLineIndex = textBox.UIElement.GetFirstVisibleLineIndex();
                        double maxHeight = grid.RowDefinitions.ElementAt(1).ActualHeight;
                        double newY = ((newLineIndex / textBox.UIElement.LineCount) * maxHeight);
                        slider.Margin = new Thickness(0, newY, 0, 0);
                    }
                }
                public void AdjustSlider(object sender, EventArgs e) {
                    AdjustSlider();
                }
            }
        }

        enum DockDirection {
            Right,
            Bottom,
            Left,
            Top
        }

    }

    public enum ProgramRunType {
        DebugLibrary,
        StandAlone
    }
}
