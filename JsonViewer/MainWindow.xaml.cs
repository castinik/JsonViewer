using JsonViewer.Model;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace JsonViewer
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private delegate void AutoDelegate();
        private Span _lastSelectedSpan;

        public MainWindow()
        {
            InitializeComponent();
        }

        #region ############################ INPUT UTENTE ############################
        private void OpenJsonFile_Left_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "File JSON (*.json)|*.json|Tutti i file (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                FilePathLabel.Text = dialog.FileName;
                CaricaFileJSON(dialog.FileName, TreeType.Left);
            }
        }
        private void OpenJsonFile_Right_Click(object sender, RoutedEventArgs eventArgs)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "File JSON (*.json)|*.json|Tutti i file (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                FilePathLabelRight.Text = dialog.FileName;
                CaricaFileJSON(dialog.FileName, TreeType.Right);
            }
        }
        private void EspandiTutto_Left_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in JsonTreeView.Items)
            {
                ExpandNodeRecursive((TreeViewItem)item, true, TreeType.Left);
            }
        }
        private void EspandiTutto_Right_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in JsonTreeViewRight.Items)
            {
                ExpandNodeRecursive((TreeViewItem)item, true, TreeType.Right);
            }
        }
        private void CollassaTutto_Left_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in JsonTreeView.Items)
            {
                CollapseNodeRecursive((TreeViewItem)item);
            }
        }
        private void CollassaTutto_Right_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in JsonTreeViewRight.Items)
            {
                CollapseNodeRecursive((TreeViewItem)item);
            }
        }
        #endregion

        #region ############################ CREAZIONE ALBERO ############################
        private async void CaricaFileJSON(string path, TreeType type)
        {
            TreeView treeView = new TreeView();
            switch (type)
            {
                case TreeType.Left:
                    treeView = JsonTreeView;
                    break;
                case TreeType.Right:
                    treeView = JsonTreeViewRight;
                    break;
            }
            try
            {
                LoadingBar.Visibility = Visibility.Visible;
                treeView.Visibility = Visibility.Hidden;

                JsonNode rootNode = await Task.Run(() =>
                {
                    string jsonContent = File.ReadAllText(path);
                    JToken token = JToken.Parse(jsonContent);
                    return BuildLogicalTree("Json", token);
                });

                treeView.Items.Clear();
                treeView.Items.Add(BuildVisualTree(rootNode));
                treeView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore: {ex.Message}");
            }
            finally
            {
                LoadingBar.Visibility = Visibility.Collapsed;
            }
        }
        private JsonNode BuildLogicalTree(string name, JToken token)
        {
            JsonNode node = new JsonNode
            {
                Name = name,
                Type = token.Type,
                Value = token is JValue value ? GetValuePreview(value) : null
            };

            if (token is JObject obj)
            {
                foreach (JProperty prop in obj.Properties())
                {
                    node.Children.Add(BuildLogicalTree(prop.Name, prop.Value));
                }
            }
            else if (token is JArray array)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    node.Children.Add(BuildLogicalTree($"[{i}]", array[i]));
                }
            }

            return node;
        }

        private TreeViewItem BuildVisualTree(JsonNode node)
        {
            TextBlock textBlock = GetTextNode(node);

            TreeViewItem item = new TreeViewItem { Header = textBlock, Tag = node };

            foreach (JsonNode child in node.Children)
            {
                TextBlock childTextBlock = GetTextNode(child);
                TreeViewItem childItem = new TreeViewItem { Header = childTextBlock, Tag = child };
                item.Items.Add(childItem);
            }
            item.Expanded += TreeViewItem_Expanded;
            item.Collapsed += TreeViewItem_Collapsed;
            return item;
        }
        private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.RoutedEvent != null) e.Handled = true;

            TreeViewItem item = sender as TreeViewItem;
            if (item == null)
            {
                return;
            }

            JsonNode node = item.Tag as JsonNode;
            if (node.Children.Count == 0) return;
            if (node.IsExpanded)
            {
                item.Header = OpenParentesis(node);
                return;
            }
            if (node.Children.Count > 0)
            {
                item.Header = GetTextNode(node, "Loading...");
                item.Items.Clear();
                await Task.Yield();
                await Task.Run(() => { Thread.Sleep(50); });

                foreach (JsonNode child in node.Children)
                {
                    item.Items.Add(BuildVisualTree(child));
                }
                item.Header = OpenParentesis(node);
                item.Items.Add(CloseParentesis(node));
            }
            node.IsExpanded = true;
        }
        private async void TreeViewItem_Collapsed(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            TreeViewItem item = sender as TreeViewItem;
            if (item == null)
            {
                return;
            }
            JsonNode node = item.Tag as JsonNode;
            item.Header = GetTextNode(node);
        }

        private void TreeItemSelected(JsonNode node, Span span)
        {
            if (_lastSelectedSpan != null)
                _lastSelectedSpan.Background = Brushes.Transparent;

            span.Background = Brushes.LightGray;
            _lastSelectedSpan = span;
        }

        //private void TreeItemSelected(JsonNode node, TreeType type)
        //{
        //    if (node.Children.Count > 0) return;

        //    switch (type)
        //    {
        //        case TreeType.Left:

        //            break;
        //        case TreeType.Right:

        //            break;
        //    }
        //}
        #endregion

        #region ############################ FORMATTAZIONE ############################
        private TextBlock OpenParentesis(JsonNode node)
        {
            string header = String.Empty;
            switch (node.Type)
            {
                case JTokenType.Array:
                    header = $"[{node.Children.Count} items";
                    break;
                case JTokenType.Object:
                    header = $"{{{node.Children.Count} props";
                    break;
            }

            TextBlock textBlock = new TextBlock();
            textBlock.Inlines.Add(new Run($"{node.Name} : ") { Foreground = Brushes.DarkBlue });
            textBlock.Inlines.Add(new Run(header) { Foreground = GetColorForValue(node.Type) });
            textBlock.FontSize = 20;

            return textBlock;
        }
        private TreeViewItem CloseParentesis(JsonNode node)
        {
            string header = String.Empty;
            switch (node.Type)
            {
                case JTokenType.Array:
                    header = "]";
                    break;
                case JTokenType.Object:
                    header = "}";
                    break;
            }
            TreeViewItem item = new TreeViewItem
            {
                Header = header,
                Foreground = GetColorForValue(node.Type)
            };
            item.FontSize = 20;
            return item;
        }
        private TextBlock GetTextNode(JsonNode node, string message = "")
        {
            String valueType = String.Empty;
            if (String.IsNullOrEmpty(message))
            {
                switch (node.Type)
                {
                    case JTokenType.Array:
                        valueType = $"[{node.Children.Count} items]";
                        break;
                    case JTokenType.Object:
                        valueType = $"{{{node.Children.Count} props}}";
                        break;
                    case JTokenType.Null:
                        valueType = " null";
                        break;
                    default:
                        valueType = node.Value != null ? $"{node.Value}  ({node.Type.ToString()})" : $" ({node.Type.ToString()})";
                        break;
                }
            }
            else
            {
                switch (node.Type)
                {
                    case JTokenType.Array:
                        valueType = $"[{message}";
                        break;
                    case JTokenType.Object:
                        valueType = $"{{{message}";
                        break;
                }
            }

            TextBlock textBlock = new TextBlock();
            Span nameSpan = new Span(new Run($"{node.Name} : ") { Foreground = Brushes.DarkBlue });
            Span valueSpan = new Span(new Run(valueType) { Foreground = GetColorForValue(node.Type) });

            nameSpan.MouseLeftButtonDown += (s, e) => TreeItemSelected(node, nameSpan);
            valueSpan.MouseLeftButtonDown += (s, e) => TreeItemSelected(node, valueSpan);

            textBlock.Inlines.Add(nameSpan);
            textBlock.Inlines.Add(valueSpan);
            textBlock.FontSize = 20;
            //textBlock.MouseLeftButtonDown += JsonTreeViewLeft_SelectedItemChanged;
            return textBlock;
        }
        private string GetValuePreview(JValue token)
        {
            switch (token.Type)
            {
                case JTokenType.String:
                    return $"\"{token.Value<string>()}\"";  // Gestiamo stringhe

                case JTokenType.Integer:
                    return token.Value<int>().ToString();  // Gestiamo numeri interi

                case JTokenType.Float:
                    return token.Value<float>().ToString("G");  // Gestiamo numeri float

                case JTokenType.Boolean:
                    return token.Value<bool>() ? "true" : "false";  // Gestiamo booleani

                case JTokenType.Null:
                    return "null";  // Gestiamo valori nulli

                case JTokenType.Date:
                    return token.Value<DateTime>().ToString("yyyy-MM-dd HH:mm:ss");  // Gestiamo date

                case JTokenType.Array:
                    return "[Array]";  // Gestiamo array

                case JTokenType.Object:
                    return "{Object}";  // Gestiamo oggetti

                case JTokenType.Bytes:
                    return "[Bytes]";  // Gestiamo byte

                case JTokenType.Guid:
                    return token.Value<Guid>().ToString();  // Gestiamo GUID

                case JTokenType.Uri:
                    return token.Value<Uri>().ToString();  // Gestiamo Uri

                case JTokenType.TimeSpan:
                    return token.Value<TimeSpan>().ToString();  // Gestiamo TimeSpan

                default:
                    return token.ToString();  // Fallback per ogni altro tipo non previsto
            }
        }
        private Brush GetColorForValue(JTokenType token)
        {
            switch (token)
            {
                case JTokenType.String:
                    return Brushes.DarkGreen;  // Colore per le stringhe

                case JTokenType.Integer:
                    return Brushes.DarkOrange;  // Colore per numeri interi

                case JTokenType.Float:
                    return Brushes.Orange;  // Colore per numeri float

                case JTokenType.Boolean:
                    return Brushes.Blue;  // Colore per booleani

                case JTokenType.Null:
                    return Brushes.Gray;  // Colore per valori nulli

                case JTokenType.Date:
                    return Brushes.DarkCyan;  // Colore per le date

                case JTokenType.Array:
                    return Brushes.Teal;  // Colore per gli array

                case JTokenType.Object:
                    return Brushes.Purple;  // Colore per gli oggetti

                case JTokenType.Bytes:
                    return Brushes.Brown;  // Colore per i byte

                case JTokenType.Guid:
                    return Brushes.Magenta;  // Colore per GUID

                case JTokenType.Uri:
                    return Brushes.Indigo;  // Colore per Uri

                case JTokenType.TimeSpan:
                    return Brushes.Pink;  // Colore per TimeSpan

                default:
                    return Brushes.Black;  // Colore di default per tipi non previsti
            }
        }
        #endregion

        #region ############################ ESPANDI E COLLASSA ############################
        private async Task ExpandNodeRecursive(TreeViewItem item, bool isFirstFunc = false, TreeType type = TreeType.Default)
        {
            if (item == null) return;

            JsonNode node = item.Tag as JsonNode;
            if (node == null) return;

            // Disabilito gli altri pulsanti
            if (isFirstFunc)
            {
                switch (type)
                {
                    case TreeType.Left:
                        btnCollapseLeft.IsEnabled = false;
                        btnExpandLeft.IsEnabled = false;
                        btnOpenLeft.IsEnabled = false;
                        break;
                    case TreeType.Right:
                        btnCollapseRight.IsEnabled = false;
                        btnExpandRight.IsEnabled = false;
                        btnOpenRight.IsEnabled = false;
                        break;
                }
            }

            // Se il nodo è gia stato espanso
            if (!node.IsExpanded)
            {
                // Se il nodo ha figli
                if (node.Children.Count > 0)
                {
                    item.Header = GetTextNode(node, "Loading...");
                    item.Items.Clear();

                    // Lascia respirare la UI
                    await Task.Yield();
                    await Task.Run(() => Thread.Sleep(50));

                    foreach (JsonNode child in node.Children)
                    {
                        item.Items.Add(BuildVisualTree(child));
                    }

                    item.Header = OpenParentesis(node);
                    item.Items.Add(CloseParentesis(node));
                }
                node.IsExpanded = true;
            }
            // Espande visivamente il nodo
            item.IsExpanded = true;
            if (node.Children.Count == 0)
            {
                item.Header = GetTextNode(node);
            }
            // Delay leggero per non congelare l'interfaccia
            await Task.Delay(10);

            foreach (TreeViewItem child in item.Items)
            {
                await ExpandNodeRecursive(child);
            }

            // Riabilito gli altri pulsanti
            if (isFirstFunc)
            {
                switch (type)
                {
                    case TreeType.Left:
                        btnCollapseLeft.IsEnabled = true;
                        btnExpandLeft.IsEnabled = true;
                        btnOpenLeft.IsEnabled = true;
                        break;
                    case TreeType.Right:
                        btnCollapseRight.IsEnabled = true;
                        btnExpandRight.IsEnabled = true;
                        btnOpenRight.IsEnabled = true;
                        break;
                }
            }
        }
        private void CollapseNodeRecursive(TreeViewItem item)
        {
            item.IsExpanded = false;

            foreach (ItemsControl childItem in item.Items)
            {
                if (childItem is TreeViewItem child)
                {
                    CollapseNodeRecursive(child);
                }
            }
        }
        #endregion
    }
}
