using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImageContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ImageServer.Database;
namespace ImageServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private IImagesDb db;

        public ImagesController(IImagesDb db)
        {
            this.db = db;
        }

        [HttpGet]
        public async Task<List<int>> GetImages()
        {
            var result = await db.GetAllImages();
            return result;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Image>> GetImages(int id)
        {
            var result = await db.TryGetImageById(id);
            if (result != null)
                return result;
            return StatusCode(404, "Изображение с данным id не найдено");
        }

        [HttpPost]
        public async Task<int> AddImages([FromBody] DataStruct obj, CancellationToken token)
        {
            var image = obj.Image;
            var path = obj.Path;
            return await db.PostImage(image, path, token);
        }

        [HttpDelete("{id}")]
        public async Task<int> DeleteImages(int id)
        {
            var result = await db.TryDeleteImage(id);
            return result;
        }
    }

}
