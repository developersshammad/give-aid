namespace GiveAid.Helpers
{
    /// <summary>
    /// Saves uploaded images (partner logos, programme pictures, gallery photos)
    /// into wwwroot/uploads/{folder}/ with a unique file name.
    /// </summary>
    public class FileStorage
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        public const long MaxBytes = 5 * 1024 * 1024;   // 5 MB

        private readonly IWebHostEnvironment _env;
        public FileStorage(IWebHostEnvironment env) => _env = env;

        /// <summary>Returns an error message, or null if the file is acceptable.</summary>
        public static string? Check(IFormFile? file, bool required)
        {
            if (file == null || file.Length == 0)
                return required ? "Please choose an image file." : null;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return "Only JPG, PNG, GIF or WEBP images are allowed.";
            if (file.Length > MaxBytes)
                return "Image must be smaller than 5 MB.";
            if (!LooksLikeImage(file))
                return "This file is not a real image. Choose a JPG, PNG, GIF or WEBP picture.";
            return null;
        }

        /// <summary>
        /// Checks the first bytes of the file ("magic number"), so a renamed file
        /// (for example a .txt or .exe saved as .jpg) is refused.
        /// </summary>
        private static bool LooksLikeImage(IFormFile file)
        {
            var head = new byte[12];
            using (var s = file.OpenReadStream())
            {
                int read = 0, n;
                while (read < head.Length && (n = s.Read(head, read, head.Length - read)) > 0) read += n;
                if (read < 4) return false;
            }
            bool jpg = head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF;
            bool png = head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47;
            bool gif = head[0] == 0x47 && head[1] == 0x49 && head[2] == 0x46;                       // "GIF"
            bool webp = head[0] == 0x52 && head[1] == 0x49 && head[2] == 0x46 && head[3] == 0x46 &&   // "RIFF....WEBP"
                        head[8] == 0x57 && head[9] == 0x45 && head[10] == 0x42 && head[11] == 0x50;
            return jpg || png || gif || webp;
        }

        /// <summary>Stores the file and returns its web path, e.g. /uploads/gallery/abc.jpg</summary>
        public async Task<string> SaveAsync(IFormFile file, string folder)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var dir = Path.Combine(_env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(dir);

            var name = $"{Guid.NewGuid():N}{ext}";
            await using (var stream = new FileStream(Path.Combine(dir, name), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return $"/uploads/{folder}/{name}";
        }

        /// <summary>Deletes a previously uploaded file (sample images in /images are kept).</summary>
        public void Delete(string? webPath)
        {
            if (string.IsNullOrEmpty(webPath) || !webPath.StartsWith("/uploads/")) return;
            var full = Path.Combine(_env.WebRootPath, webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(full)) File.Delete(full);
        }
    }
}
