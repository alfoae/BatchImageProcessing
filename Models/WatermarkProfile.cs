using System.Collections.Generic;

namespace ImageProcessing.Models
{
    public class WatermarkLayerSnapshot
    {
        public string ImagePath    { get; set; } = string.Empty;
        public double X            { get; set; }
        public double Y            { get; set; }
        public double Width        { get; set; } = 150;
        public double Height       { get; set; } = 150;
        public double Opacity      { get; set; } = 1.0;
        public bool   UseAlignment { get; set; }
        public int    AlignmentIndex { get; set; }
    }

    public class WatermarkProfile
    {
        public string Name { get; set; } = string.Empty;
        public bool ApplyToAll { get; set; }
        public List<WatermarkLayerSnapshot> Layers { get; set; } = new();
    }
}
