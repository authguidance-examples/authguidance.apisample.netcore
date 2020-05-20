﻿namespace SampleApi.Host.Plumbing.Claims
{
    using System.Threading.Tasks;

    /*
     * A base class for enabling custom claims to be included in the cache after OAuth processing
     */
    public class CustomClaimsProvider<TClaims>
        where TClaims : CoreApiClaims, new()
    {
        /*
         * This is overridden by base classes
         */
        public virtual Task AddCustomClaimsAsync(string accessToken, TClaims claims)
        {
            return Task.FromResult(0);
        }
    }
}