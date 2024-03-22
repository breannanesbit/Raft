using RaftElection;

namespace RaftWeb1
{
    public class Productservice
    {
        public List<Product> products = new List<Product>
        {
        new Product { ProductItem = "Candy Bar", Cost = 1.00D, Quanity = 2 },
        new Product { ProductItem = "Football", Cost = 5.00D, Quanity = 4 },
        new Product { ProductItem = "Water Bottle", Cost = 2.00D, Quanity = 3 },
        new Product { ProductItem = "Pens", Cost = 1.00D, Quanity = 2 },
        };
    }
}
