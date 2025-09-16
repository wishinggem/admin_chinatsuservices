using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace admin_chinatsuservices.Pages
{
    public class UPSModel : PageModel
    {
        public UpsDisplayData Data { get; set; }
        public string DataAsJson { get; set; }
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            NutServerSettings settings = new NutServerSettings
            {
                Host = "192.168.0.131",
                Port = 3493,
                Username = "observer",
                Password = "3!gvYQYTqA",
                UPSName = "ups"
            };
            NutClientService nutClientService = new NutClientService(settings);
            nutClientService.jsonData = await nutClientService.GetFullDataJsonAsync();
            if (string.IsNullOrEmpty(nutClientService.jsonData) || nutClientService.jsonData == "{}")
            {
                ErrorMessage = "Failed to retrieve data from NUT server or no data was available.";
            }

            Data = new UpsDisplayData
            {
                BatteryCharge = double.Parse(nutClientService.GetValueFromJson("battery.charge")),
                BatteryStatus = nutClientService.GetValueFromJson("battery.charger.status"),
                UpsLoad = double.Parse(nutClientService.GetValueFromJson("ups.load")),
                BatteryRuntimeMinutes = double.Parse(nutClientService.GetValueFromJson("battery.runtime")),
                InputVoltage = double.Parse(nutClientService.GetValueFromJson("input.voltage")),
                OutputVoltage = double.Parse(nutClientService.GetValueFromJson("output.voltage"))
            };

            DataAsJson = JsonSerializer.Serialize(Data);
        }
    }
}

public class UpsDisplayData
{
    public double BatteryCharge { get; set; }
    public string BatteryStatus { get; set; }
    public double UpsLoad { get; set; }
    public double BatteryRuntimeMinutes { get; set; }
    public double InputVoltage { get; set; }
    public double OutputVoltage { get; set; }
}

public class NutClientService
{
    private readonly NutServerSettings _settings;

    public string jsonData = "";

    public NutClientService(NutServerSettings settings)
    {
        _settings = settings;
    }

    public async Task<string> GetFullDataJsonAsync()
    {
        var allData = await GetFullDataAsync();
        if (allData == null)
        {
            return "{}"; // Return empty JSON on failure
        }
        return JsonSerializer.Serialize(allData, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<Dictionary<string, string>> GetFullDataAsync()
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port);
            await using var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.ASCII);
            var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

            // 1. Authenticate
            await writer.WriteLineAsync($"USERNAME {_settings.Username}");
            await writer.WriteLineAsync($"PASSWORD {_settings.Password}");

            var authResponse = await reader.ReadLineAsync();
            if (authResponse == null || !authResponse.StartsWith("OK"))
            {
                // Handle authentication failure
                return null;
            }

            // 2. Request a list of all variables
            await writer.WriteLineAsync($"LIST VAR {_settings.UPSName}");
            var data = new Dictionary<string, string>();
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("END LIST VAR"))
                {
                    break;
                }

                // Example line: VAR your_ups_name battery.charge "98.7"
                var parts = line.Split(new[] { ' ' }, 4);
                if (parts.Length >= 4)
                {
                    string key = parts[2];
                    string value = parts[3].Trim('"'); // Remove quotes from the value
                    data[key] = value;
                }
            }
            return data;
        }
        catch (Exception ex)
        {
            // Log exception
            return null;
        }
    }

    public string GetValueFromJson(string key)
    {
        if (string.IsNullOrEmpty(jsonData) || string.IsNullOrEmpty(key))
        {
            return null;
        }

        try
        {
            // Parse the JSON string into a JsonDocument
            using JsonDocument doc = JsonDocument.Parse(jsonData);

            // Try to get the root element
            JsonElement root = doc.RootElement;

            // If the key is found, return its string value.
            if (root.TryGetProperty(key, out JsonElement element))
            {
                return element.ToString();
            }
        }
        catch (JsonException)
        {
            // Handle cases where the input string is not valid JSON
            return null;
        }

        return null;
    }
}

public class NutServerSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string UPSName { get; set; }
}

public class UpsData
{
    public string Status { get; set; }
    public double BatteryCharge { get; set; }
    // Add other relevant properties
}


//nut json data will look as follows
/*
 * {
  "VAR": "ups",
  "battery.capacity": "9.00",
  "battery.charge": "100",
  "battery.charge.low": "30",
  "battery.charge.restart": "0",
  "battery.charger.status": "floating",
  "battery.energysave": "no",
  "battery.runtime": "3180",
  "battery.runtime.low": "180",
  "battery.type": "PbAc",
  "device.mfr": "DELL",
  "device.model": "Dell UPS Rack 1000W HV",
  "device.serial": "CN-0J718N-75162-33M-0041-A08",
  "device.type": "ups",
  "driver.name": "usbhid-ups",
  "driver.parameter.pollfreq": "30",
  "driver.parameter.pollinterval": "2",
  "driver.parameter.port": "auto",
  "driver.parameter.productid": "ffff",
  "driver.parameter.synchronous": "no",
  "driver.parameter.vendorid": "047c",
  "driver.version": "2.7.4",
  "driver.version.data": "MGE HID 1.40",
  "driver.version.internal": "0.41",
  "input.frequency": "49.8",
  "input.frequency.nominal": "50",
  "input.transfer.boost.low": "210",
  "input.transfer.high": "286",
  "input.transfer.low": "160",
  "input.transfer.trim.high": "250",
  "input.voltage": "245.0",
  "input.voltage.nominal": "230",
  "outlet.1.autoswitch.charge.low": "0",
  "outlet.1.delay.shutdown": "2147483647",
  "outlet.1.delay.start": "2",
  "outlet.1.desc": "PowerShare Outlet 1",
  "outlet.1.id": "1",
  "outlet.1.status": "on",
  "outlet.1.switchable": "yes",
  "outlet.2.autoswitch.charge.low": "0",
  "outlet.2.delay.shutdown": "2147483647",
  "outlet.2.delay.start": "3",
  "outlet.2.desc": "PowerShare Outlet 2",
  "outlet.2.id": "2",
  "outlet.2.status": "on",
  "outlet.2.switchable": "yes",
  "outlet.desc": "Main Outlet",
  "outlet.id": "0",
  "outlet.switchable": "no",
  "output.current": "0.80",
  "output.frequency": "49.8",
  "output.frequency.nominal": "50",
  "output.voltage": "247.0",
  "output.voltage.nominal": "230",
  "ups.beeper.status": "enabled",
  "ups.date": "2018/09/21",
  "ups.delay.shutdown": "20",
  "ups.delay.start": "30",
  "ups.firmware": "01.14.0003",
  "ups.load": "21",
  "ups.load.high": "110",
  "ups.mfr": "DELL",
  "ups.model": "Dell UPS Rack 1000W HV",
  "ups.power": "211",
  "ups.power.nominal": "1000",
  "ups.productid": "ffff",
  "ups.realpower": "163",
  "ups.realpower.nominal": "1000",
  "ups.serial": "CN-0J718N-75162-33M-0041-A08",
  "ups.shutdown": "enabled",
  "ups.start.battery": "yes",
  "ups.start.reboot": "yes",
  "ups.status": "OL CHRG",
  "ups.test.interval": "7776000",
  "ups.test.result": "No test initiated",
  "ups.time": "19:29:28",
  "ups.timer.shutdown": "0",
  "ups.timer.start": "0",
  "ups.type": "offline / line interactive",
  "ups.vendorid": "047c"
}
*/