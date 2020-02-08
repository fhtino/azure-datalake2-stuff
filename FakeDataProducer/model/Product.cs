using System;
using System.Collections.Generic;
using System.Text;

namespace FakeDataProducer
{

    //  WARNING : This class must stay aligned with model.json

    public class Product
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public double Price { get; set; }
    }
}
