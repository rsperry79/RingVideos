using System;

namespace RingVideos.Writers
{
   public class LineWriter
   {
      /// <summary>Index of the fixed active-region slot this item currently occupies (not an absolute console row).</summary>
      public int LinePosition { get; set; }
      public string InitialMessage { get; set; } = "";
      public int InitialStatusLength { get; set; } = 0;
      public string LastMessage { get; set; }
      public MessageType? LastMessageType { get; set; }
      public string RenderedLine { get; set; } = "";

      public LineWriter(int linePosition)
      {
         LinePosition = linePosition;
      }
   }
}
