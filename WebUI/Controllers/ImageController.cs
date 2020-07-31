using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebUI.Models;

namespace WebUI.Controllers
{
    public class ImageController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<HomeController> _logger;

        public ImageController(IWebHostEnvironment env, ILogger<HomeController> logger)
        {
            _env = env;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View(new ImageViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile uploadimage)
        {
            if (uploadimage.Length > 0)
            {
                var rnd = Guid.NewGuid().ToString("N");
                var extension = Path.GetExtension(uploadimage.FileName);

                var tempfileName = $"{Path.GetFileNameWithoutExtension(uploadimage.FileName)}_{rnd}{extension}";
                var tempFilePath = Path.Combine(_env.ContentRootPath, "wwwroot", "Uploads");
                var fullPath = Path.Join(tempFilePath, tempfileName);


                using (var fs = new FileStream(fullPath, FileMode.OpenOrCreate))
                {
                    await uploadimage.CopyToAsync(fs);
                    return View("Index", new ImageViewModel(tempfileName, origFileName: uploadimage.FileName));
                }
            }

            return View("Index", new ImageViewModel());
        }


        [HttpPost]
        public async Task<IActionResult> CarveImage(string contentpath, string origfilename, int columnsToCarve)
        {
            SeamCarver.SeamCarver.CarveVertically(Path.Combine(_env.ContentRootPath, "wwwroot", "Uploads", contentpath), columnsToCarve, Path.Combine(_env.ContentRootPath, "wwwroot", "Uploads", origfilename), SeamCarver.ImageFormat.jpeg, CancellationToken.None);

            return View("Index", new ImageViewModel());
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
