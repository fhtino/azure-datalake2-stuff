using Azure.Storage;
using Azure.Storage.Files.DataLake;
using CsvHelper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;


namespace FakeDataProducer
{

    class Program
    {

        private static Random _rnd = new Random();
        private static IConfiguration _configuration;

        private static int _numberOfOrders;
        private static int _ordersCutOff;
        private static int _minRowsPerOrder;
        private static int _maxRowsPerOrder;
        private static int _ordersRowsCutOff;

        private static string _ADLSaccountName;
        private static string _ADLSaccountKey;
        private static string _ADLSfileSystemName;
        private static string _ADLSworkingFolder;


        private static List<Tuple<Type, string>> _csvFileList = new List<Tuple<Type, string>>();   // type, filename

        private static string _ADLSBaseURL;
        private static DataLakeServiceClient _ADLSClient;
        private static DataLakeFileSystemClient _ADLSFileSystemClient;
        private static DataLakeDirectoryClient _ADLSDirectoryClient;



        static void Main(string[] args)
        {
            ReadConfig();
            SetupADLSClient();
            Step1_CreateCSVFiles();
            Step2_CreateModelJson();
        }



        private static void Step1_CreateCSVFiles()
        {
            List<Product> products = ReadAWProducts();
            WriteCSVFile(products);

            var orderBuffer = new List<Order>();
            var orderRowsBuffer = new List<OrderRow>();

            for (int i = 0; i < _numberOfOrders; i++)
            {
                if (i % 10000 == 0)
                    Console.WriteLine($"Creating order {i}");

                var order = new Order()
                {
                    OrderID = (10000 + i).ToString(),
                    CustomerID = Guid.NewGuid().ToString().ToUpper().Substring(0, 3),
                    TrackingID = Guid.NewGuid().ToString().Replace("-", "").ToUpper(),
                    DT = DateTime.UtcNow.AddDays(-Math.Pow(365, _rnd.NextDouble())),
                    TotalPrice = 0
                };
                orderBuffer.Add(order);

                int n = new Random().Next(5, 20);
                for (int j = 0; j < n; j++)
                {
                    int pID = _rnd.Next(0, products.Count);
                    var orderRow = new OrderRow()
                    {
                        OrderID = order.OrderID,
                        RowNumber = j + 1,
                        ProductID = pID,
                        Quantity = _rnd.Next(0, 10),
                        UnitPrice = (decimal)Math.Round(products[pID].Price * (1.0 + (_rnd.NextDouble() - 0.5) / 10.0), 2),
                    };
                    orderRowsBuffer.Add(orderRow);
                    order.TotalPrice += orderRow.UnitPrice * orderRow.Quantity;
                }

                if (orderBuffer.Count > _ordersCutOff)
                {
                    WriteCSVFile(orderBuffer);
                    orderBuffer.Clear();
                }
                if (orderRowsBuffer.Count > _ordersRowsCutOff)
                {
                    WriteCSVFile(orderRowsBuffer);
                    orderRowsBuffer.Clear();
                }
            }

            WriteCSVFile(orderBuffer);
            WriteCSVFile(orderRowsBuffer);
        }


        private static void Step2_CreateModelJson()
        {
            var json = ModelJsonBuilder.Run(
               new List<Type>
               {
                    typeof(Product),
                    typeof(Order),
                    typeof(OrderRow),
               },
               new List<Tuple<Type, string, Type, string>>
               {
                    new Tuple<Type, string, Type, string>(typeof(Order), nameof(Order.OrderID), typeof(OrderRow), nameof(OrderRow.OrderID)),
                    new Tuple<Type, string, Type, string>(typeof(Product), nameof(Product.ProductID), typeof(OrderRow), nameof(OrderRow.ProductID))
               },
               _csvFileList);

            string fileName = "model.json";

            if (_configuration.GetValue<bool>("DumpToLocalFile"))
            {
                File.WriteAllText(fileName, json);
            }

            if (_configuration.GetValue<bool>("UploadToADLSGen2"))
            {
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                var adlsFileClient = _ADLSDirectoryClient.GetFileClient("model.json");
                Console.WriteLine($"Uploading {adlsFileClient.Uri.ToString()}");
                adlsFileClient.Create();
                adlsFileClient.Append(new MemoryStream(buffer), 0);
                adlsFileClient.Flush(buffer.Length);
            }

        }


        private static void ReadConfig()
        {
            _configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               //.AddEnvironmentVariables()
               //.AddCommandLine(args)
               .Build();

            _numberOfOrders = _configuration.GetValue<int>("numberOfOrders");
            _ordersCutOff = _configuration.GetValue<int>("ordersCutOff");
            _minRowsPerOrder = _configuration.GetValue<int>("minRowsPerOrder");
            _maxRowsPerOrder = _configuration.GetValue<int>("maxRowsPerOrder");
            _ordersRowsCutOff = _configuration.GetValue<int>("ordersRowsCutOff");

            _ADLSaccountName = _configuration.GetValue<string>("ADLSaccountName");
            _ADLSaccountKey = _configuration.GetValue<string>("ADLSaccountKey");
            _ADLSfileSystemName = _configuration.GetValue<string>("ADLSfileSystemName");
            _ADLSworkingFolder = _configuration.GetValue<string>("ADLSworkingFolder");
        }


        private static void SetupADLSClient()
        {
            _ADLSBaseURL = "https://" + _ADLSaccountName + ".dfs.core.windows.net";
            _ADLSClient = new DataLakeServiceClient(new Uri(_ADLSBaseURL),
                                                    new StorageSharedKeyCredential(_ADLSaccountName, _ADLSaccountKey));
            _ADLSFileSystemClient = _ADLSClient.GetFileSystemClient(_ADLSfileSystemName);
            _ADLSDirectoryClient = _ADLSFileSystemClient.GetDirectoryClient(_ADLSworkingFolder);

            if (_configuration.GetValue<bool>("UploadToADLSGen2"))
            {
                _ADLSDirectoryClient.Create();
            }
        }


        private static List<Product> ReadAWProducts()
        {
            var lines = File.ReadAllLines("data/AWproducts.txt");
            int i = 0;
            return lines.Select(x => x.Split('\t'))
                        .Where(x => x.Length == 2)
                        .Select(x =>
                              new Product()
                              {
                                  ProductID = i++,
                                  Name = x[0],
                                  Code = x[1],
                                  Price = Math.Abs(x[0].GetHashCode() % 10000) / 100.0
                              })
                        .ToList();
        }


        private static void WriteCSVFile<T>(List<T> items)
        {
            if (items.Count == 0)
                return;

            byte[] csvBody = BuildCSVBody(items.Select(x => (object)x));

            Type itemType = items.GetType().GetGenericArguments()[0];
            int n = _csvFileList.Count(x => x.Item1 == itemType);
            string fileName = itemType.Name + "_" + n + ".csv";
            string fileNameRelative = $"{itemType.Name}/{fileName}";

            var adlsFileClient = _ADLSDirectoryClient.GetFileClient(fileNameRelative);
            _csvFileList.Add(new Tuple<Type, string>(itemType, adlsFileClient.Uri.ToString()));

            if (_configuration.GetValue<bool>("DumpToLocalFile"))
            {
                File.WriteAllBytes(fileName, csvBody);
            }

            if (_configuration.GetValue<bool>("UploadToADLSGen2"))
            {
                Console.WriteLine($"Uploading {adlsFileClient.Uri.ToString()}");
                adlsFileClient.Create();
                adlsFileClient.Append(new MemoryStream(csvBody), 0);
                adlsFileClient.Flush(csvBody.Length);
            }
        }


        private static byte[] BuildCSVBody(IEnumerable<Object> items)
        {
            var sw = new StringWriter();
            using (var csv = new CsvWriter(sw, CultureInfo.InvariantCulture))
            {
                csv.Configuration.HasHeaderRecord = false;
                csv.Configuration.Delimiter = ",";
                csv.Configuration.CultureInfo = CultureInfo.InvariantCulture;
                csv.Configuration.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = new[] { "o" };
                csv.WriteRecords(items);
            }
            byte[] csvBody = Encoding.UTF8.GetBytes(sw.ToString());
            return csvBody;
        }

    }
}
