using LinqOp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LinqOp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly OrderContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        public ValuesController(OrderContext context, IWebHostEnvironment  hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: api/<ValuesController>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var orders = GetOrders(1, 1);
            return Ok(await orders.ToListAsync());
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Orders()
        {
            return await ReadJsonFromFile("data-all-order.json");
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> OrderItems()
        {
            return await ReadJsonFromFile("data-order-items.json");
        }


        [HttpPost("[action]")]
        public async Task<IActionResult> Items()
        {
            return await ReadJsonFromFile("data-items.json");
        }

        private async Task<IActionResult> ReadJsonFromFile(string fileName)
        {
            string filePath = Path.Combine(_hostEnvironment.WebRootPath, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { message = $"JSON file '{fileName}' not found" });
            }

            string jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
            return Content(jsonContent, "application/json");
        }
        private IQueryable<OrderViewModel> GetOrders(long StoreId, int? Payee = null, DateTime? FromDate = null, DateTime? ToDate = null)
        {
            var query = _context.tblPOSOrderMasters
                //.Include(a => a.tblPOSOrderDetails)
                .Where(p => p.StoreID == StoreId);

            if (FromDate.HasValue && ToDate.HasValue)
            {
                var fromDateTruncated = FromDate.Value.Date;
                var toDateTruncated = ToDate.Value.Date;
                query = query.Where(p => p.Date.Date >= fromDateTruncated && p.Date.Date <= toDateTruncated);
            }

            if (Payee.HasValue)
            {
                query = query.Where(p => p.Vendor == Payee);
            }

            //var orderDetails = _context.tblPOSOrderDetails
            //.Join(_context.tblVendorsItems,
            //    od => od.Itemkey,
            //    vi => vi.Itemkey,
            //    (od, vi) => new
            //    {
            //        od.OrderID,
            //        UnitCost = vi.UnitCost,
            //        UnitRetail = vi.UnitRetail
            //    });
            //return query.Select(s => new OrderViewModel
            //{
            //    OrderID = s.OrderID,
            //    OrderNumber = s.OrderNumber,
            //    Date = s.Date,
            //    Vendor = s.Vendor,
            //    StoreID = s.StoreID,
            //    IsFinished = s.IsFinished,
            //    TotalCost = orderDetails.Where(od => od.OrderID == s.OrderID).Sum(od => od.UnitCost),
            //    TotalRetail = orderDetails.Where(od => od.OrderID == s.OrderID).Sum(od => od.UnitRetail),
            //    Margin = 0
            //});



            var orderDetails = from od in _context.tblPOSOrderDetails
                               join vi in _context.tblVendorsItems
                               on od.Itemkey equals vi.Itemkey
                               let orderItem = new
                               {
                                   od.OrderID,
                                   vi.UnitCost,
                                   vi.UnitRetail,
                                   Margin = vi.UnitRetail - vi.UnitCost
                               }
                               group orderItem by orderItem.OrderID into g
                               select new
                               {
                                   OrderID = g.Key,
                                   TotalCost = g.Sum(x => x.UnitCost),
                                   TotalRetail = g.Sum(x => x.UnitRetail),
                                   Margin = g.Sum(x => x.UnitRetail - x.UnitCost),
                                   TotalMarginPercentage = g.Sum(x => x.UnitRetail) > 0
                                      ? (Math.Round(g.Sum(x => x.Margin), 2) / g.Sum(x => x.UnitRetail)) * 100
                                      : 0
                               };

            return query.Select(s => new OrderViewModel
            {
                OrderID = s.OrderID,
                OrderNumber = s.OrderNumber,
                Date = s.Date,
                Vendor = s.Vendor,
                StoreID = s.StoreID,
                IsFinished = s.IsFinished,
                TotalCost = orderDetails.First(od => od.OrderID == s.OrderID).TotalCost,
                TotalRetail = orderDetails.First(od => od.OrderID == s.OrderID).TotalRetail,
                Margin = orderDetails.First(od => od.OrderID == s.OrderID).TotalMarginPercentage,
            });
        }
    }
}
