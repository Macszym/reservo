using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiDemo
{
    class Program
    {
        // Zmień te wartości zgodnie z Twoim środowiskiem
        private static readonly string BaseUrl = "http://localhost:5106"; // zmienione na HTTP
        private static readonly string AdminApiKey = "evDS91xtcl+59IPwSZt7A4OvyyLjX/KFQQp4cZjswZA="; // Wstaw klucz API admina po uruchomieniu aplikacji
        private static readonly string UserApiKey = "xTu1YmYqeVQwHQc6bGRMdmO2LKRmmJJ5CbS8Wfcy/Xw=";  // Wstaw klucz API użytkownika po uruchomieniu aplikacji
        
        private static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== DEMO REST API systemu Reservo ===\n");
            
            // Wyłączenie walidacji certyfikatu SSL dla localhost (tylko development!)
            HttpClientHandler handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            using var httpClient = new HttpClient(handler);
            
            Console.WriteLine("UWAGA: Przed uruchomieniem demo:");
            Console.WriteLine("1. Uruchom aplikację Reservo");
            Console.WriteLine("2. Zaloguj się jako admin/user");
            Console.WriteLine("3. Skopiuj klucze API z panelu administracyjnego");
            Console.WriteLine("4. Wklej je do kodu w zmiennych AdminApiKey i UserApiKey\n");
            
            if (string.IsNullOrEmpty(AdminApiKey))
            {
                Console.WriteLine("Błąd: Nie ustawiono kluczy API. Edytuj kod i wstaw prawidłowe klucze API.");
                Console.WriteLine("Klucze można znaleźć w aplikacji w sekcji 'Zarządzaj użytkownikami'");
                return;
            }

            try
            {
                // Test 1: Pobieranie zasobów (jako admin)
                await TestGetResources(httpClient, AdminApiKey);
                
                // Test 2: Dodawanie zasobu (jako admin)
                await TestCreateResource(httpClient, AdminApiKey);
                
                // Test 3: Pobieranie zasobów (jako user)
                await TestGetResources(httpClient, UserApiKey);
                
                // Test 4: Tworzenie rezerwacji (jako user)
                await TestCreateReservation(httpClient, UserApiKey);
                
                // Test 5: Pobieranie rezerwacji (jako user)
                await TestGetReservations(httpClient, UserApiKey);
                
                // Test 6: Sprawdzenie dostępności zasobu
                await TestResourceAvailability(httpClient, UserApiKey, 1);
                
                // Test 7: Próba utworzenia zasobu jako user (powinien być błąd)
                await TestUnauthorizedCreateResource(httpClient, UserApiKey);
                
                Console.WriteLine("\n=== DEMO ZAKOŃCZONE ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas wykonywania demo: {ex.Message}");
                Console.WriteLine("Upewnij się, że aplikacja Reservo jest uruchomiona i dostępna.");
            }
        }

        static async Task TestGetResources(HttpClient client, string apiKey)
        {
            Console.WriteLine("--- Test: Pobieranie listy zasobów ---");
            
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/api/Resources");
            request.Headers.Add("X-API-Key", apiKey);
            
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Odpowiedź: {content}\n");
        }

        static async Task TestCreateResource(HttpClient client, string apiKey)
        {
            Console.WriteLine("--- Test: Tworzenie nowego zasobu (jako admin) ---");
            
            var newResource = new
            {
                Name = "Sala testowa API",
                Description = "Sala utworzona przez REST API",
                Location = "Budynek Test, piętro 1",
                IsAvailable = true,
                MaxReservationHours = 8,
                CategoryId = 1
            };
            
            var json = JsonSerializer.Serialize(newResource);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/Resources");
            request.Headers.Add("X-API-Key", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Odpowiedź: {content}\n");
        }

        static async Task TestCreateReservation(HttpClient client, string apiKey)
        {
            Console.WriteLine("--- Test: Tworzenie rezerwacji ---");
            
            var newReservation = new
            {
                ResourceId = 1,
                StartDate = DateTime.Now.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                EndDate = DateTime.Now.AddHours(3).ToString("yyyy-MM-ddTHH:mm:ss"),
                Purpose = "Spotkanie zespołu - test API"
            };
            
            var json = JsonSerializer.Serialize(newReservation);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/Reservations");
            request.Headers.Add("X-API-Key", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Odpowiedź: {content}\n");
        }

        static async Task TestGetReservations(HttpClient client, string apiKey)
        {
            Console.WriteLine("--- Test: Pobieranie rezerwacji ---");
            
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/api/Reservations");
            request.Headers.Add("X-API-Key", apiKey);
            
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Odpowiedź: {content}\n");
        }

        static async Task TestResourceAvailability(HttpClient client, string apiKey, int resourceId)
        {
            Console.WriteLine($"--- Test: Sprawdzanie dostępności zasobu {resourceId} ---");
            
            var startDate = DateTime.Today.ToString("yyyy-MM-dd");
            var endDate = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");
            
            var request = new HttpRequestMessage(HttpMethod.Get, 
                $"{BaseUrl}/api/Resources/{resourceId}/availability?startDate={startDate}&endDate={endDate}");
            request.Headers.Add("X-API-Key", apiKey);
            
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Odpowiedź: {content}\n");
        }

        static async Task TestUnauthorizedCreateResource(HttpClient client, string apiKey)
        {
            Console.WriteLine("--- Test: Próba utworzenia zasobu jako user (oczekiwany błąd) ---");
            
            var newResource = new
            {
                Name = "Nieuprawniony zasób",
                Description = "Ten zasób nie powinien zostać utworzony"
            };
            
            var json = JsonSerializer.Serialize(newResource);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/Resources");
            request.Headers.Add("X-API-Key", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Odpowiedź: {content}\n");
        }
    }
}
