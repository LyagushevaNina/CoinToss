#pragma warning disable CA1031
#pragma warning disable CA1303
#pragma warning disable CA2213
#pragma warning disable CS8618
#pragma warning disable IDE0058

using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Alice;

internal partial class AliceForm : Form
{
    private TextBox textBoxMessages;
    private TextBox textBoxInput;
    private Button buttonSend;
    private TcpClient client;
    private NetworkStream stream;
    private byte aliceBit;
    private byte buddyBit;
    private byte buddyRnd;
    private string servMess;

    public AliceForm()
    {
        InitializeComponent("Alice");
        ConnectToBob();
    }

    private async void ConnectToBob()
    {
        try
        {
            client = new TcpClient("127.0.0.1", 5050);
            stream = client.GetStream();
            AppendMessage("Connection established");

            // Шаг 1: Ожидаем сообщение от Боба (хэш-значение)
            servMess = await ReceiveMessageAsync();
            if (string.IsNullOrEmpty(servMess) || !servMess.Contains("/"))
            {
                AppendMessage("Ошибка: Неверный формат сообщения от Боба (хэш).");
                return;
            }
            // Получили хеш от Боба
            string buddyHash = servMess[1..servMess.IndexOf("/")];

            // Шаг 2: Алиса подбрасывает монету
            Random random = new Random();
            aliceBit = (byte)random.Next(2);
            AppendMessage("Результат броска Алисы: " + aliceBit);

            // Шаг 3: Отправка результата Бобу
            await SendMessageAsync("!" + aliceBit.ToString() + "/");

            // Шаг 4: Получение результата и случайного числа от Боба
            servMess = await ReceiveMessageAsync();
            if (string.IsNullOrEmpty(servMess) || !servMess.Contains("/") || servMess.Split('/').Length < 3)
            {
                AppendMessage("Ошибка: Неверный формат сообщения от Боба (данные).");
                return;
            }

            string[] parts = servMess.Split('/');
            if (!byte.TryParse(parts[1], out buddyBit) || !byte.TryParse(parts[2], out buddyRnd))
            {
                AppendMessage("");
                return;
            }

            // Шаг 5: Вычисление хэша
            byte[] kHash = { buddyBit, buddyRnd };
            byte[] md5Hash = MD5.HashData(kHash);
            string calculatedHash = md5Hash[0].ToString() + md5Hash[1].ToString();

            // Шаг 6: Сравнение хэшей
            if (buddyHash == calculatedHash)
            {
                int result = (aliceBit + buddyBit) % 2;
                AppendMessage("Общий результат броска: " + result);
            }
            else
            {
                AppendMessage("Ошибка: Хэши не совпадают. Возможна подмена данных.");
            }
        }
        catch (Exception ex)
        {
            AppendMessage("Ошибка: " + ex.Message);
        }
    }

    private void AppendMessage(string message)
    {
        textBoxMessages.AppendText(message + Environment.NewLine);
    }

    private async Task SendMessageAsync(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        await stream.WriteAsync(data, 0, data.Length);
    }

    private async Task<string> ReceiveMessageAsync()
    {
        byte[] buffer = new byte[256];
        int bytesRead = await stream.ReadAsync(buffer).ConfigureAwait(true);
        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }

    private void InitializeComponent(string name)
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.Sizable;
        Text = name;

        TableLayoutPanel mainLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };

        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 80)); // Окно с логами - 80% высоты
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20)); // Панель ввода текста и кнопки - 20% высоты

        textBoxMessages = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            TabIndex = 1,
            ScrollBars = ScrollBars.Vertical
        };

        TableLayoutPanel inputPanel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };

        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80)); // Поле ввода текста - 80% ширины
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20)); // Кнопка - 20% ширины

        textBoxInput = new TextBox
        {
            Dock = DockStyle.Fill,
            TabIndex = 0
        };

        buttonSend = new Button
        {
            Dock = DockStyle.Fill,
            TabIndex = 2,
            Text = "Send",
            UseVisualStyleBackColor = true
        };

        inputPanel.Controls.Add(textBoxInput, 0, 0);
        inputPanel.Controls.Add(buttonSend, 1, 0);

        mainLayout.Controls.Add(textBoxMessages, 0, 0);
        mainLayout.Controls.Add(inputPanel, 0, 1);

        Controls.Add(mainLayout);

        ClientSize = new Size(700, 500);
        MinimumSize = new Size(300, 200);

        FormClosing += OnFormClosing;
        buttonSend.Click += ButtonSend_Click;
        textBoxInput.KeyDown += TextBoxInput_KeyDown;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        Environment.Exit(0);
    }

    private void ButtonSend_Click(object? sender, EventArgs e)
    {
        string message = textBoxInput.Text;
        SendMessageAsync(message);
        AppendMessage($"Alice: {message}");
        textBoxInput.Clear();
    }

    private void TextBoxInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            buttonSend.PerformClick();
        }
    }
}

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using AliceForm form = new();
        Application.Run(form);
    }
}
