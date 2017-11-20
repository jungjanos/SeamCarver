using Microsoft.Win32;
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
using System.Drawing;

namespace SeamCarving
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Controls.Image image;
        Bitmap bitmap;

        public MainWindow()
        {
            InitializeComponent();

            var mainWindow = Application.Current.MainWindow;
            mainWindow.SizeToContent = SizeToContent.WidthAndHeight;

        }

        private void updateSizeDisplay()
        {
            widthLabel.Content = "Width: " + bitmap.Width + "pixels";
            heightLabel.Content = "Height: " + bitmap.Height + "pixels";
        }

        private void clickedFileOpen(object sender, RoutedEventArgs e)
        {

            // Displays an Open File Dialog to load an image for display and edit              
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            openFileDialog.DefaultExt = ".jpg";
            openFileDialog.Filter = "Image documents (*.bmp;*.png;*.jpeg;*.jpg)|*.bmp;*.png;*.jpeg;*.jpg|All files (*.*)|*.*";
            openFileDialog.ShowDialog();


            // Loads the image and displays it             
            image = new System.Windows.Controls.Image();
            image.Source = new BitmapImage(new Uri(openFileDialog.FileName, UriKind.RelativeOrAbsolute));
            image.Stretch = Stretch.Uniform;            
            ImageControl.Source = image.Source;


            // loads same file as bitmap (BMP) to memory to work on
            bitmap = new Bitmap(openFileDialog.FileName);
            
            updateSizeDisplay();
            


            
        }
    }
}
