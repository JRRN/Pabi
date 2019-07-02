using Windows.AI.MachineLearning;
using Windows.Media;

namespace Pabi.Models
{
    public sealed class Yolov2Input
    {
        public ImageFeatureValue image; // shape(-1,3,416,416)
    }
}