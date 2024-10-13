using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gk1
{
    public class PromptForm : Form
    {
        private TextBox inputTextBox;
        private Button okButton;
        private Button cancelButton;

        public string InputValue { get; private set; }

        public PromptForm(string prompt, float length)
        {
            // Inicjalizacja kontrolek
            this.Text = prompt;
            inputTextBox = new TextBox { Width = 300 };
            inputTextBox.Text = length.ToString();
            okButton = new Button { Text = "OK", DialogResult = DialogResult.OK };
            cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };

            okButton.Click += (sender, e) => { InputValue = inputTextBox.Text; this.Close(); };
            cancelButton.Click += (sender, e) => this.Close();

            // Ustawienia layoutu
            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Controls.Add(inputTextBox);
            panel.Controls.Add(okButton);
            panel.Controls.Add(cancelButton);

            this.Controls.Add(panel);
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
    }
}

