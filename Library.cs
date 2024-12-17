using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using RuriLib.Attributes;
using RuriLib.Legacy.Blocks;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using HttpMethod = System.Net.Http.HttpMethod;

namespace Http2;

[BlockCategory("Http", "Blocks For Different Http Operations", "#CD3255")]
public class Library : BlockRequest
{
    [Block("Performs A Custom Http Request With A Specific Version", name = "Http2Request")]
    public static async Task<string> Http2Request(
        BotData Data,
        string Url,
        RuriLib.Functions.Http.HttpMethod Method,
        bool AutoRedirect,
        string Body,
        Dictionary<string, string> Headers,
        Dictionary<string, string> Cookies,
        int Timeout,
        bool RawOutput,
        string ContentType = "application/x-www-form-urlencoded",
        string Version = "2.0")
    {
        Version HttpVersion = Version switch
        {
            "3.0" => new Version(3, 0),
            "2.0" => new Version(2, 0),
            _ => new Version(1, 1),
        };

        using HttpClientHandler Handler = new HttpClientHandler
        {
            AllowAutoRedirect = AutoRedirect
        };

        if (Data.Proxy != null)
        {
            WebProxy Proxy = new WebProxy(Data.Proxy.Host, Data.Proxy.Port);

            if (Data.Proxy.NeedsAuthentication)
                Proxy.Credentials = new NetworkCredential(Data.Proxy.Username, Data.Proxy.Password);

            Handler.Proxy = Proxy;
        }

        using HttpClient Client = new HttpClient(Handler)
        {
            DefaultRequestVersion = HttpVersion,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
            Timeout = Timeout > 0 ? TimeSpan.FromSeconds(Timeout) : TimeSpan.FromSeconds(60)
        };

        HttpRequestMessage Request = new HttpRequestMessage(new HttpMethod(Method.ToString()), Url)
        {
            Content = Body != string.Empty ? new StringContent(Body, Encoding.UTF8, ContentType) : null,
        };

        foreach (KeyValuePair<string, string> Header in Headers ?? [])
            Request.Headers.Add(Header.Key, Header.Value);

        if (Cookies?.Count > 0)
            Request.Headers.Add("Cookie", string.Join("; ", Cookies.Select(x => $"{x.Key}={x.Value}")));

        HttpResponseMessage Response = await Client.SendAsync(Request);

        byte[] Buffer = await Response.Content.ReadAsByteArrayAsync();
        string Content = Encoding.UTF8.GetString(Buffer);

        #region Request-Logging
        Data.Logger.Log(">> Http2Request\n", LogColors.DarkOrchid);

        Data.Logger.Log($"Request Method: {Method} / HTTP/{HttpVersion}", LogColors.WhiteSmoke);
        Data.Logger.Log($"Url: {Url}", LogColors.WhiteSmoke);

        if (Headers?.Count > 0)
        {
            Data.Logger.Log("Request Headers:", LogColors.WhiteSmoke);
            foreach (KeyValuePair<string, string> Header in Headers)
                Data.Logger.Log($"  {Header.Key}: {Header.Value}", LogColors.WhiteSmoke);
        }

        if (Cookies?.Count > 0)
        {
            Data.Logger.Log("Request Cookies:", LogColors.WhiteSmoke);
            foreach (KeyValuePair<string, string> Cookie in Cookies)
                Data.Logger.Log($"  {Cookie.Key}={Cookie.Value}", LogColors.WhiteSmoke);
        }

        if (Body != string.Empty)
            Data.Logger.Log($"Request Body: {Body}", LogColors.WhiteSmoke);
        #endregion

        #region Response-Logging
        Data.Logger.Log($"Response Code: {Response.StatusCode}\n", LogColors.Yellow);

        if (Response.Headers?.Count() > 0)
        {
            Data.Logger.Log("Received Headers:", LogColors.BluePurple);
            foreach (KeyValuePair<string, IEnumerable<string>> Header in Response.Headers)
                Data.Logger.Log($"  {Header.Key}: {string.Join(" ", Header.Value)}", LogColors.PurplePizzazz);
        }
        else
        {
            Data.Logger.Log("No Headers Received", LogColors.Pink);
        }

        Data.Logger.Log("Received Payload:", LogColors.AndroidGreen);

        if (RawOutput)
            Data.Logger.Log(string.Join(", ", Buffer.Select(x => $"0x{x:X2}")), LogColors.Amber);

        Data.Logger.Log($"{Content}", LogColors.CaribbeanGreen);
        #endregion

        return Content;
    }
}
