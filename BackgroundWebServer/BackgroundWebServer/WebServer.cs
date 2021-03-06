﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace BackgroundWebServer
{
    internal class WebServer
    {
        private const uint BufferSize = 8192;

        public async void Start()
        {
            var listener = new StreamSocketListener();

            await listener.BindServiceNameAsync("8081"); //the 8080 port is already in use

            listener.ConnectionReceived += async (sender, args) =>
            {
                var request = new StringBuilder();

                using (var input = args.Socket.InputStream)
                {
                    var data = new byte[BufferSize];
                    IBuffer buffer = data.AsBuffer();
                    var dataRead = BufferSize;

                    while (dataRead == BufferSize)
                    {
                        await input.ReadAsync(
                             buffer, BufferSize, InputStreamOptions.Partial);
                        request.Append(Encoding.UTF8.GetString(
                                                      data, 0, data.Length));
                        dataRead = buffer.Length;
                    }
                }

                string query = GetQuery(request);

                using (var output = args.Socket.OutputStream)
                {
                    using (var response = output.AsStreamForWrite())
                    {
                        String responseString = @"<html><head><title> Background Application Web Server </title ></head >
                                                    <body><p>Hello World! Webserver is working!<p/>
                                                    <p>You sent the following query in your HTTP GET request:" + query + 
                                                    "</p></body></html>";

                        var html = Encoding.UTF8.GetBytes(responseString);
                        using (var bodyStream = new MemoryStream(html))
                        {
                            var header = $"HTTP/1.1 200 OK\r\nContent-Length: {bodyStream.Length}\r\nConnection: close\r\n\r\n";
                            var headerArray = Encoding.UTF8.GetBytes(header);
                            await response.WriteAsync(headerArray,
                                                      0, headerArray.Length);
                            await bodyStream.CopyToAsync(response);
                            await response.FlushAsync();
                        }
                    }
                }
            };
        }

        private static string GetQuery(StringBuilder request)
        {
            var requestLines = request.ToString().Split(' ');

            var url = requestLines.Length > 1
                              ? requestLines[1] : string.Empty;

            var uri = new Uri("http://localhost" + url);
            var query = uri.Query;
            return query;
        }
    }
}
