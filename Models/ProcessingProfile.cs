using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing.Models
{
    public class ProcessingProfile
    {
        public string Name { get; set; }
        public int CropX { get; set; }
        public int CropY { get; set; }
        public int CropWidth { get; set; }
        public int CropHeight { get; set; }
        public double WatermarkX { get; set; }
        public double WatermarkY { get; set; }
        public double WatermarkWidth { get; set; }
        public double WatermarkHeight { get; set; }
        public double WatermarkOpacity { get; set; }
        public bool ApplyToAll { get; set; }
    }
}