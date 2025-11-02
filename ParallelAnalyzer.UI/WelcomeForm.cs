using System;
using Eto.Forms;
using Eto.Drawing;

namespace ParallelAnalyzer.UI
{
    public class WelcomeForm : Form
    {
        public WelcomeForm()
        {
            Title = "Добро пожаловать";
            ClientSize = new Size(700, 550);
            Padding = 20;
            Resizable = false;

            var lblText = new Label
            {
                Text =
@"Приветствую, пользователь!

Перед тобой первый вариант аналитической лаборатории 
для параллельных методов, в которой можно экспериментировать 
с разными методами параллелизма, пробуя их на различных задачах, 
а потом визуализировать и интерпретировать результаты.

Проект предназначен для студентов, чтобы они могли 
наглядно увидеть, какие методы работают быстрее, 
а какие медленнее.

Изначально планировалось добавить возможность 
загрузки собственных библиотек с заданиями, 
но пока эта часть отложена.

Работа с текстовыми файлами сейчас дорабатывается 
и доступна в тестовом режиме.

Также нужна доработка отображения сохраненных результатов.",
                Wrap = WrapMode.Word,
                TextAlignment = TextAlignment.Left,
                Font = new Font(SystemFont.Default, 11)
            };

            var scroll = new Scrollable { Content = lblText, ExpandContentWidth = true };

            var btnOk = new Button { Text = "OK", Width = 100 };
            btnOk.Click += (s, e) =>
            {
                var main = new MainForm();
                Application.Instance.MainForm = main; 
                main.Show();
                Close(); 
            };

            Content = new StackLayout
            {
                Spacing = 20,
                Items =
                {
                    lblText,
                    new StackLayoutItem(btnOk, HorizontalAlignment.Center)
                }
            };
        }
    }
}
