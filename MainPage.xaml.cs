using Markdig;
using HandlebarsDotNet;
using SelectPdf;
using YamlDotNet.Serialization;

namespace DocGenerator
{
    /// <summary>
    /// Основной класс MainPage
    /// </summary>
    public partial class MainPage : ContentPage
    {
        private string _markdownPath;
        private string _templatePath;
        private string _savePath;
        /// <summary>
        /// Конструктор класса MainPage
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public MainPage()
        {
            Handlebars.RegisterHelper("eq", (output, context, arguments) =>
            {
                if (arguments.Length < 2)
                {
                    throw new ArgumentException("Хелпер 'eq' требует как минимум два аргумента.");
                }

                var first = arguments[0];
                var second = arguments[1];

                if (first == null && second == null)
                {
                    output.WriteSafeString("true");
                }
                else if (first?.Equals(second) == true)
                {
                    output.WriteSafeString("true");
                }
                else
                {
                    output.WriteSafeString("false");
                }
            });

            Handlebars.RegisterHelper("neq", (output, context, arguments) =>
            {
                if (arguments.Length < 2)
                {
                    throw new ArgumentException("Хелпер 'neq' требует как минимум два аргумента.");
                }

                var first = arguments[0];
                var second = arguments[1];

                if (first == null && second == null)
                {
                    output.WriteSafeString("false");
                }
                else if (first?.Equals(second) != true)
                {
                    output.WriteSafeString("true");
                }
                else
                {
                    output.WriteSafeString("false");
                }
            });

            InitializeComponent();
        }

        /// <summary>
        /// Обработчик кнопки для выбора Markdown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SelectMarkdown_Clicked(object sender, EventArgs e)
        {
            var result = await FilePicker.PickAsync();
            if (result != null)
                _markdownPath = result.FullPath;
        }
        /// <summary>
        /// Обработчик кнопки для выбора шаблона YAML
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SelectTemplate_Clicked(object sender, EventArgs e)
        {
            var result = await FilePicker.PickAsync();
            if (result != null)
                _templatePath = result.FullPath;
        }
        /// <summary>
        /// Загрузка шаблона HTML
        /// </summary>
        /// <returns></returns>
        private string LoadHtmlTemplate()
        {
            using var stream = typeof(MainPage).Assembly.GetManifestResourceStream("DocGenerator.Resources.Raw.html_template.html");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        /// <summary>
        /// Обработчик кнопки генерации документа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GenerateDocument_Clicked(object sender, EventArgs e)
        {
            try
            {
                var markdown = !string.IsNullOrWhiteSpace(MarkdownEditor.Text)
                    ? MarkdownEditor.Text
                    : (!string.IsNullOrEmpty(_markdownPath) && File.Exists(_markdownPath)
                    ? File.ReadAllText(_markdownPath)
                    : GetDefaultMarkdown());

                var yamlTemplate = !string.IsNullOrEmpty(_templatePath) && File.Exists(_templatePath)
                    ? File.ReadAllText(_templatePath)
                    : null;

                var metadata = yamlTemplate != null
                    ? new Deserializer().Deserialize<Dictionary<string, object>>(yamlTemplate)
                    : GetDefaultMetadata();

                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .Build();
                var htmlContent = Markdown.ToHtml(markdown, pipeline);

                var htmlTemplate = LoadHtmlTemplate();

                var template = Handlebars.Compile(htmlTemplate);
                var templateData = new
                {
                    Content = htmlContent,
                    Theme = ThemePicker.SelectedItem?.ToString().ToLower() ?? "light",
                    Author = metadata.ContainsKey("author") ? metadata["author"].ToString() : "Unknown",
                    Version = metadata.ContainsKey("version") ? metadata["version"].ToString() : "1.0.0"
                };
                var html = template(templateData);

                string outputPath = Path.Combine(
                    !string.IsNullOrEmpty(_savePath) ? _savePath : Directory.GetCurrentDirectory(),
                    FormatPicker.SelectedItem?.ToString() == "PDF" ? "output.pdf" : "output.html");
                
                if (FormatPicker.SelectedItem?.ToString() == "PDF")
                {
                    var converter = new HtmlToPdf();
                    var doc = converter.ConvertHtmlString(html);
                    doc.Save(outputPath);
                }
                else
                {
                    File.WriteAllText(outputPath, html);
                }

                DisplayAlert("Успех", $"Файл сохранён в той же папке, что и выбранный файл: {outputPath}", "OK");
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }
        /// <summary>
        /// Загрузка стандартного шаблона Markdown
        /// </summary>
        /// <returns></returns>
        private string GetDefaultMarkdown()
        {
            return @"# Пример документации
    Это пример документа, сгенерированного автоматически.";
        }
        private Dictionary<string, object> GetDefaultMetadata()
        {
            return new Dictionary<string, object>
            {
                { "author", "Unknown Author" },
                { "version", "1.0.0" }
            };
        }
        /// <summary>
        /// Обработчик кнопки очистки текста
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearMarkdownEditor_Clicked(object sender, EventArgs e)
        {
            MarkdownEditor.Text = string.Empty;
        }
        /// <summary>
        /// Обработчик кнопки выбора места сохранения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SelectSaveLocation_Clicked(object sender, EventArgs e)
        {
            var folder = await FilePicker.PickAsync();
            if (folder != null)
            {
                _savePath = folder.FullPath;
                DisplayAlert("Успех", $"Место сохранения: {_savePath}", "OK");
            }
        }

    }
}