﻿namespace BunnyLand.DesktopGL.Models
{
    public class SharedContext
    {
        public bool IsClient { get; set; }
        public int FrameCounter { get; set; }
        public int FrameOffset { get; set; } = 2;
    }
}
