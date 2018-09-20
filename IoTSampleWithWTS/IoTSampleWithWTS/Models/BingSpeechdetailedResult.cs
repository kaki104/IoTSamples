namespace IoTSampleWithWTS.Models
{
    public class BingSpeechdetailedResult
    {
        public string RecognitionStatus { get; set; }
        public int Offset { get; set; }
        public int Duration { get; set; }
        public Nbest[] NBest { get; set; }
    }
}
