using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media;
using System;
using System.Threading.Tasks;
using JsonViewer.Model;
using System.Windows.Threading;

namespace JsonViewer
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region ############################ INPUT UTENTE ############################
        private void OpenJsonFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "File JSON (*.json)|*.json|Tutti i file (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                FilePathLabel.Text = dialog.FileName;
                CaricaFileJSON(dialog.FileName);
            }
        }
        private void EspandiTutto_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in JsonTreeView.Items)
            {
                ExpandNodeRecursive((TreeViewItem)item);
            }
        }
        private void CollassaTutto_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in JsonTreeView.Items)
            {
                CollapseNodeRecursive((TreeViewItem)item);
            }
        }
        #endregion

        #region ############################ CREAZIONE ALBERO ############################
        private async void CaricaFileJSON(string path)
        {
            try
            {
                LoadingBar.Visibility = Visibility.Visible;
                JsonTreeView.Visibility = Visibility.Hidden;

                JsonNode rootNode = await Task.Run(() =>
                {
                    string jsonContent = File.ReadAllText(path);
                    JToken token = JToken.Parse(jsonContent);
                    return BuildLogicalTree("", token);
                });

                JsonTreeView.Items.Clear();
                //AddNodesToTreeView(rootNode);
                JsonTreeView.Items.Add(BuildVisualTree(rootNode));
                JsonTreeView.Visibility = Visibility.Visible;
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
            string header = node.Value != null
                ? $"{node.Name}: {node.Value}"
                : node.Name;

            TextBlock textBlock = new TextBlock
            {
                Text = header,
                Foreground = GetColorForValue(node.Type)
            };

            TreeViewItem item = new TreeViewItem { Header = textBlock };

            foreach (JsonNode child in node.Children)
            {
                item.Items.Add(BuildVisualTree(child));
            }

            return item;
        }
        private void AddNodesToTreeView(JsonNode node)
        {
            // Aggiungi il nodo principale
            TreeViewItem treeViewItem = BuildVisualTree(node);
            JsonTreeView.Items.Add(treeViewItem);

            // Aggiungi i figli in modo incrementale, senza bloccare la UI
            foreach (JsonNode child in node.Children)
            {
                // Per ogni figlio, aggiungilo con un piccolo delay, garantendo un caricamento progressivo
                Dispatcher.InvokeAsync(() =>
                {
                    treeViewItem.Items.Add(BuildVisualTree(child));
                }, DispatcherPriority.Background);  // Usando una bassa priorità per non bloccare la UI
            }
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
        private void ExpandNodeRecursive(TreeViewItem item)
        {
            item.IsExpanded = true;

            foreach (var childItem in item.Items)
            {
                if (childItem is TreeViewItem child)
                {
                    ExpandNodeRecursive(child);
                }
            }
        }
        private void CollapseNodeRecursive(TreeViewItem item)
        {
            item.IsExpanded = false;

            foreach (var childItem in item.Items)
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
