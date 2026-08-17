namespace RingVideos.Writers
{
   public class LineWriter
   {
      public int LinePosition { get; set; }
      public string InitialMessage { get; set; } = "";
      public int InitialStatusLength { get; set; } = 0;
      public LineWriter(int linePosition)
      {
         LinePosition = linePosition;
      }

   }
}
