using Hci.Gma.CodeGenerators.Cli.Dtos;

namespace Hci.Gma.CodeGenerators.Cli;

internal static class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var price = new Price()
        {
            Amount = 100,
            Type = PriceType.TotalCost
        };
        Console.WriteLine(
            $"price.Type =  {price.Type}; price.PaymentType = {price.PaymentType}; price.Amount = {price.Amount}");
    }
}
