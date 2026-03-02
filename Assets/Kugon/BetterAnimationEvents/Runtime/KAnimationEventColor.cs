using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    public class KAnimationEventColor
    {
        public List<KColor> UserGeneratedColor;

        public KAnimationEventColor()
        {
            UserGeneratedColor = new List<KColor>();
        }
        
        public Color OriginalColor { get; set; }
        
        public Color SelectionColor =>new Color32(41, 121, 209, 255);
        public Color Red => Color.red;
        public Color Clear => Color.clear;
        
        public Color White => Color.white;
        public Color Gray => Color.gray;
        public Color ClearGray => new Color(0.5f, 0.5f, 0.5f, 0.5f);
        
        public Color Even => new Color32(45, 45, 45, 255);  // Darker
        public Color Odd => new Color32(60, 60, 60, 255);  // Lighter

        public Color LightTimeline => new Color(0.15f, 0.25f, 0.29f);
        public Color DarkTimeline => new Color(0.16f, 0.18f, 0.19f);
    }
}