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

namespace PasswordGenWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void cbUppercase_Checked(object sender, RoutedEventArgs e)
        {
            if (tbUppercaseWeight == null) return;

            tbUppercaseWeight.IsEnabled = cbUppercase.IsChecked ?? false;
        }

        private void cbLowercase_Checked(object sender, RoutedEventArgs e)
        {
            if (tbLowercaseWeight == null) return;

            tbLowercaseWeight.IsEnabled = cbLowercase.IsChecked ?? false;
        }

        private void cbNumbers_Checked(object sender, RoutedEventArgs e)
        {
            if (tbNumbersWeight == null) return;

            tbNumbersWeight.IsEnabled = cbNumbers.IsChecked ?? false;
        }

        private void cbSymbolPunct_Checked(object sender, RoutedEventArgs e)
        {
            if (tbSymbolPunctWeight == null) return;

            tbSymbolPunctWeight.IsEnabled = cbSymbolPunct.IsChecked ?? false;
        }

        private void cbAlsoInclude_Checked(object sender, RoutedEventArgs e)
        {
            if (tbAlsoInclude == null || tbAlsoIncludeWeight == null) return;

            tbAlsoInclude.IsEnabled = cbAlsoInclude.IsChecked ?? false;
            tbAlsoIncludeWeight.IsEnabled = cbAlsoInclude.IsChecked ?? false;
        }

        private void cbDoNotInclude_Checked(object sender, RoutedEventArgs e)
        {
            if (tbDoNotInclude == null) return;

            tbDoNotInclude.IsEnabled = cbDoNotInclude.IsChecked ?? false;
        }

        private void bGenerate_Click(object sender, RoutedEventArgs e)
        {
            GenerateStrings();
            mpWindow.CurrentPage = "Page2";
        }

        private void bBack_Click(object sender, RoutedEventArgs e)
        {
            mpWindow.CurrentPage = "Main";
        }

        private void bMore_Click(object sender, RoutedEventArgs e)
        {
            GenerateStrings();
        }

        private void DoValidation()
        {
            if (!IsInitialized) return;

            UIElement[] u = new UIElement[]
            {
                cbUppercase, tbUppercaseWeight, lblUppercaseWeightValid,
                cbLowercase, tbLowercaseWeight, lblLowercaseWeightValid,
                cbNumbers, tbNumbersWeight, lblNumbersWeightValid,
                cbSymbolPunct, tbSymbolPunctWeight, lblSymbolPunctWeightValid,
                cbAlsoInclude, tbAlsoIncludeWeight, lblAlsoIncludeWeightValid,
                null, tbPasswordCount, lblPasswordCountValid,
                null, tbPasswordLength, lblPasswordLengthValid
            };

            bool anyBad = false;

            for (int i = 0; i < u.Length; i += 3)
            {
                UIElement cb = u[i];
                UIElement tb = u[i + 1];
                UIElement lbl = u[i + 2];

                if (cb == null || (((CheckBox)cb).IsChecked ?? false))
                {
                    int val;
                    if (int.TryParse(((TextBox)tb).Text, out val))
                    {
                        if (val < 1)
                        {
                            lbl.Visibility = Visibility.Visible;
                            anyBad = true;
                        }
                        else
                        {
                            lbl.Visibility = Visibility.Hidden;
                        }
                    }
                    else
                    {
                        lbl.Visibility = Visibility.Visible;
                        anyBad = true;
                    }
                }
            }

            bGenerate.IsEnabled = !anyBad;
        }

        private void tbIntegerEdit_TextChanged(object sender, TextChangedEventArgs e)
        {
            DoValidation();
        }

        private void GenerateStrings()
        {
            try
            {
                Func<CheckBox, TextBox, int> doParse = delegate (CheckBox cb, TextBox tb)
                {
                    return (cb.IsChecked ?? false) ? int.Parse(tb.Text) : 0;
                };

                int uppercaseWeight = doParse(cbUppercase, tbUppercaseWeight);
                int lowercaseWeight = doParse(cbLowercase, tbLowercaseWeight);
                int numberWeight = doParse(cbNumbers, tbNumbersWeight);
                int symbolPunctWeight = doParse(cbSymbolPunct, tbSymbolPunctWeight);
                int alsoIncludeWeight = doParse(cbAlsoInclude, tbAlsoIncludeWeight);
                string alsoInclude = (cbAlsoInclude.IsChecked ?? false) ? (tbAlsoInclude.Text ?? "") : "";
                string exclude = (cbDoNotInclude.IsChecked ?? false) ? (tbDoNotInclude.Text ?? "") : "";

                int passwordLength = int.Parse(tbPasswordLength.Text);
                int passwordCount = int.Parse(tbPasswordCount.Text);

                AlphabetSpec spec = new AlphabetSpec
                (
                    uppercaseWeight,
                    lowercaseWeight,
                    numberWeight,
                    symbolPunctWeight,
                    alsoInclude,
                    alsoIncludeWeight,
                    exclude
                );

                Alphabet a = new Alphabet(spec);

                using (RandomGen r = new RandomGen())
                {
                    List<string> lst = Enumerable.Range(0, passwordCount).Select(i => Utility.GeneratePassword(a, r, passwordLength)).ToList();

                    tbPasswords.Text = string.Join(Environment.NewLine, lst);
                }

                lblAverageBitsPerPassword.Content = $"Average Bits per Password: {a.AverageBitsPerCharacter * passwordLength}";
            }
            catch(FormatException)
            {
                // ignore
            }
        }
    }
}
