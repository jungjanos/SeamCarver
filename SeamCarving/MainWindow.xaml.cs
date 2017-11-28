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
        BusinessLogic businessLogic;
        public bool ImageLoaded { set; get; } = false;
        public List<ResultInfoItem> ResultsToDisplay;
        
        


        public MainWindow()
        {
            InitializeComponent();
            businessLogic = new BusinessLogic();

            var mainWindow = Application.Current.MainWindow;
            mainWindow.SizeToContent = SizeToContent.WidthAndHeight;
            ResultsToDisplay = new List<ResultInfoItem>();




            ResultDataGrid.ItemsSource = ResultsToDisplay;


            //Below out-commented lines are for UI test, can be deleted
            {
                ////ResultDataGrid.col

                //ResultInfoItem message1 = new ResultInfoItem { Message = "abrakadabra 1" };
                //ResultInfoItem message2 = new ResultInfoItem { Message = "2 xxxxxxx" };
                //ResultsToDisplay.Add(message1);
                //ResultsToDisplay.Add(message2);

                //ResultInfoItem message3 = new ResultInfoItem { Message = "VV akarmi aaa 3" };
                //ResultsToDisplay.Add(message3);

                //ResultInfoItem message4 = new ResultInfoItem { Message = "VV akarmi aaa 3" };
                //ResultInfoItem message5 = new ResultInfoItem { Message = "VV akarmi aaa 3" };
                //ResultInfoItem message6 = new ResultInfoItem { Message = "VV akarmi aaa 3" };
                //ResultInfoItem message7 = new ResultInfoItem { Message = "VV akarmi aaa 3" };
                //ResultInfoItem message8 = new ResultInfoItem { Message = "VV akarmi aaa 3" };
                //ResultInfoItem message9 = new ResultInfoItem { Message = "VV akarmi aaa 3" };

                //ResultsToDisplay.Add(message4);
                //ResultsToDisplay.Add(message5);
                //ResultsToDisplay.Add(message6);
                //ResultsToDisplay.Add(message7);
                //ResultsToDisplay.Add(message8);
                //ResultsToDisplay.Add(message9);
            }

        }

        private void updateSizeDisplay()
        {
            if (businessLogic.ImageWorkingSize.IsEmpty == false)
            {
                widthLabel.Content = "Width: " + businessLogic.ImageWorkingSize.Width + "pixels";
                heightLabel.Content = "Height: " + businessLogic.ImageWorkingSize.Height + "pixels";
            }
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
            businessLogic.bitmap = new Bitmap(openFileDialog.FileName);
            businessLogic.SetupSeamCarver();
           
            //line below is only included for hard wired testing, needs to be cut out
            // businessLogic.sH.FindHorizontalSeam();
            
            updateSizeDisplay();
            ImageLoaded = true;
            showCarvingOptions();
                        
        }

        private void showCarvingOptions()
        {
            if (ImageLoaded)
            {
                CarveImageLabel.Visibility = Visibility.Visible;
                CarveWrapePanel.Visibility = Visibility.Visible;
                directionButton.Visibility = Visibility.Visible;
                ApplyCarvingButton.Visibility = Visibility.Visible;
            }
        }
        private void directionButtonClicked(object sender, RoutedEventArgs e)
        {          

            switch (directionButton.IsChecked)
            {
                case true:
                    directionButton.Content = "horizontally"; break;
                case false:
                    directionButton.Content = "vertically"; break;
            }

        }

        private void clickApplyCarvingButton(object sender, RoutedEventArgs e)
        {
            int numberOfSeams = int.Parse(CarveImageTextBox.Text);

            // for now hard wired to carve horizontally

            businessLogic.sH.RemoveNHorizontalSeams(numberOfSeams);

            
        }
    }
}
