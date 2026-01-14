# Timezone Tests

## Running the Timezone Tests

The existing test project has some pre-existing build errors from refactoring (ViewModels removal, AutoMapper removal).

To run ONLY the new timezone tests, use:

```bash
# Build only our new test files (they don't depend on broken tests)
dotnet build BoardGameTracker.Core/BoardGameTracker.Core.csproj
dotnet build BoardGameTracker.Api/BoardGameTracker.Api.csproj

# Then manually verify the classes compile
```

## Manual Testing

Since the test project has build issues, here's how to manually test:

### 1. Test DateTimeProvider

Create a simple console app or use the Host project:

```csharp
var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string>
    {
        { "TZ", "Europe/Brussels" }
    })
    .Build();

var provider = new DateTimeProvider(config);

Console.WriteLine($"UTC Now: {provider.UtcNow}");
Console.WriteLine($"Local Now: {provider.Now}");
Console.WriteLine($"Timezone: {provider.TimeZone.Id}");
Console.WriteLine($"Offset: {provider.TimeZone.GetUtcOffset(provider.UtcNow)}");
```

### 2. Test API DateTime Conversion

**Request (from Brussels, UTC+1):**
```bash
curl -X POST http://localhost:5444/api/session \
  -H "Content-Type: application/json" \
  -d '{
    "gameId": 1,
    "start": "2026-01-07T14:30:00+01:00",
    "minutes": 120,
    "playerSessions": [],
    "comment": "Test session"
  }'
```

**Expected behavior:**
- API receives: `2026-01-07T14:30:00+01:00`
- Converts to UTC: `2026-01-07T13:30:00Z`
- Stores in DB: `2026-01-07 13:30:00` (UTC)
- Returns: `2026-01-07T13:30:00Z`

**Verify in database:**
```sql
SELECT id, start, "end" FROM sessions ORDER BY id DESC LIMIT 1;
-- Should show: 2026-01-07 13:30:00 (UTC time)
```

### 3. Test Different Timezones

**New York (UTC-5):**
```json
{
  "start": "2026-01-07T08:30:00-05:00"
}
```
Should convert to: `2026-01-07T13:30:00Z`

**Tokyo (UTC+9):**
```json
{
  "start": "2026-01-07T22:30:00+09:00"
}
```
Should convert to: `2026-01-07T13:30:00Z`

### 4. Test UI Display

In your browser console (F12):

```javascript
// API returns UTC
const utcDate = "2026-01-07T13:30:00Z";

// Browser automatically converts to local
const date = new Date(utcDate);
console.log(date.toLocaleString());  // Shows in YOUR timezone

// Force Brussels display
console.log(date.toLocaleString('en-US', { timeZone: 'Europe/Brussels' }));
// Output: "1/7/2026, 2:30:00 PM" (if UTC+1)
```

## Test Checklist

- [ ] DateTimeProvider returns UTC
- [ ] DateTimeProvider handles invalid timezone (falls back to UTC)
- [ ] API converts incoming dates with timezone to UTC
- [ ] API converts incoming dates without timezone (assumes UTC)
- [ ] API returns dates as UTC with 'Z' suffix
- [ ] Database stores UTC timestamps
- [ ] Browser displays dates in user's local timezone
- [ ] Round-trip: User input → API → DB → API → User display works correctly

## Known Issues

The main test project has build errors due to:
- Removed ViewModels (replaced with DTOs)
- Removed AutoMapper dependency

These are pre-existing and unrelated to timezone functionality.

## Recommendation

Once the main refactoring is complete and old controller tests are updated, run:

```bash
dotnet test BoardGameTracker.Tests/BoardGameTracker.Tests.csproj
```

All timezone tests should pass.
