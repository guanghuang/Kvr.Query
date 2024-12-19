using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kvr.Query.Tests.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int AddressId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public CustomerAddress? Address { get; set; }
    }
    
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public int CategoryId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Amount { get; set; }
        public OrderDetail Detail { get; set; }
        public Category Category { get; set; }
    }

    [Table("CustomerAddresses")]
    public class CustomerAddress
    {
        [Key]
        public int AddressId { get; set; }
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
} 