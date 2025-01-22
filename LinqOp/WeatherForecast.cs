namespace LinqOp
{
    public class WeatherForecast
    {
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string? Summary { get; set; }
    }


    // View Model
    public class OrderViewModel
    {
        public int OrderID { get; set; }
        public string? OrderNumber { get; set; }
        public DateTime Date { get; set; }
        public int Vendor { get; set; }
        public long StoreID { get; set; }
        public bool IsFinished { get; set; }
        public string? VendorName { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalRetail { get; set; }
        public decimal Margin { get; set; }
    }
}
