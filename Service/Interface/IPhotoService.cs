using System.Threading.Tasks;
using CloudinaryDotNet.Actions;

namespace MenuBuilderBack.Service.Interface
{
    public interface IPhotoService
    {
        Task<ImageUploadResult> AddPhotoAsync(string base64Image);
        Task<DeletionResult> DeletePhotoAsync(string publicId);
    }
}
