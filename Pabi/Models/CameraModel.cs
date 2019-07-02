using Microsoft.Toolkit.Uwp.UI.Controls;

namespace Pabi.Models
{
    public class CameraModel
    {
        public uint Width { get; set; }
        public uint Height { get; set; }

        public CameraModel GetCameraSize(CameraPreview cameraPreview)
        {
            return new CameraModel
            {
                Width = (uint) cameraPreview.Width,
                Height = (uint) cameraPreview.Height
            };
        }
    }
}