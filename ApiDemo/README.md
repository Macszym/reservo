# Demo programu REST API dla systemu Reservo

Ten program konsolowy demonstruje działanie REST API systemu rezerwacji zasobów Reservo.

## Wymagania

1. Uruchomiona aplikacja Reservo na localhost
2. .NET 9.0 lub nowszy
3. Klucze API dla użytkowników admin i user

## Jak uruchomić demo

### 1. Przygotowanie kluczy API

1. Uruchom aplikację Reservo:
   ```bash
   cd ../MVC_Wynajem
   dotnet run
   ```

2. Otwórz przeglądarkę i przejdź do aplikacji (np. https://localhost:5106)

3. Zaloguj się jako administrator (login: `admin`, hasło: `admin`)

4. Przejdź do sekcji "Zarządzaj użytkownikami"

5. Skopiuj klucze API dla użytkowników `admin` i `user`

### 2. Konfiguracja demo

1. Otwórz plik `Program.cs`

2. Wklej skopiowane klucze API w zmiennych:
   ```csharp
   private static readonly string AdminApiKey = "TUTAJ_KLUCZ_ADMINA";
   private static readonly string UserApiKey = "TUTAJ_KLUCZ_USERA";
   ```

3. Sprawdź adres URL aplikacji w zmiennej `BaseUrl` (domyślnie https://localhost:5106)

### 3. Uruchomienie demo

```bash
dotnet run
```

## Co testuje demo

Program demonstracyjny wykonuje następujące operacje:

1. **Pobieranie listy zasobów (jako admin)** - GET /api/Resources
2. **Tworzenie nowego zasobu (jako admin)** - POST /api/Resources
3. **Pobieranie listy zasobów (jako user)** - GET /api/Resources
4. **Tworzenie rezerwacji (jako user)** - POST /api/Reservations
5. **Pobieranie rezerwacji (jako user)** - GET /api/Reservations
6. **Sprawdzanie dostępności zasobu** - GET /api/Resources/{id}/availability
7. **Próba utworzenia zasobu jako user** - POST /api/Resources (oczekiwany błąd 403)

## Struktura odpowiedzi API

### Zasoby (Resources)
```json
{
  "id": 1,
  "name": "Sala A1",
  "description": "Sala konferencyjna na 20 osób",
  "location": "Budynek A, piętro 1",
  "isAvailable": true,
  "maxReservationHours": 24,
  "createdAt": "2023-12-01T10:00:00",
  "categoryId": 1,
  "categoryName": "Sale konferencyjne",
  "categoryColor": "#007bff"
}
```

### Rezerwacje (Reservations)
```json
{
  "id": 1,
  "startDate": "2023-12-01T14:00:00",
  "endDate": "2023-12-01T16:00:00",
  "purpose": "Spotkanie zespołu",
  "status": "Active",
  "createdAt": "2023-12-01T10:00:00",
  "userId": 2,
  "username": "user",
  "resourceId": 1,
  "resourceName": "Sala A1",
  "resourceLocation": "Budynek A, piętro 1"
}
```

## Autoryzacja

API używa kluczy API przekazywanych w nagłówku `X-API-Key`. 

- **Admin** - pełny dostęp do wszystkich operacji
- **User** - może tworzyć i zarządzać swoimi rezerwacjami, przeglądać zasoby

## Błędy

- **401 Unauthorized** - brak lub nieprawidłowy klucz API
- **403 Forbidden** - brak uprawnień do wykonania operacji
- **404 Not Found** - zasób nie został znaleziony
- **400 Bad Request** - nieprawidłowe dane w żądaniu

## Dokumentacja API

Pełna dokumentacja API jest dostępna w interfejsie Swagger pod adresem:
https://localhost:5106/api-docs (gdy aplikacja jest uruchomiona)
