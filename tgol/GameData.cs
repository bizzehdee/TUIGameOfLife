using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tgol
{
    internal class GameData
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int[] Data { get; set; }

        public override string ToString()
        {
            return $"{Name} ({Width} x {Height})";
        }
    }
}
