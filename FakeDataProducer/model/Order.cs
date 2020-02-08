using System;
using System.Collections.Generic;
using System.Text;

namespace FakeDataProducer
{
    //  WARNING : This class must stay aligned with model.json

    public class Order
    {
        public string OrderID { get; set; }
        public string CustomerID { get; set; }
        public string TrackingID { get; set; }
        public DateTime DT { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
