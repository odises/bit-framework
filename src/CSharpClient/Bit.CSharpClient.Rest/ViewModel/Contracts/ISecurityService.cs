﻿using Bit.ViewModel.Implementations;
using IdentityModel.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.ViewModel.Contracts
{
    public class Token
    {
        public static implicit operator Token(Dictionary<string, string> props)
        {
            if (props == null)
                throw new ArgumentNullException(nameof(props));

            Token token = new Token
            {
                access_token = props[nameof(access_token)],
                expires_in = Convert.ToInt64(props[nameof(expires_in)]),
                token_type = props[nameof(token_type)]
            };

            if (props.ContainsKey(nameof(token.id_token)))
                token.id_token = props[nameof(token.id_token)];

            if (!props.ContainsKey(nameof(token.login_date)))
                token.login_date = DefaultDateTimeProvider.Current.GetCurrentUtcDateTime();
            else
                token.login_date = Convert.ToDateTime(props[nameof(login_date)]);

            return token;
        }

        public static implicit operator Token(TokenResponse tokenResponse)
        {
            if (tokenResponse == null)
                throw new ArgumentNullException(nameof(tokenResponse));

            return new Token
            {
                access_token = tokenResponse.AccessToken,
                expires_in = tokenResponse.ExpiresIn,
                login_date = DefaultDateTimeProvider.Current.GetCurrentUtcDateTime(),
                token_type = tokenResponse.TokenType
            };
        }

        public string access_token { get; set; }

        public string id_token { get; set; }

        public string token_type { get; set; }

        public long expires_in { get; set; }

        public DateTimeOffset? login_date { get; set; }
    }

    public class BitJwtToken
    {
        public virtual string UserId { get; set; }

        public virtual Dictionary<string, string> CustomProps { get; set; } = new Dictionary<string, string> { };

        public static BitJwtToken FromJson(string json)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));

            return DefaultJsonContentFormatter.Current.Deserialize<BitJwtToken>(json);
        }

        public static string ToJson(BitJwtToken bitJwtToken)
        {
            if (bitJwtToken == null)
                throw new ArgumentNullException(nameof(bitJwtToken));

            return DefaultJsonContentFormatter.Current.Serialize(bitJwtToken);
        }
    }

    public interface ISecurityService
    {
        Task<bool> IsLoggedInAsync(CancellationToken cancellationToken = default);

        Task<Token> LoginWithCredentials(string userName, string password, string client_id, string client_secret, string[] scopes = null, IDictionary<string, string> acr_values = null, CancellationToken cancellationToken = default);

        Task<Token> Login(object state = null, string client_id = null, IDictionary<string, string> acr_values = null, CancellationToken cancellationToken = default);

        Task<Token> GetCurrentTokenAsync(CancellationToken cancellationToken = default);

        Task Logout(object state = null, string client_id = null, CancellationToken cancellationToken = default);

        Uri GetLoginUrl(object state = null, string client_id = null, IDictionary<string, string> acr_values = null);

        Uri GetLogoutUrl(string id_token, object state = null, string client_id = null);

        Task OnSsoLoginLogoutRedirectCompleted(Uri url);

        bool UseSecureStorage();

        Task<BitJwtToken> GetBitJwtToken(CancellationToken cancellationToken);
    }
}
