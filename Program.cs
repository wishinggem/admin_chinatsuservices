using admin_chinatsuservices.Pages;
using Newtonsoft.Json;

namespace admin_chinatsuservices
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddHttpClient();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }
}

public static class JsonHandler
{
    public static void SerializeJsonFile<T>(string filePath, T obj, bool append = false)
    {
        using var writer = new StreamWriter(filePath, append);
        writer.Write(JsonConvert.SerializeObject(obj));
    }

    public static T DeserializeJsonFile<T>(string filePath) where T : new()
    {
        if (!System.IO.File.Exists(filePath))
            return new T();

        using var reader = new StreamReader(filePath);
        return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
    }
}