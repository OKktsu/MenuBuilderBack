using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MenuBuilderBack.Models.Settings;
using MenuBuilderBack.Service.Interface;
using Microsoft.Extensions.Options;

namespace MenuBuilderBack.Service
{
    public class PhotoService : IPhotoService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<PhotoService> _logger;

        private static readonly string[] AllowedMimeTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
        private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

        public PhotoService(IOptions<CloudinarySettings> config, ILogger<PhotoService> logger)
        {
            var acc = new Account(config.Value.CloudName, config.Value.ApiKey, config.Value.ApiSecret);
            _cloudinary = new Cloudinary(acc);
            _logger = logger;
        }

        public async Task<ImageUploadResult> AddPhotoAsync(string base64Image)
        {
            if (string.IsNullOrEmpty(base64Image))
                return new ImageUploadResult { Error = new Error { Message = "Imagem não fornecida." } };

            var mimeType = ExtractMimeType(base64Image);
            if (!AllowedMimeTypes.Contains(mimeType))
                return new ImageUploadResult { Error = new Error { Message = $"Tipo de imagem não permitido: {mimeType}. Use JPEG, PNG, WebP ou GIF." } };

            var base64Data = base64Image.Contains(",") ? base64Image.Split(',')[1] : base64Image;

            byte[] imageBytes;
            try
            {
                imageBytes = Convert.FromBase64String(base64Data);
            }
            catch (FormatException)
            {
                return new ImageUploadResult { Error = new Error { Message = "Dados de imagem inválidos." } };
            }

            if (imageBytes.Length > MaxFileSizeBytes)
                return new ImageUploadResult { Error = new Error { Message = "Imagem excede o tamanho máximo de 5MB." } };

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(Guid.NewGuid().ToString(), new MemoryStream(imageBytes)),
                Folder = "menu-items"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
                _logger.LogError("Erro ao fazer upload no Cloudinary: {Error}", result.Error.Message);

            return result;
        }

        public async Task<DeletionResult> DeletePhotoAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            return await _cloudinary.DestroyAsync(deleteParams);
        }

        private static string ExtractMimeType(string base64Image)
        {
            if (!base64Image.StartsWith("data:")) return string.Empty;
            var semicolonIndex = base64Image.IndexOf(';');
            return semicolonIndex < 0 ? string.Empty : base64Image[5..semicolonIndex];
        }
    }
}
