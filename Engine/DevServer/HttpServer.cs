using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PHPIL.Engine.Runtime;

namespace PHPIL.Engine.DevServer
{
    public class HttpServer
    {
        public static async Task Start(string entryPoint, string hostWithPort)
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(entryPoint));
            if (dir != null)
                Directory.SetCurrentDirectory(dir);

            var parts = hostWithPort.Split(':');
            var host = parts[0];
            var port = int.Parse(parts[1]);

            // HttpListener does not support 0.0.0.0
            if (host == "0.0.0.0")
                host = "*";

            var listener = new HttpListener();
            listener.Prefixes.Add($"http://{host}:{port}/");
            listener.Start();

            Console.WriteLine($"Dev server running at http://localhost:{port}/");

            while (listener.IsListening)
            {
                var context = await listener.GetContextAsync();
                var entryPointFileName = Path.GetFileName(entryPoint);
                _ = Task.Run(() => HandleRequest(context, entryPointFileName));
            }
        }

        private static async Task HandleRequest(HttpListenerContext context, string entryPointFileName)
        {
            // Create an isolated execution context for this request
            var executionContext = new Runtime.ExecutionContext();
            Runtime.Runtime.SetContext(executionContext);

            try
            {
                var requestPath = context.Request.Url!.AbsolutePath.TrimStart('/');
                if (string.IsNullOrEmpty(requestPath))
                    requestPath = "index.php";

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), requestPath);

                if (File.Exists(filePath) && !filePath.EndsWith(".php"))
                {
                    // Serve static file
                    var bytes = await File.ReadAllBytesAsync(filePath);
                    context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes);
                }
                else
                {
                    // Populate superglobals from HTTP request into the isolated context
                    PopulateSuperglobals(context, executionContext);
                    
                    // Execute PHP script
                    var output = ExecutePhp(entryPointFileName);
                    
                    // Send response
                    var buffer = Encoding.UTF8.GetBytes(output);
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.ContentType = "text/html";
                    await context.Response.OutputStream.WriteAsync(buffer);
                }
            }
            catch (Exception ex)
            {
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = 500;
                Console.WriteLine(ex);
                
                var errorMessage = ex.ToString();
                var errorBuffer = Encoding.UTF8.GetBytes(errorMessage);
                context.Response.ContentLength64 = errorBuffer.Length;
                await context.Response.OutputStream.WriteAsync(errorBuffer);
            }
            finally
            {
                context.Response.Close();
                
                // Clean up execution context
                Runtime.Runtime.ClearContext();
                executionContext.Dispose();
            }
        }

        // Step 4: PHP execution stub
        private static string ExecutePhp(string script)
        {
            Runtime.Runtime.ExecuteFile(script);
            return Runtime.Runtime.GetExecutionResult();
        }

        private static void PopulateSuperglobals(HttpListenerContext context, Runtime.ExecutionContext executionContext)
        {
            var request = context.Request;
            
            // Populate $_SERVER
            var serverVars = new Dictionary<object, object>
            {
                ["SERVER_NAME"] = request.Url?.Host ?? "",
                ["SERVER_PORT"] = request.Url?.Port.ToString() ?? "",
                ["REQUEST_METHOD"] = request.HttpMethod,
                ["REQUEST_URI"] = request.Url?.AbsolutePath ?? "",
                ["QUERY_STRING"] = request.Url?.Query.TrimStart('?') ?? "",
                ["REMOTE_ADDR"] = request.RemoteEndPoint?.Address.ToString() ?? "",
                ["HTTP_HOST"] = request.Headers["Host"] ?? "",
                ["HTTP_USER_AGENT"] = request.UserAgent ?? "",
                ["HTTP_ACCEPT"] = request.Headers["Accept"] ?? "",
                ["HTTP_ACCEPT_LANGUAGE"] = request.Headers["Accept-Language"] ?? "",
                ["HTTP_ACCEPT_ENCODING"] = request.Headers["Accept-Encoding"] ?? "",
                ["HTTP_CONNECTION"] = request.Headers["Connection"] ?? "",
                ["HTTP_CACHE_CONTROL"] = request.Headers["Cache-Control"] ?? "",
                ["HTTP_COOKIE"] = request.Headers["Cookie"] ?? "",
                ["CONTENT_TYPE"] = request.ContentType ?? "",
                ["CONTENT_LENGTH"] = request.ContentLength64.ToString(),
                ["SCRIPT_FILENAME"] = Path.GetFullPath(request.Url?.AbsolutePath ?? ""),
                ["SCRIPT_NAME"] = request.Url?.AbsolutePath ?? "",
                ["REQUEST_SCHEME"] = request.Url?.Scheme ?? "http",
                ["HTTPS"] = request.Url?.Scheme == "https" ? "on" : "off"
            };
            
            // Add all request headers with HTTP_ prefix
            var allKeys = request.Headers?.AllKeys;
            if (allKeys != null && request.Headers != null)
            {
                foreach (var header in allKeys)
                {
                    if (header != null)
                    {
                        var key = "HTTP_" + header.ToUpper().Replace("-", "_");
                        serverVars[key] = request.Headers[header] ?? "";
                    }
                }
            }
            
            executionContext.PopulateServer(serverVars);
            
            // Populate $_GET from query string
            var getVars = new Dictionary<object, object>();
            var queryString = request.Url?.Query.TrimStart('?') ?? "";
            if (!string.IsNullOrEmpty(queryString))
            {
                foreach (var pair in queryString.Split('&'))
                {
                    var parts = pair.Split('=');
                    if (parts.Length == 2)
                    {
                        var key = Uri.UnescapeDataString(parts[0]);
                        var value = Uri.UnescapeDataString(parts[1]);
                        getVars[key] = value;
                    }
                }
            }
            executionContext.PopulateGet(getVars);
            
            // Populate $_POST from request body
            var postVars = new Dictionary<object, object>();
            if (request.HasEntityBody && (request.HttpMethod == "POST" || request.HttpMethod == "PUT" || request.HttpMethod == "PATCH"))
            {
                try
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
                    var body = reader.ReadToEnd();
                    
                    if (request.ContentType?.StartsWith("application/x-www-form-urlencoded") == true)
                    {
                        foreach (var pair in body.Split('&'))
                        {
                            var parts = pair.Split('=');
                            if (parts.Length == 2)
                            {
                                var key = Uri.UnescapeDataString(parts[0]);
                                var value = Uri.UnescapeDataString(parts[1]);
                                postVars[key] = value;
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore parsing errors
                }
            }
            executionContext.PopulatePost(postVars);
            
            // Populate $_COOKIE from Cookie header
            var cookieVars = new Dictionary<object, object>();
            var cookieHeader = request.Headers?.Get("Cookie");
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                foreach (var pair in cookieHeader.Split(';'))
                {
                    var parts = pair.Trim().Split('=');
                    if (parts.Length >= 2)
                    {
                        var key = Uri.UnescapeDataString(parts[0]);
                        var value = Uri.UnescapeDataString(string.Join("=", parts.Skip(1)));
                        cookieVars[key] = value;
                    }
                }
            }
            executionContext.PopulateCookie(cookieVars);
            
            // Populate $_REQUEST with combined GET, POST, and COOKIE
            var requestVars = new Dictionary<object, object>();
            foreach (var kvp in getVars)
                requestVars[kvp.Key] = kvp.Value;
            foreach (var kvp in postVars)
                requestVars[kvp.Key] = kvp.Value;
            foreach (var kvp in cookieVars)
                requestVars[kvp.Key] = kvp.Value;
            executionContext.PopulateRequest(requestVars);
        }
    }
}