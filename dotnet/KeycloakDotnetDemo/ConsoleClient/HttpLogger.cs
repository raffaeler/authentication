using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleClient;

internal class HttpLogger : DelegatingHandler
{
    public HttpLogger() : this(new HttpClientHandler())
    {
    }

    public HttpLogger(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var defaultColor = Console.ForegroundColor;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Request:");
        Console.ForegroundColor = defaultColor;

        Console.WriteLine(request.ToString());
        if (request.Content != null)
        {
            var body = await request.Content.ReadAsStringAsync();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(body);
            Console.ForegroundColor = defaultColor;
        }
        Console.WriteLine();

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        Console.ForegroundColor = response.IsSuccessStatusCode
                ? ConsoleColor.Green
                : ConsoleColor.Red;
        Console.WriteLine("Response:");
        Console.ForegroundColor = defaultColor;

        Console.WriteLine(response.ToString());
        if (response.Content != null)
        {
            var body = await response.Content.ReadAsStringAsync();

            Console.ForegroundColor = response.IsSuccessStatusCode
                ? ConsoleColor.DarkGray
                : ConsoleColor.Red;
            Console.WriteLine(body);
            Console.ForegroundColor = defaultColor;
        }
        Console.WriteLine();
        Console.WriteLine();

        return response;
    }
}
