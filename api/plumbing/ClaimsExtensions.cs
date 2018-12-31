namespace api.Plumbing
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Security.Claims;
    using IdentityModel;
    using Microsoft.AspNetCore.Http;
    using api.Entities;

    /*
     * Extensions related to claims in a principal
     */
    public static class ClaimsExtensions
    {
        /*
         * Our non standard claims
         */
        class CustomClaimTypes
        {
            public const string UserCompanyId = "userCompanyId";
        }

        /*
         * The Identity Model introspection already sets token claims in the claims principal
         * So we only need to add claims for central user data or product user data
         */
        public static void SetAdditionalApiClaims(this HttpContext context, ApiClaims claims)
        {
            // Get the claims identity
            var identity = context.User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                throw new InvalidOperationException(
                    "An unexpected identity type was encountered when setting claims");
            }

            // Make a sanity check that we have expected claims
            if (claims.UserInfo == null)
            {
                throw new InvalidOperationException(
                    "SetAdditionalApiClaims was called but no additional claims are present");
            }
            
            // Add central user data claims
            identity.AddClaim(new Claim(JwtClaimTypes.GivenName, claims.UserInfo.GivenName));
            identity.AddClaim(new Claim(JwtClaimTypes.FamilyName, claims.UserInfo.FamilyName));
            identity.AddClaim(new Claim(JwtClaimTypes.Email, claims.UserInfo.Email));
            
            // Add product user claims
            foreach (var companyId in claims.UserCompanyIds)
            {
                var stringValue = Convert.ToString(companyId, CultureInfo.InvariantCulture);
                identity.AddClaim(new Claim(CustomClaimTypes.UserCompanyId, stringValue));
            }
        }

        /*
         * Read the claims we are interested in from the claims principal into a type safe object
         */
        public static ApiClaims GetApiClaims(this ClaimsPrincipal principal)
        {
            // Read token values
            string userId = principal.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Subject)?.Value;
            string callingApplicationId = principal.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.ClientId)?.Value;
            var scopes = principal.Claims.Where(c => c.Type == JwtClaimTypes.Scope).Select(c => c.Value).ToList();
            
            // Read user info values
            string givenName = principal.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.GivenName)?.Value;
            string familyName = principal.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.FamilyName)?.Value;
            string email = principal.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Email)?.Value;
            
            // Read custom values
            var userCompanyIds = principal.Claims.Where(c => c.Type == CustomClaimTypes.UserCompanyId).Select(c => c.Value).ToList();

            // Sanity check
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(callingApplicationId) ||
                scopes.Count == 0)
            {
                throw new InvalidOperationException("GetApiClaims was called but no OAuth claims are in the claims principal");
            }

            // Create the object
            var claims = new ApiClaims(userId, callingApplicationId, scopes.ToArray());

            // Return central user data claims if they exist
            if (!string.IsNullOrWhiteSpace(givenName) &&
                !string.IsNullOrWhiteSpace(familyName) &&
                !string.IsNullOrWhiteSpace(email))
            {
                claims.SetCentralUserData(givenName, familyName, email);
            }

            // Return product user data claims if they exist
            if (userCompanyIds.Count > 0)
            {
                var intValues = userCompanyIds.Select(s => Convert.ToInt32(s, CultureInfo.InvariantCulture));
                claims.setProductSpecificUserRights(intValues.ToArray());
            }
            
            return claims;
        }

        /*
         * Look up the expiry time from the token, whose claim is added by Identity Model introspection
         */
        public static int GetAccessTokenExpirationClaim(this ClaimsPrincipal principal)
        {
            var expiryClaim = principal.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Expiration);
            if (expiryClaim == null)
            {
                throw new InvalidOperationException("Unable to find expiry claim with which to cache claims");
            }

            return Convert.ToInt32(expiryClaim.Value, CultureInfo.InvariantCulture);
        }
    }
}