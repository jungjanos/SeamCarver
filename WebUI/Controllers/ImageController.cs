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
using System.Linq;

namespace WebUI.Controllers
{
    public class ImageController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly FileSystemHelper _fsHelper;
        private readonly ILogger<ImageController> _logger;
        private readonly SeamCarverContext _db;

        public ImageController(IWebHostEnvironment env, FileSystemHelper fsHelper, ILogger<ImageController> logger, SeamCarverContext db)
        {
            _env = env;
            _fsHelper = fsHelper;
            _logger = logger;
            _db = db;
        }

        public IActionResult Index()
        {
            return View(ImageViewModel.Empty);
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile uploadimage)
        {
            if (uploadimage.Length > 0 && uploadimage.ContentType.Contains("image"))
            {
                var localFileName = await _fsHelper.SaveUploadFileToRandomFile(uploadimage.FileName, uploadimage.OpenReadStream());                
                return View("Index", new ImageViewModel(localFileName, origFileName: uploadimage.FileName));                
            }
            else
            {
                // TODO : some error display
                return RedirectToAction(nameof(Index), ImageViewModel.Empty);
            }
        }


        [HttpPost]
        public IActionResult CarveImage(string filename, string origfilename, int columnsToCarve)
        {
            var physicalPath = _fsHelper.PrependPhysicalFolderPath(filename);
            var targetFilename = _fsHelper.CreateRandomFilename(origfilename);            

            SeamCarver.SeamCarverWrapper.CarveVertically(physicalPath, columnsToCarve, _fsHelper.PrependPhysicalFolderPath(targetFilename), ImageFormat.jpeg, CancellationToken.None);

            return View("Index", new ImageViewModel(targetFilename, null, null, origfilename, null));
        }

        [AllowAnonymous]
        public IActionResult Test()
        {
            var first = _db.Users.FirstOrDefault();

            return new OkResult();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
