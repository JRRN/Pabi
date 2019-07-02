using Windows.AI.MachineLearning;

namespace Pabi.Models
{
    public sealed class Yolov2Output
    {
        public TensorFloat grid; // shape(-1,125,13,13)
    }
}