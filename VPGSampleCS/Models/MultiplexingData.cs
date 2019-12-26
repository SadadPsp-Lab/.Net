using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace VPGSampleCS.Models
{
    public class MultiplexingData
    {
        public MultiplexingData()
        {
            MultiplexingRows = new List<MultiplexingDataItem>();
        }

        [Display(Name = @"نوع تسهیم")]
        public MultiplexingType? Type { get; set; }
        public List<MultiplexingDataItem> MultiplexingRows { get; set; }
        public bool IsValid()
        {
            if (!this.Type.HasValue) return false;
            if (!this.MultiplexingRows.Any()) return false;
            if (this.MultiplexingRows.Any(t => t.Value < 0)) return false;

            switch (this.Type.Value)
            {
                case MultiplexingType.Percentage:
                    if (this.MultiplexingRows.Sum(t => t.Value) > 100)
                        return false;
                    if (this.MultiplexingRows.Any(t => t.Value > 99))
                        return false;
                    break;
                case MultiplexingType.Amount:
                    break;
                default:
                    break;
            }

            return true;
        }


        public enum MultiplexingType
        {
            Percentage,
            Amount
        }

        public class MultiplexingDataItem
        {
            public int IbanNumber { get; set; }
            public long Value { get; set; }
        }
    }
}