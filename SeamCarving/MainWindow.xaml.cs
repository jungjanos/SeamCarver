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
using System.ComponentModel;
using System.IO;


namespace SeamCarving
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {        
        BusinessLogic businessLogic;
        public bool ImageLoaded { set; get; } = false;
        public List<ResultInfoItem> ResultsToDisplay;
        private string onlyFileName;
        private string folderName;
        private Bitmap imageToDisplay;
        

        //determines the quality of BMP -> JPEG compression when saving an image
        //least compression == 100L
        //max compression == 0L
        //currently hard wired, should be implemented as UI control
        private long jpegQualityLevel = 100L;

        private BackgroundWorker backgroundWorker1;


        public MainWindow()
        {
            InitializeComponent();            

            var mainWindow = Application.Current.MainWindow;
            mainWindow.SizeToContent = SizeToContent.WidthAndHeight;
            ResultsToDisplay = new List<ResultInfoItem>();
            businessLogic = new BusinessLogic(ResultsToDisplay);            

            onlyFileName = String.Empty;
            folderName  = String.Empty;


            ResultDataGrid.ItemsSource = ResultsToDisplay;

            ResultsToDisplay.Add(new ResultInfoItem { Message = "Number of logical processor detected: " + Environment.ProcessorCount });

            setupBackgroundWorker();
        }

        private void setupBackgroundWorker()
        {
            backgroundWorker1 = new BackgroundWorker();
            backgroundWorker1.DoWork +=
                new DoWorkEventHandler(backgroundSetupSeamCarver);

            backgroundWorker1.RunWorkerCompleted +=
                new RunWorkerCompletedEventHandler(backgroundSetupSeamCarverCompleted);

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
            if (openFileDialog.ShowDialog() == true)
            {
                string fileName = openFileDialog.FileName;

                onlyFileName = System.IO.Path.GetFileName(fileName);                
                StringBuilder sb = new StringBuilder(fileName);
                folderName = sb.Remove(fileName.Length - onlyFileName.Length, onlyFileName.Length).ToString();


                // Loads the image and displays it                                                 

                //Bitmap is loaded from file and packaged in WPF friendly format
                imageToDisplay = new Bitmap(fileName);
                ImageControl.Width = imageToDisplay.Width;
                ImageControl.Height = imageToDisplay.Height;
                ImageControl.Source = Tools.BitmapToImageSource(imageToDisplay);

                
                ResultDataGrid.ItemsSource = null;

                backgroundWorker1.RunWorkerAsync(imageToDisplay);
            }         
                        
        }

        private void backgroundSetupSeamCarver(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker backgroundWorker = sender as BackgroundWorker;
            businessLogic.SetupSeamCarver((Bitmap)e.Argument);
        }

        private void backgroundSetupSeamCarverCompleted (object sender, RunWorkerCompletedEventArgs e)
        {
            ResultDataGrid.ItemsSource = ResultsToDisplay;
            if (e.Error != null)
            {
                ResultsToDisplay.Add(new ResultInfoItem { Message = "backgroundSetupSeamCarver failed" + e.Error.Message });
                ResultDataGrid.Items.Refresh();
            }
            else
            {

                CarveImageLabel.IsEnabled = true;
                CarveWrapePanel.IsEnabled = true;
                directionButton.IsEnabled = true;
                ApplyCarvingButton.IsEnabled = true;

                updateSizeDisplay();
                ResultDataGrid.Items.Refresh();
                ImageLoaded = true;
                showCarvingOptions();
            }


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

            Bitmap newImage = businessLogic.sH.RemoveNHorizontalSeams(numberOfSeams);
            ImageControl.Source = Tools.BitmapToImageSource( newImage);
            imageToDisplay = newImage;
            updateSizeDisplay();
            ResultDataGrid.Items.Refresh();

        }

        private void uncheckedShowMessages(object sender, RoutedEventArgs e)
        {
            //ResultDataGrid.Visibility = Visibility.Hidden;
        }

        private void checkedShowMessages(object sender, RoutedEventArgs e)
        {
            //ResultDataGrid.Visibility = Visibility.Visible;
        }

        private void clickedShowMessages(object sender, RoutedEventArgs e)
        {
            if (ResultDataGrid.Visibility == Visibility.Visible) ResultDataGrid.Visibility = Visibility.Collapsed;
            else ResultDataGrid.Visibility = Visibility.Visible;
        }

        private void clickExitMenu(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void clickedFileSave(object sender, RoutedEventArgs e)
        {
            
            if (folderName != String.Empty && onlyFileName != String.Empty && businessLogic.sH != null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.InitialDirectory = folderName;
                saveFileDialog.DefaultExt = ".jpeg";
                System.Drawing.Imaging.ImageFormat imageFormat = System.Drawing.Imaging.ImageFormat.Jpeg;
                saveFileDialog.AddExtension = true;
                saveFileDialog.Filter = "JPEG image (*.jpeg)|*.jpeg;|Bitmap image (*.bmp)|*.bmp;|PNG image (*.png)|*.png;|GIF image (*.gif)|*.gif;|All files (*.*)|*.*";
                
                if (saveFileDialog.ShowDialog() == true)
                {
                    string saveFileExtension = System.IO.Path.GetExtension(saveFileDialog.FileName);                    
                    
                    switch (saveFileExtension)
                    {
                        case ".bmp": imageFormat = System.Drawing.Imaging.ImageFormat.Bmp; break;
                        case ".png": imageFormat = System.Drawing.Imaging.ImageFormat.Png; break;
                        case ".gif": imageFormat = System.Drawing.Imaging.ImageFormat.Gif; break;
                    }

                    businessLogic.sH.SaveBitmap(saveFileDialog.FileName, imageFormat, jpegQualityLevel);
                }
                ResultDataGrid.Items.Refresh();
            }            
        }
    }
}
