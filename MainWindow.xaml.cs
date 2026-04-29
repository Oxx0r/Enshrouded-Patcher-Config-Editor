using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Linq;
using System.Globalization;
using System.Reflection;

namespace EnshroudedConfigManager
{
    public partial class MainWindow : Window
    {
        private ConfigRoot? _config;
        private string _filePath = "config.json";
        private Dictionary<string, Control> _inputRefs = new Dictionary<string, Control>();

        public MainWindow()
        {
            InitializeComponent();
            SetWindowTitleWithVersion();
            LoadAndBuildUI();
        }

        private void SetWindowTitleWithVersion()
        {
            // Gets the version from the Assembly (defined in .csproj)
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            string versionString = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
            this.Title = $"Enshrouded Patcher Config Editor v{versionString} by Oxx0r";
        }

        private void LoadAndBuildUI()
        {
            if (!File.Exists(_filePath))
            {
                MessageBox.Show("Error: 'config.json' not found!\n\nPlease make sure the file is in the same folder as this application.", 
                                "File Missing", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            try 
            {
                string jsonContent = File.ReadAllText(_filePath);
                jsonContent = Regex.Replace(jsonContent, @"(?<="":\s*"")(.*?)(?="")", m => m.Value.Replace("\\", "\\\\"));

                _config = JsonConvert.DeserializeObject<ConfigRoot>(jsonContent);
                if (_config == null) return;

                TxtVersion.Text = _config.kfcParserVersion;
                BuildPathSection();
                BuildModSections();
                BuildSettingsSection();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Load Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown();
            }
        }

        private void BuildPathSection()
        {
            if (_config == null) return;
            AddPathRow("gameDirectory", _config.gameDirectory);
            AddPathRow("outputDirectory", _config.outputDirectory);
        }

        private void AddPathRow(string label, string value)
        {
            var dock = new DockPanel { Margin = new Thickness(0, 2, 0, 2) };
            var lbl = new TextBlock { Text = label + ":", Width = 120, VerticalAlignment = VerticalAlignment.Center };
            var txt = new TextBox { Text = value, Background = new SolidColorBrush(Color.FromRgb(43, 43, 43)), Foreground = Brushes.White };
            _inputRefs["TOP_" + label] = txt;
            dock.Children.Add(lbl);
            dock.Children.Add(txt);
            PathSection.Children.Add(dock);
        }

        private void BuildModSections()
        {
            if (_config == null) return;
            string[] cats = { "player", "inventory", "world", "gameplay" };
            
            foreach (var catName in cats)
            {
                var propInfo = _config.GetType().GetProperty(catName);
                if (propInfo == null) continue;
                var catObj = (JObject)propInfo.GetValue(_config)!;
                
                var header = new TextBlock { Text = catName.ToUpper(), Foreground = new SolidColorBrush(Color.FromRgb(58, 126, 191)), FontWeight = FontWeights.Bold, Margin = new Thickness(0, 15, 0, 5) };
                ModSection.Children.Add(header);

                foreach (var mod in catObj)
                {
                    var modKey = mod.Key;
                    var modData = (JObject)mod.Value!;

                    var container = new Border { Background = new SolidColorBrush(Color.FromRgb(35, 35, 35)), Margin = new Thickness(0, 2, 0, 2), Padding = new Thickness(8), CornerRadius = new CornerRadius(3) };
                    var grid = new Grid();
                    grid.RowDefinitions.Add(new RowDefinition());
                    grid.RowDefinitions.Add(new RowDefinition());

                    var cbEnabled = new CheckBox { Content = modKey, IsChecked = (bool?)modData["enabled"] ?? false, Foreground = Brushes.White, FontWeight = FontWeights.Bold, VerticalContentAlignment = VerticalAlignment.Center };
                    _inputRefs[$"{catName}_{modKey}_enabled"] = cbEnabled;
                    grid.Children.Add(cbEnabled);

                    var stackValues = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                    foreach (var p in modData.Properties())
                    {
                        if (p.Name == "enabled" || p.Name == "description") continue;

                        if (p.Value.Type == JTokenType.Boolean)
                        {
                            var subCb = new CheckBox { 
                                Content = p.Name, 
                                IsChecked = (bool)p.Value, 
                                Foreground = Brushes.LightGray, 
                                FontSize = 10, 
                                Margin = new Thickness(10, 0, 5, 0),
                                VerticalContentAlignment = VerticalAlignment.Center
                            };
                            _inputRefs[$"{catName}_{modKey}_{p.Name}"] = subCb;
                            stackValues.Children.Add(subCb);
                        }
                        else 
                        {
                            stackValues.Children.Add(new TextBlock { Text = p.Name + ":", FontSize = 10, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 2, 0), Foreground = Brushes.LightGray });
                            var txtVal = new TextBox { Text = p.Value.ToString(Formatting.None).Trim('"'), Width = 50, Height = 20, FontSize = 10, Background = Brushes.Black, Foreground = Brushes.White };
                            _inputRefs[$"{catName}_{modKey}_{p.Name}"] = txtVal;
                            stackValues.Children.Add(txtVal);
                        }
                    }
                    grid.Children.Add(stackValues);

                    if (modData["description"] != null)
                    {
                        var desc = new TextBlock { Text = modData["description"]!.ToString(), FontSize = 10, Foreground = Brushes.Gray, Margin = new Thickness(28, 4, 0, 0), TextWrapping = TextWrapping.Wrap };
                        Grid.SetRow(desc, 1);
                        grid.Children.Add(desc);
                    }
                    container.Child = grid;
                    ModSection.Children.Add(container);
                }
            }
        }

        private void BuildSettingsSection()
        {
            if (_config == null) return;
            var header = new TextBlock { Text = "INTERNAL SETTINGS", Foreground = new SolidColorBrush(Color.FromRgb(58, 126, 191)), FontWeight = FontWeights.Bold, Margin = new Thickness(0, 20, 0, 5) };
            SettingsSection.Children.Add(header);
            var panel = new WrapPanel();
            foreach (var s in _config.settings)
            {
                var cb = new CheckBox { Content = s.Key, IsChecked = s.Value, Margin = new Thickness(0, 5, 20, 5), Foreground = Brushes.White };
                _inputRefs["SET_" + s.Key] = cb;
                panel.Children.Add(cb);
            }
            SettingsSection.Children.Add(panel);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_config == null) return;
            try
            {
                _config.gameDirectory = ((TextBox)_inputRefs["TOP_gameDirectory"]).Text;
                _config.outputDirectory = ((TextBox)_inputRefs["TOP_outputDirectory"]).Text;
                _config.kfcParserVersion = TxtVersion.Text;

                string[] cats = { "player", "inventory", "world", "gameplay" };
                foreach (var catName in cats)
                {
                    var propInfo = _config.GetType().GetProperty(catName);
                    var catObj = (JObject)propInfo!.GetValue(_config)!;
                    foreach (var mod in catObj)
                    {
                        var modKey = mod.Key;
                        var modData = (JObject)mod.Value!;

                        modData["enabled"] = ((CheckBox)_inputRefs[$"{catName}_{modKey}_enabled"]).IsChecked ?? false;

                        foreach (var prop in modData.Properties().ToList())
                        {
                            if (prop.Name == "enabled" || prop.Name == "description") continue;
                            
                            var control = _inputRefs[$"{catName}_{modKey}_{prop.Name}"];

                            if (control is CheckBox cb)
                            {
                                modData[prop.Name] = cb.IsChecked ?? false;
                            }
                            else if (control is TextBox tb)
                            {
                                string rawValue = tb.Text.Trim();
                                if (double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double dbl))
                                {
                                    if (dbl % 1 == 0)
                                        modData[prop.Name] = (long)dbl;
                                    else
                                        modData[prop.Name] = dbl;
                                }
                                else
                                {
                                    modData[prop.Name] = tb.Text;
                                }
                            }
                        }
                    }
                }

                foreach (var key in _config.settings.Keys.ToList())
                {
                    _config.settings[key] = ((CheckBox)_inputRefs["SET_" + key]).IsChecked ?? false;
                }

                string json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                json = json.Replace("\\\\", "\\");
                File.WriteAllText(_filePath, json);
                MessageBox.Show("Configuration saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("Error while saving: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}