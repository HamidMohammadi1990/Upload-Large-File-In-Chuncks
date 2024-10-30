using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace CheckUploadFiles.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileManagerController : ControllerBase
    {
        static int chunkSize = 1024 * 1024;
        private readonly string _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");        

        [HttpPost("upload-chunk")]
        public async Task<IActionResult> UploadChunk([FromForm] int chunkNumber, [FromForm] int totalChunks, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file chunk received.");

            var filePath = Path.Combine(_uploadPath, "uploaded_file.rar");
            if (System.IO.File.Exists(filePath))
            {
                var uploadedFileLength = new FileInfo(filePath).Length;
                var expectedFileSize = chunkNumber * chunkSize + file.Length;

                if (uploadedFileLength >= expectedFileSize)
                {
                    return Ok("Chunk already uploaded.");
                }
            }

            using (var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write))
                await file.CopyToAsync(stream);

            // اگر فایل را از همان اول در پوشه اصلی و با نام اصلیش ذخیره کرده باشیم دیگه نیازی به این خط نیست
            // چون ابتدا در یک پوشه موقتی دخیره نمی کنیم که بعدش بخواهیم منتقلش کنیم به پوشه اصلی 
            //if (chunkNumber + 1 == totalChunks)
            //    System.IO.File.Move(filePath, Path.Combine(_uploadPath, "final_file_name.rar"));

            return Ok();
        }

        [HttpPost("complete-upload")]
        public IActionResult CompleteUpload(CompleteUploadRequest request)
        {
            var filePath = Path.Combine(_uploadPath, request.FileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found");
            }

            // محاسبه checksum
            var checksum = CalculateChecksum(filePath);

            return Ok(new { checksum });
        }

        public static string CalculateChecksum(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = System.IO.File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public class CompleteUploadRequest
        {
            public string FileName { get; set; }
        }
    }
}