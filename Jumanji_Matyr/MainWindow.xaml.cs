using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Jumanji_Matyr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            int c = 16;
            ColumnDefinition[] coldef = new ColumnDefinition[c];
            for (int iCol = 0; iCol < coldef.Length; iCol++)
            {
                coldef[iCol] = new ColumnDefinition();
                martyr.ColumnDefinitions.Add(coldef[iCol]);
            }
            RowDefinition[] rowdef = new RowDefinition[c];
            for (int iRow = 0; iRow < rowdef.Length; iRow++)
            {
                rowdef[iRow] = new RowDefinition();
                martyr.RowDefinitions.Add(rowdef[iRow]);
            }

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri("assets/foret.png", UriKind.Relative);
            image.EndInit();

            Image imBouton = new Image();
            imBouton.Source = image;
            imBouton.Stretch = System.Windows.Media.Stretch.None;
            martyr.Children.Add(imBouton);
            Grid.SetColumnSpan(imBouton, c);
            Grid.SetRowSpan(imBouton, c);

            Button[,] ButtonsMartyr = new Button[c, c];
            TextBlock[,] MonTexte = new TextBlock[c, c];
            for (int iTxt = 0; iTxt < rowdef.Length; iTxt++)
            {
                for (int jTxt = 0; jTxt < rowdef.Length; jTxt++)
                {
                    ButtonsMartyr[iTxt, jTxt] = new Button();
                    Grid.SetRow(ButtonsMartyr[iTxt, jTxt], iTxt);
                    Grid.SetColumn(ButtonsMartyr[iTxt, jTxt], jTxt);
                    //ButtonsMartyr[iTxt, jTxt].Content = "Jumanji";
                    // MonTexte[iTxt, jTxt].HorizontalAlignment = HorizontalAlignment.Center;
                    // MonTexte[iTxt, jTxt].VerticalAlignment = VerticalAlignment.Center;
                    // MonTexte[iTxt, jTxt].FontWeight = FontWeights.UltraBold;
                    // MonTexte[iTxt, jTxt].FontSize = 40;
                    // MonTexte[iTxt, jTxt].Tag = (iTxt, jTxt);
                    // MonTexte[iTxt, jTxt].MouseLeftButtonDown += TextBlock_Click;
                    martyr.Children.Add(ButtonsMartyr[iTxt, jTxt]);
                }
            }
            TheFloorIsLava(ButtonsMartyr, c);
        }
        static void TheFloorIsLava(Button[,] ButtonsMartyr, int c)
        {
            for (int iRow = 0; iRow < c; iRow++)
            {
                for (int iColumn = 0; iColumn < c; iColumn++)
                {
                    ButtonsMartyr[iRow, iColumn].Background = Brushes.Transparent;
                }
            }
            Random alea = new Random();
            for (int iRow = 0; iRow < c; iRow++)
            {
                for (int iColumn = 0; iColumn < c; iColumn++)
                {
                    if ((alea.Next(0, 4) == 0) && (((iRow > 9) || (iRow < 6)) || (iColumn > 9) || (iColumn < 6)))
                    {
                        ButtonsMartyr[iRow, iColumn].Background = Brushes.Red;
                    }
                }
            }
        }
    }
}