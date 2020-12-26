using System;
using System.Collections.Generic;
using System.Text;

namespace kinodrom_bot.Service
{
    public class NewYearStatisticModel
    {
        public int tNum { get; set; }
        public int bNum { get; set; }

        public string phone { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime Time { get; set; }
        public int Qty { get; set; }
        public string error { get; set; }
        public string description { get; set; }
    }
}
