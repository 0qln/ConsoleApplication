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
using CommandLibrary;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Data;

namespace ConsoleWindow {

    public partial class MainWindow : Window {

        private static string executionSymbol = "";
        private static readonly string pipeName = "ContentPipe"; // Do not change unless you change the dll assembly!
        private static NamedPipeClientStream pipeClient;
        private static DispatcherTimer timer;
        private static StreamReader reader;

        private static double optionHeight = 22;
        private static double maximizedFix = 5;
        private static double systemButton_size = 30;
        private static double arrowButton_size = 15;
        private static double qSettingsHeight = 30;
        private static (double, double) iconSize = (22, 22);
        private static double clientButton_size = systemButton_size * (4 / 5);
        private static List<UIElement> clientButtonUnfoldMenus = new List<UIElement>();
        private static List<ConsoleWindow> consoles = new List<ConsoleWindow>();
        private static ConsoleWindow MainConsole;

        private static DropDownMenu fileMenu;
        private static DropDownMenu viewMenu;
        private static DropDownMenu editMenu;
        private static DropDownMenu windowMenu;

        // Set False to activate the Piping system
        private static bool DEVELOPMENT_ = true;


        public MainWindow() {
            InitializeComponent();
            WindowChrome.SetWindowChrome(this, new WindowChrome());

            UpdateSystemButtonSize();
            SetTitleBar();
            PrepareUIElements();

            Loaded += (object sender, RoutedEventArgs e) => {
                MigrateWinResourcesToAppResources();
                CreateMainConsole();
                GenerateClientbuttonMenus();
            };


            if (DEVELOPMENT_ == false) {
                StartTimer_();
            }
        }

        private static void MigrateWinResourcesToAppResources() {
            ResourceDictionary windowResources = Application.Current.MainWindow.Resources;
            foreach (DictionaryEntry entry in windowResources) {
                if (entry.Value is Style style) {
                    Style newStyle = new Style(style.TargetType);

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
            ConsoleWindow newConsole = new ConsoleWindow(arrowButton_size);
            newConsole.AddQuickSettings(qSettingsHeight);

            ParentGrid.Children.Add(newConsole.UIElement);
            Grid.SetRow(newConsole.UIElement, ParentGrid.RowDefinitions.Count - 1);
            Grid.SetColumn(newConsole.UIElement, ParentGrid.ColumnDefinitions.Count - 1);

            newConsole.WriteLine("Hello World!");
            MainConsole = newConsole;
            consoles.Add(newConsole);
        }

        private void GenerateClientbuttonMenus() {
            fileMenu = new DropDownMenu(DockDirection.Bottom, FileButton);
            fileMenu.AddOption(new DropDownMenu.MenuOption(optionHeight).SetName("Safe").SetKeyboardShortcut("Strg + S"));
            fileMenu.AddOption(new DropDownMenu.MenuOption(optionHeight).SetName("Load").SetKeyboardShortcut("Strg + O"));
            MainCanvas.Children.Add(fileMenu.UIElement);
            clientButtonUnfoldMenus.Add(fileMenu.UIElement);

            viewMenu = new DropDownMenu(DockDirection.Bottom, ViewButton);
            viewMenu.AddOption(new DropDownMenu.MenuOption(optionHeight)
                .SetName("Increase Font Size")
                .SetKeyboardShortcut("Strg + Mousewheel Up")
                .AddCommand(MainConsole.IncreaseConsoleFont)
            );
            viewMenu.AddOption(new DropDownMenu.MenuOption(optionHeight)
                .SetName("Decrease Font Size")
                .SetKeyboardShortcut("Strg + Mousewheel Down")
                .AddCommand(MainConsole.DecreaseConsoleFont)
            );
            MainCanvas.Children.Add(viewMenu.UIElement);
            clientButtonUnfoldMenus.Add(viewMenu.UIElement);

            editMenu = new DropDownMenu(DockDirection.Bottom, EditButton);
            editMenu.AddOption(new DropDownMenu.MenuOption(optionHeight).SetName("Search and Replace").SetKeyboardShortcut("Strg + F"));
            MainCanvas.Children.Add(editMenu.UIElement);
            clientButtonUnfoldMenus.Add(editMenu.UIElement);

            windowMenu = new DropDownMenu(DockDirection.Bottom, WindowsButton);
            windowMenu.AddOption(new DropDownMenu.MenuOption(optionHeight).SetName("Add new Console").SetKeyboardShortcut("Strg + N"));
            MainCanvas.Children.Add(windowMenu.UIElement);
            clientButtonUnfoldMenus.Add(windowMenu.UIElement);
        }

        private void PrepareUIElements() {
            IconSymbol icon = new IconSymbol(@"D:\Programmmieren\__DebugLibrary\VSCode\ConsoleWindow\Zeichnung.png", iconSize.Item1, iconSize.Item1);
            HandleBar.Children.Insert(0, icon.UIElement);
        }
        private void UpdateSystemButtonSize() {
            ChildGrid_1.ColumnDefinitions[1].Width = new GridLength(3 * systemButton_size);
        }
        private void SetTitleBar() {
            foreach (UIElement element in SystemButtons.Children) {
                WindowChrome.SetIsHitTestVisibleInChrome(element, true);
            }
            foreach (UIElement element in HandleBar.Children) {
                WindowChrome.SetIsHitTestVisibleInChrome(element, true);
            }
        }

        #region System
        /// <summary>
        /// kein plan warum das funktioniert
        /// DONT CHANGE!!!
        /// </summary>
        private void StartTimer_() {
            // Initiate the PipeClient
            pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.In);
            pipeClient.Connect();

            // Initialize the reader
            reader = new StreamReader(pipeClient);

            //declare executionSymbol
            try {
                executionSymbol = reader.ReadLine();
            }
            catch (Exception e) {

            }

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += async (sender, e) => {
                try {
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

                            // If both instances of executionSymbol were found, extract the executionMessage between them
                            if (startIndex >= 0 && endIndex > startIndex) {
                                executionMessage = message.Substring(startIndex + executionSymbol.Length, endIndex - startIndex - executionSymbol.Length);
                                parameter = message.Substring(endIndex + executionSymbol.Length, message.Length - (2 * executionSymbol.Length) - executionMessage.Length);
                            }
                            else {
                                break;
                            }
                            // Extract the parameter

                            // Execute the executionMessage command
                            Execute(executionMessage, parameter);
                        }
                        else {
                            // Print out the message on a new line
                            MainConsole.WriteLine(message);
                            MainConsole.ScrollToEnd();
                        }

                        // Get next message for next iteration
                        message = await reader.ReadLineAsync();
                    }



                }
                catch (Exception e1) {

                }
            };
            timer.Start();

        }
        /// <summary>
        /// Executes the <paramref name="command"/>.
        /// </summary>
        /// <param name="command"></param>
        private void Execute(string command, string parameter) {
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
        private void ShutDown() {
            if (DEVELOPMENT_ == false) {
                try {
                    timer.Stop();
                    pipeClient.Close();
                    pipeClient.Dispose();
                }
                catch (IOException ex) {
                    Application.Current.Shutdown();
                    throw new ArgumentException($"An Error occured while trying to shut down the ConsoleWindowApplication: {ex}");
                }
            }


            Application.Current.Shutdown();
        }
        #endregion

        #region System Buttons
        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            ShutDown();
        }
        private void MaximizeButton_Click(object sender, RoutedEventArgs e) {
            AdjustWindowSize();
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) {
            this.WindowState = WindowState.Minimized;
        }
        private void AdjustWindowSize() {
            if (this.WindowState == WindowState.Maximized) {
                this.WindowState = WindowState.Normal;
                ParentGrid.Margin = new Thickness(0, 0, 0, 0);
            }
            else {
                this.WindowState = WindowState.Maximized;
                ParentGrid.Margin = new Thickness(maximizedFix, maximizedFix, maximizedFix, maximizedFix);
            }
        }
        #endregion


        #region Client buttons
        private void EditButton_Click(object sender, RoutedEventArgs e) {
            ActivateMenuOnClick(editMenu);
        }

        private void FileButton_Click(object sender, RoutedEventArgs e) {
            ActivateMenuOnClick(fileMenu);
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e) {
            ActivateMenuOnClick(viewMenu);
        }
        private void View_Option1_MouseDown(object sender, MouseButtonEventArgs e) {
            MainConsole.DecreaseConsoleFont();
        }
        private void View_Option2_MouseDown(object sender, MouseButtonEventArgs e) {
            MainConsole.IncreaseConsoleFont();
        }

        private void WindowsButton_Click(object sender, RoutedEventArgs e) {
            ActivateMenuOnClick(windowMenu);
        }

        private void Windows_Options1_MouseDown(object sender, MouseButtonEventArgs e) {
            NewConsoleAtBottom();
        }
        private void NewConsoleAtBottom() {
            ConsoleWindow newConsole = new ConsoleWindow(arrowButton_size);
            newConsole.AddQuickSettings(qSettingsHeight);

            RowDefinition rowDefinition = new RowDefinition();
            rowDefinition.Height = new GridLength(100, GridUnitType.Pixel);
            ParentGrid.RowDefinitions.Add(rowDefinition);
            ParentGrid.Children.Add(newConsole.UIElement);
            Grid.SetRow(newConsole.UIElement, ParentGrid.RowDefinitions.Count - 1);
            Grid.SetColumn(newConsole.UIElement, ParentGrid.ColumnDefinitions.Count - 1);

            newConsole.WriteLine("Hello World!");
        }


        // Helper Methods
        private static bool isUsingClientButtons = false;
        private void ActivateClientButton(DropDownMenu element) {
            element.UIElement.Visibility = Visibility.Visible;
        }
        private void DeactivateAllClientButtons() {
            foreach (Border element in clientButtonUnfoldMenus) {
                element.Visibility = Visibility.Collapsed;
            }
        }
        private void Button_MouseEnter(object sender, MouseEventArgs e) {
            if (!isUsingClientButtons) return;

            Button button = sender as Button;
            switch (button.Tag.ToString()) {
                case "View":
                    DeactivateAllClientButtons();
                    ActivateClientButton(viewMenu);
                    break;

                case "File":
                    DeactivateAllClientButtons();
                    ActivateClientButton(fileMenu);
                    break;

                case "Edit":
                    DeactivateAllClientButtons();
                    ActivateClientButton(editMenu);
                    break;

                case "Windows":
                    DeactivateAllClientButtons();
                    ActivateClientButton(windowMenu);
                    break;
            }
        }
        private void ActivateMenuOnClick(DropDownMenu element) {
            if (isUsingClientButtons) {
                DeactivateAllClientButtons();
                isUsingClientButtons = false;
            }
            else {
                isUsingClientButtons = true;
                ActivateClientButton(element);
            }
        }
        #endregion

        #region QuickActions
        #endregion



        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e) {
            ParentGrid.Height = MainCanvas.ActualHeight;
            ParentGrid.Width = MainCanvas.ActualWidth;
        }



        class QuickSettings {
            private Grid grid = new Grid();
            public UIElement UIElement => grid;

            private StackPanel stackPanel = new StackPanel();

            private TextBlock lineCounter = new TextBlock();
            public TextBlock LineCounter { get => lineCounter; }

            private Button clearAllTextButton = new Button();
            private Button deleteLineButton = new Button();
            private Button fillConsoleButton = new Button();

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

        class IconSymbol {
            private Grid grid = new Grid();
            private Image image = new Image();
            public UIElement UIElement => grid;

            public IconSymbol(string path, double width, double height) {
                image.Source = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
                image.HorizontalAlignment = HorizontalAlignment.Stretch;
                image.VerticalAlignment = VerticalAlignment.Stretch;
                image.Width = width;
                image.Height = height;

                ColumnDefinition c = new ColumnDefinition();
                c.Width = new GridLength(1, GridUnitType.Star);
                grid.ColumnDefinitions.Add(c);

                RowDefinition r = new RowDefinition();
                r.Height = new GridLength(1, GridUnitType.Star);
                grid.RowDefinitions.Add(r);

                grid.Children.Add(image);
                Grid.SetColumn(image, 0);
                Grid.SetRow(image, 0);
            }
            public IconSymbol(double width, double height) {
                image.HorizontalAlignment = HorizontalAlignment.Stretch;
                image.VerticalAlignment = VerticalAlignment.Stretch;
                image.Width = width;
                image.Height = height;

                ColumnDefinition c = new ColumnDefinition();
                c.Width = new GridLength(1, GridUnitType.Star);
                grid.ColumnDefinitions.Add(c);

                RowDefinition r = new RowDefinition();
                r.Height = new GridLength(1, GridUnitType.Star);
                grid.RowDefinitions.Add(r);

                grid.Children.Add(image);
                Grid.SetColumn(image, 0);
                Grid.SetRow(image, 0);
            }


            public void SetGridPosition(Grid parent, int row, int column) {
                if (grid.Parent.GetType() == typeof(Grid)) {
                    parent.Children.Add(grid);
                    Grid.SetColumn(grid, column);
                    Grid.SetRow(grid, row);
                }
            }
        }

        class ConsoleWindow {
            private Border border = new Border();
            public UIElement UIElement => border;

            private List<string> content = new List<string>();
            private Grid parentGrid = new Grid();
            private Grid topGrid = new Grid();
            private Grid bottomGrid = new Grid();
            private ConsoleText text;
            private ConsoleScrollbar scrollbar;
            private QuickSettings settings;
            private double arrowButtonWidth;


            public ConsoleWindow(double pArrowButtonWidht) {
                arrowButtonWidth = pArrowButtonWidht;
                text = new ConsoleText(this);
                scrollbar = new ConsoleScrollbar(text, pArrowButtonWidht);
                InitGrid();

                border.Child = parentGrid;
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#707070"));
                border.BorderThickness = new Thickness(1);
                border.CornerRadius = new CornerRadius(0);

                text.UIElement.Loaded += scrollbar.AdjustSlider;
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
                ColumnDefinition c = new ColumnDefinition();
                c.Width = new GridLength(1, GridUnitType.Star);

                // Top/ConsoleAndScrollbar Grid
                RowDefinition r1 = new RowDefinition();
                r1.Height = new GridLength(1, GridUnitType.Star);

                // Bottom/qSettings Grid
                RowDefinition r2 = new RowDefinition();
                r2.Height = new GridLength(0, GridUnitType.Pixel);

                parentGrid.ColumnDefinitions.Add(c);
                parentGrid.RowDefinitions.Add(r1);
                parentGrid.RowDefinitions.Add(r2);
            }
            private void InitBottomGrid() {
                ColumnDefinition c1 = new ColumnDefinition();
                c1.Width = new GridLength(1, GridUnitType.Star);

                RowDefinition r1 = new RowDefinition();
                r1.Height = new GridLength(1, GridUnitType.Star);

                bottomGrid.ColumnDefinitions.Add(c1);
                bottomGrid.RowDefinitions.Add(r1);
            }
            private void InitTopGrid() {
                ColumnDefinition c1 = new ColumnDefinition();
                c1.Width = new GridLength(1, GridUnitType.Star);

                ColumnDefinition c2 = new ColumnDefinition();
                c2.Width = new GridLength(arrowButtonWidth, GridUnitType.Pixel);

                RowDefinition r1 = new RowDefinition();
                r1.Height = new GridLength(1, GridUnitType.Star);

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
                    StringBuilder b = new StringBuilder();
                    foreach (var s in content) {
                        b.Append(s);
                        b.Append('\n');
                    }
                    return b.ToString();
                }
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
                    textBox = new TextBox();
                    textBox.Style = Application.Current.Resources["ConsoleStyle"] as Style;
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

                public int VisibleLines => textBox.GetLastVisibleLineIndex() - textBox.GetFirstVisibleLineIndex();
            }

            class ConsoleScrollbar {
                private Grid grid = new Grid();
                private Button upArrow = new Button();
                private Button downArrow = new Button();
                private Rectangle slider = new Rectangle();
                private ConsoleText textBox;

                private DispatcherTimer arrowTimer = new DispatcherTimer();
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
                    slider.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3d3d3d"));
                    slider.MouseLeftButtonDown += Slider_MouseLeftButtonDown;
                    slider.MouseLeftButtonUp += Slider_MouseLeftButtonUp;
                    slider.Width = arrowWidth;
                    slider.Height = 50;


                    grid.Children.Add(upArrow);
                    Grid.SetColumn(upArrow, 0);
                    Grid.SetRow(upArrow, 0);

                    grid.Children.Add(slider);
                    Grid.SetColumn(slider, 0);
                    Grid.SetRow(slider, 1);

                    grid.Children.Add(downArrow);
                    Grid.SetColumn(downArrow, 0);
                    Grid.SetRow(downArrow, 2);
                }
                private void InitGrid() {
                    grid.VerticalAlignment = VerticalAlignment.Stretch;

                    ColumnDefinition c1 = new ColumnDefinition();
                    c1.Width = new GridLength(1, GridUnitType.Star);

                    RowDefinition r1 = new RowDefinition();
                    r1.Height = new GridLength(arrowWidth, GridUnitType.Pixel);
                    RowDefinition r2 = new RowDefinition();
                    r2.Height = new GridLength(1, GridUnitType.Star);
                    RowDefinition r3 = new RowDefinition();
                    r3.Height = new GridLength(arrowWidth, GridUnitType.Pixel);

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
                    arrowTimer = new DispatcherTimer();
                    arrowTimer.Interval = TimeSpan.FromSeconds(activationThreshhold);
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
                    arrowTimer = new DispatcherTimer();
                    arrowTimer.Interval = TimeSpan.FromSeconds(activationThreshhold);
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
                    double mousePosDelta = mousePos - (slider.Margin.Top + minHandleHeight_relToTheWindow); // Mouse Position Relative to the handle 
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

                    //consoles[0].Text = "mousePos: " + mousePos.ToString() + "\n";
                    //WriteLineOnConsoleOfIndex(0, "newY: " + newY);
                    //WriteLineOnConsoleOfIndex(0, "Delta: " + mousePosDelta);
                    //WriteLineOnConsoleOfIndex(0, "minHeight: " + minHeight);
                    //WriteLineOnConsoleOfIndex(0, "maxHeight: " + maxHeight);
                    //WriteLineOnConsoleOfIndex(0, "newLineIndex: " + newLineIndex);
                    //WriteLineOnConsoleOfIndex(0, ConsoleBox.GetFirstVisibleLineIndex() + ", " + ConsoleBox.GetLastVisibleLineIndex());
                }
                public void AdjustSlider() {
                    double visLines = textBox.VisibleLines;
                    double totalLines = textBox.UIElement.LineCount;
                    double scrollBarHeight = grid.RowDefinitions[1].ActualHeight;
                    double newHeight = scrollBarHeight * (visLines / totalLines);
                    if (totalLines < visLines) {
                        newHeight = scrollBarHeight;
                    }
                    slider.Height = newHeight;

                    if (!isScrolling) {
                        double newLineIndex = textBox.UIElement.GetFirstVisibleLineIndex();
                        double maxHeight = grid.RowDefinitions.ElementAt(1).ActualHeight;
                        double newY = ((newLineIndex / textBox.UIElement.LineCount) * maxHeight);
                        slider.Margin = new Thickness(0, newY, 0, 0);
                    }
                }
                public void AdjustSlider(object sender, EventArgs e) {
                    double visLines = textBox.VisibleLines;
                    double totalLines = textBox.UIElement.LineCount;
                    double scrollBarHeight = grid.RowDefinitions[1].ActualHeight;
                    double newHeight = scrollBarHeight * (visLines / totalLines);
                    if (totalLines < visLines) {
                        newHeight = scrollBarHeight;
                    }
                    slider.Height = newHeight;

                    if (!isScrolling) {
                        double newLineIndex = textBox.UIElement.GetFirstVisibleLineIndex();
                        double maxHeight = grid.RowDefinitions.ElementAt(1).ActualHeight;
                        double newY = ((newLineIndex / textBox.UIElement.LineCount) * maxHeight);
                        slider.Margin = new Thickness(0, newY, 0, 0);
                    }
                }
            }
        }

        enum DockDirection {
            Right,
            Bottom
        }

        class DropDownMenu {
            private Border border = new Border();
            public UIElement UIElement => border;
            private StackPanel verticalPanel = new StackPanel();

            public DropDownMenu(DockDirection dockDir, UIElement parent) {
                verticalPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3d3d3d"));
                verticalPanel.Orientation = Orientation.Vertical;
                border.Child = verticalPanel;
                border.Style = Application.Current.Resources["ClientButtonUnfoldMenu_Style"] as Style;

                Point p = Helper.GetAbsolutePosition(parent);
                switch (dockDir) {
                    case DockDirection.Bottom:
                        border.RenderTransform = new TranslateTransform(p.X, p.Y + ((Button)parent).ActualHeight);
                        break;

                    case DockDirection.Right:
                        border.RenderTransform = new TranslateTransform(p.X + ((Button)parent).ActualHeight, p.Y);
                        break;
                }
            }

            public void AddOption(MenuOption option) {
                verticalPanel.Children.Add(option.UIElement);
                UpdateLayout();
            }

            public void UpdateLayout() {
                double maxWidth = 0;
                var grids = verticalPanel.Children.OfType<Grid>();

                //Name
                maxWidth = grids.Max(g => Helper.GetActualColumnWidth(g, 1));
                foreach (var grid in grids) {
                    grid.ColumnDefinitions[1].Width = new GridLength(maxWidth);
                }

                //Shortcut
                maxWidth = 0;
                maxWidth = grids.Max(g => Helper.GetActualColumnWidth(g, 2));
                foreach (var grid in grids) {
                    grid.ColumnDefinitions[2].Width = new GridLength(maxWidth);
                }
            }

            public class MenuOption {
                private Grid grid = new Grid();
                public UIElement UIElement => grid;

                public IconSymbol symbol;
                public Label name = new Label();
                public Label keyboardShortCut = new Label();
                public Label arrow = new Label();

                private double height;
                private double symbolOffset = 3;
                private DropDownMenu menu;

                public MenuOption(double height) {
                    arrow.Content = " ";
                    symbol = new IconSymbol(height - symbolOffset * 2, height - symbolOffset * 2);
                    this.height = height;


                    ColumnDefinition symbolCol = new ColumnDefinition();
                    symbolCol.Width = new GridLength(height, GridUnitType.Pixel);
                    grid.ColumnDefinitions.Add(symbolCol);

                    ColumnDefinition nameCol = new ColumnDefinition();
                    nameCol.Width = new GridLength(1, GridUnitType.Auto);
                    grid.ColumnDefinitions.Add(nameCol);

                    ColumnDefinition shortcutCol = new ColumnDefinition();
                    shortcutCol.Width = new GridLength(1, GridUnitType.Auto);
                    grid.ColumnDefinitions.Add(shortcutCol);

                    ColumnDefinition arrowCol = new ColumnDefinition();
                    arrowCol.Width = new GridLength(height, GridUnitType.Pixel);
                    grid.ColumnDefinitions.Add(arrowCol);


                    Helper.SetChildInGrid(grid, symbol.UIElement, 0, 0);
                    Helper.SetChildInGrid(grid, name, 0, 1);
                    Helper.SetChildInGrid(grid, keyboardShortCut, 0, 2);
                    Helper.SetChildInGrid(grid, arrow, 0, 3);
                }

                public MenuOption AddSymbol(string path) {
                    symbol = new IconSymbol(path, height - symbolOffset * 2, height - symbolOffset * 2);
                    return this;
                }
                public MenuOption SetName(string nameText) {
                    name.Content = nameText;
                    name.Foreground = Brushes.White;
                    name.VerticalAlignment = VerticalAlignment.Top;
                    name.HorizontalAlignment = HorizontalAlignment.Left;
                    return this;
                }
                public MenuOption SetKeyboardShortcut(string kShortcut) {
                    keyboardShortCut.Content = kShortcut;
                    keyboardShortCut.Foreground = Brushes.White;
                    keyboardShortCut.VerticalAlignment = VerticalAlignment.Top;
                    keyboardShortCut.HorizontalAlignment = HorizontalAlignment.Left;
                    return this;
                }
                public MenuOption AddDropdownMenu(DropDownMenu menu) {
                    arrow.Content = ">";
                    arrow.Foreground = Brushes.White;
                    keyboardShortCut.VerticalAlignment = VerticalAlignment.Top;
                    keyboardShortCut.HorizontalAlignment = HorizontalAlignment.Left;
                    this.menu = menu;
                    return this;
                }
                public MenuOption AddCommand(Action command) {
                    grid.MouseLeftButtonUp += (sender, e) => command();
                    return this;
                }
            }

        }

        static class Helper {
            public static void SetChildInGrid(Grid grid, UIElement child, int row, int column) {
                grid.Children.Add(child);
                Grid.SetColumn(child, column);
                Grid.SetRow(child, row);
            }

            public static Point GetAbsolutePosition(UIElement element) {
                Point position = element.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));

                return position;
            }

            public static double GetActualColumnWidth(Grid grid, int columnIndex) {
                grid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
                var columnWidth = grid.ColumnDefinitions[columnIndex].ActualWidth;
                return columnWidth;
            }

            public static double GetActualRowHeight(Grid grid, int rowIndex) {
                grid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
                var rowHeight = grid.RowDefinitions[rowIndex].ActualHeight;
                return rowHeight;
            }
        }

    }
}
