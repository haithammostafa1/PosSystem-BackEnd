using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
namespace Pos.BuisnessLayer
{
    public interface IFileService
    {
        // ترجع مسار الصورة بعد حفظها
        Task<string> SaveFileAsync(IFormFile file, string folderName);
        // لحذف الصورة القديمة عند تعديل أو حذف المنتج
        void DeleteFile(string fileName, string folderName);
    }
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            // 1. تحديد مسار مجلد الـ wwwroot
            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", folderName);

            // 2. إنشاء المجلد إذا لم يكن موجوداً
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // 3. إنشاء اسم فريد للصورة لمنع تداخل الأسماء (مثلاً: صورة بيبسي واسمها 1.jpg)
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 4. حفظ الملف الفعلي في السيرفر
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // 5. إرجاع المسار النسبي لحفظه في الداتابيز
            return $"/uploads/{folderName}/{uniqueFileName}";
        }

        public void DeleteFile(string fileUrl, string folderName)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            // استخراج اسم الملف من الرابط
            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_env.WebRootPath, "uploads", folderName, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
