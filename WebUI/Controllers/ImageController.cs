using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebUI.Models;
using WebUI.Service;
using Common;
using Microsoft.AspNetCore.Authorization;
using Data;
using Microsoft.Identity.Web;

namespace WebUI.Controllers
{
    public class ImageController : Controller
    {

        private readonly UserFileSystemHelper _fsHelper;
        private readonly ActionHistoryPersister _historyPersister;
        private readonly ILogger<ImageController> _logger;

        public ImageController(UserFileSystemHelper fsHelper, ActionHistoryPersister historyPersister, ILogger<ImageController> logger)
        {
            _fsHelper = fsHelper;
            _historyPersister = historyPersister;
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
                await _historyPersister.CreateHistoryEntry(User.GetObjectId(), ActionType.ImageUpload, null, uploadimage.FileName, uploadimage.Length);

                return View("Index", new ImageViewModel(_fsHelper.UserVirtualFolder, localFileName, origFileName: uploadimage.FileName));
            }
            else
            {
                // TODO : some error display
                return RedirectToAction(nameof(Index), ImageViewModel.Empty);
            }
        }


        [HttpPost]
        [Authorize(Policy = "HasAccount")]
        public async Task<IActionResult> CarveImage(string filename, string origfilename, int columnsToCarve)
        {
            var physicalPath = _fsHelper.PrependPhysicalFolderPath(filename);
            var targetFilename = _fsHelper.CreateRandomFilename(origfilename);

            SeamCarver.SeamCarverWrapper.CarveVertically(physicalPath, columnsToCarve, _fsHelper.PrependPhysicalFolderPath(targetFilename), ImageFormat.jpeg, CancellationToken.None);
            await _historyPersister.CreateHistoryEntry(User.GetObjectId(), ActionType.ImageCarving, null, origfilename, filename, columnsToCarve);

            return View("Index", new ImageViewModel(_fsHelper.UserVirtualFolder, targetFilename, null, null, origfilename, null));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
