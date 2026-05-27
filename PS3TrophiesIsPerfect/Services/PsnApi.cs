using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PS3TrophiesIsPerfect.Models;

namespace PS3TrophiesIsPerfect.Services
{
    /// <summary>The signed-in account's overall trophy standing (across every platform), for the profile chip.</summary>
    public sealed class PsnSummary
    {
        public int Level { get; set; }
        public int Platinum { get; set; }
        public int Gold { get; set; }
        public int Silver { get; set; }
        public int Bronze { get; set; }

        public int Total => Platinum + Gold + Silver + Bronze;
    }

    /// <summary>
    /// Talks to Sony's own (undocumented but stable) PSN endpoints for trophy data — the same ones the
    /// PlayStation app uses. Auth is the standard NPSSO → access-code → token flow; tokens are cached in
    /// settings and silently refreshed. Game/trophy art comes from Sony's open CDN (no Cloudflare).
    ///
    /// Constants below (client id, basic-auth, redirect, scope) are the well-known PlayStation mobile-app
    /// values used by every PSN library; they are not secrets specific to this user.
    /// </summary>
    public sealed class PsnApi
    {
        private const string AuthBase = "https://ca.account.sony.com/api/authz/v3/oauth";
        private const string ApiBase = "https://m.np.playstation.com/api/trophy/v1";
        private const string ClientId = "09515159-7237-4370-9b40-3806e67c0891";
        private const string RedirectUri = "com.scee.psxandroid.scecompcall://redirect";
        private const string Scope = "psn:mobile.v2.core psn:clientapp";
        private const string BasicAuth =
            "Basic MDk1MTUxNTktNzIzNy00MzcwLTliNDAtMzgwNmU2N2MwODkxOnVjUGprYTV0bnRCMktxc1A=";
        private const string UserAgent = "com.scee.psxandroid.scecompcall/6.30.0";

        /// <summary>Thrown when there is no usable token and no NPSSO to mint one — caller must prompt for a token.</summary>
        public sealed class AuthRequiredException : Exception { }

        private readonly AppSettings _s;

        public PsnApi(AppSettings settings)
        {
            _s = settings;
        }

        /// <summary>True once we have something to authenticate with (a stored NPSSO or refresh token).</summary>
        public bool HasCredentials =>
            !string.IsNullOrEmpty(_s.PsnNpsso) || !string.IsNullOrEmpty(_s.PsnRefreshToken);

        /// <summary>Sign in with a freshly pasted NPSSO token, storing the resulting tokens. Throws on a bad token.</summary>
        public void SignIn(string npsso)
        {
            string code = ExchangeNpssoForCode(npsso);
            StoreTokens(ExchangeCodeForTokens(code));
            _s.PsnNpsso = npsso;
            _s.Save();
        }

        // ---- Trophy data -------------------------------------------------------------------------

        /// <summary>The signed-in account's PS3 games with earned/total trophies and completion %.</summary>
        public List<GameProgress> GetPs3Games()
        {
            string token = AccessToken();
            var games = new List<GameProgress>();
            int offset = 0,
                totalItems = int.MaxValue;
            while (offset < totalItems)
            {
                string json = ApiGet(
                    $"{ApiBase}/users/me/trophyTitles?limit=800&offset={offset}",
                    token
                );
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("trophyTitles", out var titles))
                        break;
                    totalItems = Int(root, "totalItemCount"); // across all platforms

                    int pageCount = 0;
                    foreach (var t in titles.EnumerateArray())
                    {
                        pageCount++;
                        string platform = Str(t, "trophyTitlePlatform");
                        if (
                            platform == null
                            || platform.IndexOf("PS3", StringComparison.OrdinalIgnoreCase) < 0
                        )
                            continue;

                        var defined = t.TryGetProperty("definedTrophies", out var d) ? d : default;
                        DateTime lastUpdated = DateTime.MinValue;
                        if (
                            t.TryGetProperty("lastUpdatedDateTime", out var lu)
                            && lu.ValueKind == JsonValueKind.String
                        )
                            DateTime.TryParse(
                                lu.GetString(),
                                null,
                                System.Globalization.DateTimeStyles.AdjustToUniversal
                                    | System.Globalization.DateTimeStyles.AssumeUniversal,
                                out lastUpdated
                            );
                        games.Add(
                            new GameProgress
                            {
                                Name = Str(t, "trophyTitleName"),
                                GameId = Str(t, "npCommunicationId"),
                                IconUrl = Str(t, "trophyTitleIconUrl"),
                                Earned = SumTrophies(t, "earnedTrophies"),
                                Total = SumTrophies(t, "definedTrophies"),
                                Percent = Int(t, "progress"),
                                Platinum = Int(defined, "platinum"),
                                Gold = Int(defined, "gold"),
                                Silver = Int(defined, "silver"),
                                Bronze = Int(defined, "bronze"),
                                HasDlc =
                                    t.TryGetProperty("hasTrophyGroups", out var h)
                                    && h.ValueKind == JsonValueKind.True,
                                LastUpdated = lastUpdated,
                            }
                        );
                    }
                    if (pageCount == 0)
                        break; // no more pages
                    offset += pageCount;
                }
            }
            return games;
        }

        /// <summary>The account's overall trophy level + total earned trophies by type, across all platforms.</summary>
        public PsnSummary GetTrophySummary()
        {
            string token = AccessToken();
            string json = ApiGet($"{ApiBase}/users/me/trophySummary", token);
            using (var doc = JsonDocument.Parse(json))
            {
                var r = doc.RootElement;
                var earned = r.TryGetProperty("earnedTrophies", out var e) ? e : default;
                return new PsnSummary
                {
                    Level = Level(r, "trophyLevel"),
                    Platinum = Int(earned, "platinum"),
                    Gold = Int(earned, "gold"),
                    Silver = Int(earned, "silver"),
                    Bronze = Int(earned, "bronze"),
                };
            }
        }

        /// <summary>The PSN trophy level comes back as a number on some accounts and a string on others.</summary>
        private static int Level(JsonElement e, string name)
        {
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var v))
                return 0;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i))
                return i;
            if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var s))
                return s;
            return 0;
        }

        /// <summary>One PS3 game's trophies: definitions (name/detail/icon/type) merged with this account's
        /// earned status + unlock timestamps, in the game's trophy order.</summary>
        public List<TrophyDetail> GetTrophies(string npCommunicationId)
        {
            string token = AccessToken();
            const string svc = "npServiceName=trophy"; // PS3/PS4/Vita use "trophy"

            // 0) Trophy groups — for "Base Game" / DLC section names.
            var groupName = new Dictionary<string, string>();
            try
            {
                string groups = ApiGet(
                    $"{ApiBase}/npCommunicationIds/{npCommunicationId}/trophyGroups?{svc}",
                    token
                );
                using (var doc = JsonDocument.Parse(groups))
                    if (doc.RootElement.TryGetProperty("trophyGroups", out var gs))
                        foreach (var g in gs.EnumerateArray())
                        {
                            string id = Str(g, "trophyGroupId");
                            if (id == null)
                                continue;
                            groupName[id] =
                                id == "default"
                                    ? "Base Game"
                                    : (Str(g, "trophyGroupName") ?? "DLC");
                        }
            }
            catch
            { /* no group info — everything becomes Base Game below */
            }

            // 1) Definitions — names, descriptions, icons, types, which group each belongs to.
            var byId = new Dictionary<int, TrophyDetail>();
            string defs = ApiGet(
                $"{ApiBase}/npCommunicationIds/{npCommunicationId}/trophyGroups/all/trophies?{svc}",
                token
            );
            using (var doc = JsonDocument.Parse(defs))
                foreach (var tr in doc.RootElement.GetProperty("trophies").EnumerateArray())
                {
                    int id = Int(tr, "trophyId");
                    string gid = Str(tr, "trophyGroupId") ?? "default";
                    byId[id] = new TrophyDetail
                    {
                        Id = id,
                        Type = Str(tr, "trophyType") ?? "bronze",
                        Name = Str(tr, "trophyName") ?? "Hidden trophy",
                        Detail = Str(tr, "trophyDetail") ?? "",
                        IconUrl = Str(tr, "trophyIconUrl"),
                        GroupId = gid,
                        GroupName = groupName.TryGetValue(gid, out var gn)
                            ? gn
                            : (gid == "default" ? "Base Game" : "DLC"),
                    };
                }

            // 2) This account's earned status + timestamps + rarity — merge onto the definitions by trophy id.
            string earned = ApiGet(
                $"{ApiBase}/users/me/npCommunicationIds/{npCommunicationId}/trophyGroups/all/trophies?{svc}",
                token
            );
            using (var doc = JsonDocument.Parse(earned))
                foreach (var tr in doc.RootElement.GetProperty("trophies").EnumerateArray())
                {
                    int id = Int(tr, "trophyId");
                    if (!byId.TryGetValue(id, out var d))
                        continue;
                    d.Earned =
                        tr.TryGetProperty("earned", out var e) && e.ValueKind == JsonValueKind.True;
                    d.EarnedRate = Dbl(tr, "trophyEarnedRate");
                    if (
                        d.Earned
                        && tr.TryGetProperty("earnedDateTime", out var dt)
                        && dt.ValueKind == JsonValueKind.String
                        && DateTime.TryParse(
                            dt.GetString(),
                            null,
                            System.Globalization.DateTimeStyles.AdjustToUniversal
                                | System.Globalization.DateTimeStyles.AssumeUniversal,
                            out var when
                        )
                    )
                        d.EarnedUtc = when;
                }

            var list = new List<TrophyDetail>(byId.Values);
            list.Sort((a, b) => a.Id.CompareTo(b.Id));
            return list;
        }

        // ---- Token management --------------------------------------------------------------------

        /// <summary>A valid access token: cached if fresh, refreshed if expired, re-minted from the NPSSO if
        /// the refresh fails. Throws <see cref="AuthRequiredException"/> when nothing works (re-prompt needed).</summary>
        private string AccessToken()
        {
            if (
                !string.IsNullOrEmpty(_s.PsnAccessToken)
                && DateTime.UtcNow < _s.PsnAccessExpiryUtc.AddSeconds(-60)
            )
                return _s.PsnAccessToken;

            if (!string.IsNullOrEmpty(_s.PsnRefreshToken))
            {
                try
                {
                    StoreTokens(RefreshTokens(_s.PsnRefreshToken));
                    _s.Save();
                    return _s.PsnAccessToken;
                }
                catch
                { /* refresh token likely expired — fall through to NPSSO */
                }
            }

            if (!string.IsNullOrEmpty(_s.PsnNpsso))
            {
                try
                {
                    StoreTokens(ExchangeCodeForTokens(ExchangeNpssoForCode(_s.PsnNpsso)));
                    _s.Save();
                    return _s.PsnAccessToken;
                }
                catch
                { /* NPSSO expired too */
                }
            }

            throw new AuthRequiredException();
        }

        private sealed class Tokens
        {
            public string Access;
            public string Refresh;
            public int ExpiresIn;
        }

        private void StoreTokens(Tokens t)
        {
            _s.PsnAccessToken = t.Access;
            if (!string.IsNullOrEmpty(t.Refresh))
                _s.PsnRefreshToken = t.Refresh;
            _s.PsnAccessExpiryUtc = DateTime.UtcNow.AddSeconds(
                t.ExpiresIn > 0 ? t.ExpiresIn : 3600
            );
        }

        private static string ExchangeNpssoForCode(string npsso)
        {
            string url =
                $"{AuthBase}/authorize?access_type=offline&client_id={ClientId}"
                + $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}&response_type=code"
                + $"&scope={Uri.EscapeDataString(Scope)}";

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.UserAgent = UserAgent;
            req.AllowAutoRedirect = false; // the access code lives in the 302 Location header
            req.CookieContainer = new CookieContainer();
            req.CookieContainer.Add(
                new Uri("https://ca.account.sony.com"),
                new Cookie("npsso", npsso)
            );

            using (var resp = (HttpWebResponse)req.GetResponse())
            {
                string location = resp.Headers["Location"];
                var m = location != null ? Regex.Match(location, "[?&]code=([^&]+)") : Match.Empty;
                if (!m.Success)
                    throw new Exception(
                        "PSN did not return an access code — the NPSSO token is likely expired or wrong."
                    );
                return Uri.UnescapeDataString(m.Groups[1].Value);
            }
        }

        private static Tokens ExchangeCodeForTokens(string code) =>
            PostForToken(
                $"code={Uri.EscapeDataString(code)}"
                    + $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}"
                    + "&grant_type=authorization_code&token_format=jwt"
            );

        private static Tokens RefreshTokens(string refreshToken) =>
            PostForToken(
                $"refresh_token={Uri.EscapeDataString(refreshToken)}"
                    + "&grant_type=refresh_token&token_format=jwt"
                    + $"&scope={Uri.EscapeDataString(Scope)}"
            );

        private static Tokens PostForToken(string body)
        {
            var req = (HttpWebRequest)WebRequest.Create($"{AuthBase}/token");
            req.Method = "POST";
            req.UserAgent = UserAgent;
            req.ContentType = "application/x-www-form-urlencoded";
            req.Headers["Authorization"] = BasicAuth;
            byte[] payload = Encoding.UTF8.GetBytes(body);
            using (var s = req.GetRequestStream())
                s.Write(payload, 0, payload.Length);

            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream()))
            using (var doc = JsonDocument.Parse(reader.ReadToEnd()))
            {
                var r = doc.RootElement;
                return new Tokens
                {
                    Access = Str(r, "access_token"),
                    Refresh = Str(r, "refresh_token"),
                    ExpiresIn = Int(r, "expires_in"),
                };
            }
        }

        private static string ApiGet(string url, string token)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.UserAgent = UserAgent;
            req.Accept = "application/json";
            req.Headers["Authorization"] = "Bearer " + token;
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream()))
                return reader.ReadToEnd();
        }

        // ---- JSON helpers ------------------------------------------------------------------------

        private static string Str(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString()
                : null;

        private static int Int(JsonElement e, string name) =>
            e.ValueKind == JsonValueKind.Object
            && e.TryGetProperty(name, out var v)
            && v.ValueKind == JsonValueKind.Number
            && v.TryGetInt32(out var i)
                ? i
                : 0;

        // trophyEarnedRate comes back as a string like "52.30" (sometimes a number) — accept either.
        private static double Dbl(JsonElement e, string name)
        {
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var v))
                return 0;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d))
                return d;
            if (
                v.ValueKind == JsonValueKind.String
                && double.TryParse(
                    v.GetString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var s
                )
            )
                return s;
            return 0;
        }

        private static int SumTrophies(JsonElement title, string name)
        {
            if (!title.TryGetProperty(name, out var t))
                return 0;
            return Int(t, "bronze") + Int(t, "silver") + Int(t, "gold") + Int(t, "platinum");
        }
    }
}
