using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
                    // Step 3: Route to entry point
                    var output = ExecutePhp(entryPointFileName);
                    
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
            }
        }

        // Step 4: PHP execution stub
        private static string ExecutePhp(string script)
        {
            Runtime.Runtime.ExecuteFile(script);
            return Runtime.Runtime.GetExecutionResult();
        }
    }
}