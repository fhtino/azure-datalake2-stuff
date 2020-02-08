using System;
using System.Collections.Generic;
using System.Text;

namespace FakeDataProducer
{

    //  WARNING : This class must stay aligned with model.json

    public class OrderRow
    {
        public string OrderID { get; set; }     
        
        public int RowNumber { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }        
    }
}
