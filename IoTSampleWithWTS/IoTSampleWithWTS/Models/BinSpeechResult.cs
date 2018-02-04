﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTSampleWithWTS.Models
{
    public class BinSpeechResult
    {
        public string RecognitionStatus { get; set; }
        public string DisplayText { get; set; }
        public int Offset { get; set; }
        public int Duration { get; set; }
    }

}
