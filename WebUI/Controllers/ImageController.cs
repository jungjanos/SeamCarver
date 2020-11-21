using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebUI.Models;
using WebUI.Service;
using Common;
using Microsoft.AspNetCore.Authorization;
using Data;
using Microsoft.Identity.Web;
using System.ComponentModel.DataAnnotations;

namespace WebUI.Controllers
{
    public class ImageController : Controller
    {

        private readonly UserFileSystemHelper _fsHelper;
        private readonly ActionHistoryPersister _historyPersister;
        private readonly ILogger<ImageController> _logger;
        private readonly IImageDetailService _imageDetailService;

        public ImageController(UserFileSystemHelper fsHelper, ActionHistoryPersister historyPersister, IImageDetailService imageDetailService, ILogger<ImageController> logger)
        {
            _fsHelper = fsHelper;
            _historyPersister = historyPersister;
            _imageDetailService = imageDetailService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View(ImageViewModel.Empty);
        }

        [HttpPost]
        [Authorize(Policy = "HasAccount")]
        public async Task<IActionResult> UploadImage(IFormFile uploadimage)
        {
            if (uploadimage?.Length > 0 && uploadimage.ContentType.Contains("image"))
            {
                var localFileName = await _fsHelper.SaveUploadFileToRandomFile(uploadimage.FileName, uploadimage.OpenReadStream());
                var details = await _imageDetailService.GetDetailsAsync(_fsHelper.PrependPhysicalFolderPath(localFileName));

                await _historyPersister.CreateHistoryEntry(User.GetObjectId(), ActionType.ImageUpload, null, uploadimage.FileName, uploadimage.Length, $"{details.Width} x {details.Height}");

                var image = new ImageViewModel(_fsHelper.UserVirtualFolder, localFileName, details.Width, details.Height, details.Size, origFileName: uploadimage.FileName);

                return View("Index", image);
            }
            else
            {
                // TODO : some error display
                return RedirectToAction(nameof(Index), ImageViewModel.Empty);
            }
        }


        [HttpPost]
        [Authorize(Policy = "HasAccount")]
        public async Task<IActionResult> CarveImage(string filename, string origfilename, [Range(1, int.MaxValue)] int columnsToCarve)
        {
            if (ModelState.IsValid)
            {
                var physicalPath = _fsHelper.PrependPhysicalFolderPath(filename);
                var targetFilename = _fsHelper.CreateRandomFilename(origfilename);
                SeamCarver.SeamCarverWrapper.CarveVertically(physicalPath, columnsToCarve, _fsHelper.PrependPhysicalFolderPath(targetFilename), ImageFormat.jpeg, CancellationToken.None);

                var details = await _imageDetailService.GetDetailsAsync(_fsHelper.PrependPhysicalFolderPath(targetFilename));

                await _historyPersister.CreateHistoryEntry(User.GetObjectId(), ActionType.ImageCarving, null, origfilename, filename, columnsToCarve, details.Size, $"{details.Width} x {details.Height}");

                return View("Index", new ImageViewModel(_fsHelper.UserVirtualFolder, targetFilename, details.Width, details.Height, details.Size, origfilename));
            }
            else
                return View("Index", new ImageViewModel(_fsHelper.UserVirtualFolder, filename, null, null, null, origfilename));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
