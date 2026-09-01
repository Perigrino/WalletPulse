namespace WalletPulse;

public static class ApiEndpoints
{
    private const string ApiBase = "api";
    
    public static class Customers
    {
        private const string Base = $"{ApiBase}/customers";
        
        public const string Get = $"{Base}/{{id:guid}}";
        //public const string GetWallet = $"{Base}/wallets/{{id:guid}}";
        public const string GetAll = Base;
        public const string Create = Base;
        public const string Update = $"{Base}/{{id:guid}}";
        public const string Delete = $"{Base}/{{id:guid}}";
    }
    public static class CustomerWallet
    {
        private const string Base = $"{ApiBase}/wallet";
        
        public const string Get = $"{Base}/{{id:guid}}";
        public const string GetAll = Base;
        public const string Create = Base;
        public const string Update = $"{Base}/{{id:guid}}";
        public const string Delete = $"{Base}/{{id:guid}}";
    }
    
}