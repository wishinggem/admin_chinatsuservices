using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json.Serialization;

namespace admin_chinatsuservices.Pages
{
    public class ServicePannelModel : PageModel
    {
        public static readonly string rootPath = Path.Combine("wwwroot", "dashboardFiles");

        //this is stored as a Dict<id, Service> in services.json
        public static readonly string servicesPath = Path.Combine("wwwroot", "dashboardFiles", "services.json");

        private void EnsureAdminFilesExist()
        {
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }
            if (!System.IO.File.Exists(servicesPath))
            {
                List<Service> emptyList = new List<Service>();
                JsonHandler.SerializeJsonFile(servicesPath, emptyList);
                Thread.Sleep(2);
            }
        }

        public async Task OnGetAsync()
        {
            EnsureAdminFilesExist();

            try
            {
                Dictionary<string, Service> services = JsonHandler.DeserializeJsonFile<Dictionary<string, Service>>(servicesPath);

                if (services != null)
                {
                    foreach (var service in services.Values)
                    {
                        if (await SendPing(service.IP))
                        {
                            service.serviceStatus = "Online";
                        }
                        else
                        {
                            service.serviceStatus = "Offline";
                        }
                    }
                }
                else
                {
                    services = new Dictionary<string, Service>();
                }
                ViewData["Services"] = services;
            }
            catch (Exception ex)
            {
                List<Service> emptyList = new List<Service>();
                JsonHandler.SerializeJsonFile(servicesPath, emptyList);
                ViewData["Services"] = new Dictionary<string, Service>();
            }
            ViewData["IsInternalNetwork"] = IsInternalNetwork().ToString().ToLower();
        }

        private bool IsInternalNetwork()
        {
            /*var remoteIp = GetClientPublicIP();
            var serverPublicIp = GetPublicIPAddress();
            if (remoteIp == serverPublicIp || remoteIp == "::1")
            {
                return true;
            }
            else
            {
                return false;
            }*/

            //always returning true as i cannot impliement a system that will work with normal network handeling, so this will only toggle with the checkbox
            return true;
        }

        public IActionResult OnPostAddService(string serviceName, string serviceDesc, string ip, string localNetwork, string hasWebUI, string webUI)
        {
            bool localNetworkSwitch;
            bool hasWebUISwitch;
            bool canRemoteAccessSwitch;

            if (localNetwork == "on")
            {
                localNetworkSwitch = true;
            }
            else
            {
                localNetworkSwitch = false;
            }

            if (hasWebUI == "on")
            {
                hasWebUISwitch = true;
            }
            else
            {
                hasWebUISwitch = false;
            }

            /*if (canAccessOutSideNet == "on")
            {
                canRemoteAccessSwitch = true;
            }
            else
            {
                canRemoteAccessSwitch = false;
            }*/

            Dictionary<string, Service> services = null;
            try 
            {
                services = JsonHandler.DeserializeJsonFile<Dictionary<string, Service>>(servicesPath);
            }
            catch (Exception ex)
            {
                services = new Dictionary<string, Service>();
            }
            Service newService = new Service
            {
                serviceName = serviceName,
                description = serviceDesc,
                IP = ip,
                localNetwork = localNetworkSwitch,
                hasWebUI = hasWebUISwitch,
                webUI = webUI,
                canRemoteAccess = true,
                serviceStatus = "Unknown"
            };

            services.Add(Guid.NewGuid().ToString(), newService);

            JsonHandler.SerializeJsonFile(servicesPath, services);

            return RedirectToPage();
        }

        public IActionResult OnPostDeleteService(string serviceID)
        {
            Dictionary<string, Service> services = null;
            try
            {
                services = JsonHandler.DeserializeJsonFile<Dictionary<string, Service>>(servicesPath);
            }
            catch (Exception ex)
            {
                services = new Dictionary<string, Service>();
            }

            if (services.ContainsKey(serviceID))
            {
                services.Remove(serviceID);
            }

            JsonHandler.SerializeJsonFile(servicesPath, services);
            return RedirectToPage();
        }

        public IActionResult OnPostUpdateService(string serviceID, string serviceName, string serviceDesc, string ip, string localNetwork, string hasWebUI, string webUI)
        {
            bool localNetworkSwitch;
            bool hasWebUISwitch;
            bool canRemoteAccessSwitch;

            if (localNetwork == "on")
            {
                localNetworkSwitch = true;
            }
            else
            {
                localNetworkSwitch = false;
            }

            if (hasWebUI == "on")
            {
                hasWebUISwitch = true;
            }
            else
            {
                hasWebUISwitch = false;
            }

            /*if (canAccessOutSideNet == "on")
            {
                canRemoteAccessSwitch = true;
            }
            else
            {
                canRemoteAccessSwitch = false;
            }*/

            Dictionary<string, Service> services = null;
            try
            {
                services = JsonHandler.DeserializeJsonFile<Dictionary<string, Service>>(servicesPath);
            }
            catch (Exception ex)
            {
                services = new Dictionary<string, Service>();
            }

            if (services.ContainsKey(serviceID))
            {
                Service newService = new Service
                {
                    serviceName = serviceName,
                    description = serviceDesc,
                    IP = ip,
                    localNetwork = localNetworkSwitch,
                    hasWebUI = hasWebUISwitch,
                    webUI = webUI,
                    canRemoteAccess = true,
                    serviceStatus = "Unknown"
                };

                services[serviceID] = newService;
            }

            JsonHandler.SerializeJsonFile(servicesPath, services);
            return RedirectToPage();
        }

        public static async Task<bool> SendPing(string hostNameOrAddress)
        {
            using (var ping = new Ping())
            {
                try
                {
                    PingReply result = await ping.SendPingAsync(hostNameOrAddress, 500);
                    return result.Status == IPStatus.Success;
                }
                catch
                {
                    return false;
                }
            }
        }

        static string GetPublicIPAddress()
        {
            String address = "";
            WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
            using (WebResponse response = request.GetResponse())
            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
            {
                address = stream.ReadToEnd();
            }

            int first = address.IndexOf("Address: ") + 9;
            int last = address.LastIndexOf("</body>");
            address = address.Substring(first, last - first);

            return address;
        }

        private string GetClientPublicIP()
        {
            return HttpContext.Connection.RemoteIpAddress.ToString();
        }
    }
}

[Serializable]
public class Service
{
    public string serviceName;
    public string description;
    public string IP;
    public bool localNetwork;
    public bool hasWebUI;
    public bool canRemoteAccess;
    public string webUI;
    [JsonIgnore]
    public string serviceStatus;
}

[Serializable]
public class NetworkInfo
{
    public string ssid;
}
