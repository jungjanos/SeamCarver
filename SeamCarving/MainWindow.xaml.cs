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
using System.Data;

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
        
        //this is not necessariliy the image the user is currently working on
        //(think viewing past results from the temporary saved file list)
        private Bitmap currentlyDisplayedImage;
        private string applicationName;
        private List<SaveFileCatalogEntry> temporarySaveFiles;
        private string customSavePath = String.Empty;


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
            // Gets application name without folder path and extension
            // will serve as tempfolder subdirectory to store temp files
            applicationName = System.IO.Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            temporarySaveFiles = new List<SaveFileCatalogEntry>();

            onlyFileName = String.Empty;
            folderName  = String.Empty;

            ResultDataGrid.ItemsSource = ResultsToDisplay;
            savedImagesDG.ItemsSource = temporarySaveFiles;            


            ResultsToDisplay.Add(new ResultInfoItem { Message = "Number of logical processor detected: " + Environment.ProcessorCount });
            ResultDataGrid.Items.Refresh();
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

        private void displayImage(Bitmap bitmap)
        {
            ImageControl.Source = Tools.BitmapToImageSource(bitmap);
            widthLabel.Content = "Width: " + bitmap.Width + "pixels";
            heightLabel.Content = "Height: " + bitmap.Height + "pixels";
            currentlyDisplayedImage = bitmap;
        }
        private Bitmap displayImage(string path)
        {
            Bitmap bitmap = new Bitmap(path);
            displayImage(bitmap);
            return bitmap;
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
                Bitmap imageOpened = displayImage(fileName);                
                ImageControl.Width = imageOpened.Width;
                ImageControl.Height = imageOpened.Height;

                // makes a save file from the loaded original image
                temporarySave(temporarySaveFiles, imageOpened, customSavePath, System.IO.Path.GetFileNameWithoutExtension(onlyFileName), imageOpened.Size, applicationName);

                // TODO : ugly !!!!!!
                // remark: ResultDataGrid is updated from code behind by assynchronous methods
                ResultDataGrid.ItemsSource = null;
                backgroundWorker1.RunWorkerAsync(imageOpened);
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

                // TODO : Refresh() not really needed, investigate whether to remove
                // bug: if sorting is ised on DataGrid column header then the DataGrid does not get automatically updated anymore
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
            // TODO : filter out invalid inputs
            int numberOfSeams = int.Parse(CarveImageTextBox.Text);

            // for now hard wired to carve horizontally, automatically calls the horizontal carver

            //carves image and immediatelly displays the result
            Bitmap newImage = businessLogic.sH.RemoveNHorizontalSeams(numberOfSeams);

            displayImage(newImage);


            // after every finished carving step we create a temporary save file
            temporarySave(temporarySaveFiles, newImage, customSavePath, System.IO.Path.GetFileNameWithoutExtension(onlyFileName), businessLogic.ImageOriginalSize, applicationName);        

        }

        private void temporarySave(List<SaveFileCatalogEntry> saveFileCatalogEntries, Bitmap bitmap, string customSavePath, string originalFileName, System.Drawing.Size originalImageSize, string applicationName)
        {
            SaveFileCatalogEntry entryToAdd = new SaveFileCatalogEntry(bitmap, customSavePath, originalFileName, originalImageSize, applicationName);
            saveFileCatalogEntries.Add(entryToAdd);
            ResultsToDisplay.Add(new ResultInfoItem { Message = "Temporary save file created: " + entryToAdd.FilePath });
            ResultDataGrid.Items.Refresh();
            savedImagesDG.Items.Refresh();
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

        private void onClosed(object sender, EventArgs e)
        {
            string tempDir;

            if (customSavePath == string.Empty)
            {
                tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), applicationName);

               foreach (string filepath in Directory.GetFiles(tempDir))
               {
                    File.Delete(filepath);
               }
                Directory.Delete(tempDir);
            }
            else
            {
                throw new NotImplementedException("onClosed(): deleting custom temporary save directory not yet implemented");
            }
        }
                
        private void savedImagesDG_selectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)sender;
            SaveFileCatalogEntry selectedRow = (SaveFileCatalogEntry)dataGrid.SelectedItem;
            displayImage(selectedRow.FilePath);



        }
    }
}
