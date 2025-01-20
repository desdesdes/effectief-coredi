# Na sheet 8: Eerste stappen Core DI

Basis implementatie van .net core DI
```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<StorageSettings>(provider =>
    JsonSerializer.Deserialize<StorageSettings>(
    File.ReadAllText("appsettings.json"))!);

builder.Services.AddSingleton<ILogger, ConsoleLogger>();
builder.Services.AddSingleton<PersonBC>();

var host = builder.Build();

var bc = host.Services.GetRequiredService<PersonBC>();
...
await host.StopAsync();
```

# Na sheet 12: PersonBC & CrmValidations unit tests

Nu kunnen de de unit test gaan schrijven, gebruik vs2022 `Create Unit Tests` optie om snel een class te genereren.

Genereer een unit test file op `PersonBC`.

```csharp
var bc = new PersonBC(new StorageSettings());
```
Vreemd `PersonBC` heeft helemaal geen afhnkelijkheid van `StorageSettings`, maar van `Repository`, laten we dat aanpassen.

```csharp
public PersonBC(Repository repository, ILogger? logger = null)
{
_repository = repository;
_logger = logger;
}
```

in `program.cs`:
```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<Repository>(provider =>
    Repository.CreateRepository(JsonSerializer.Deserialize<StorageSettings>(
    File.ReadAllText("appsettings.json"))!));

builder.Services.AddSingleton<ILogger, ConsoleLogger>();
builder.Services.AddSingleton<PersonBC>();

var host = builder.Build();
```

Tip: Op `Host` en `WebApplication` vind je meerdere `Create` methodes, veel zijn deprecated. `Host.CreateApplicationBuilder()` of `WebApplication.CreateBuilder()` zijn de geadviseerde functies, zie [hier](https://github.com/dotnet/runtime/discussions/81090).

Pas de test als volgt aan:
```csharp
[Test()]
public void AddPerson_FirstNameStartWithSpace_ThrowsException()
{
  var bc = new PersonBC(A.Fake<Repository>());

  Assert.ThrowsAsync<Exception>(() => bc.AddPerson(new Person() { Id = Guid.NewGuid(), FirstName = "Bart", LastName = "Vries" }));
}
```
We gebruiken hier `FakeItEasy` om een `Repository` te faken. Dit is een library die het makkelijk maakt om objecten te faken. We kunnen nu de test schrijven zonder dat we een echte `Repository` nodig hebben.
Aangezien de ILogger optional was kunnen we deze weglaten.

Run de test en laat zien dat hij niet werkt. Pas ook de input aan naar `" Bart"` en laat zien dat hij dan wel werkt.

We voegen nu ook een test toe die controleert of de `AddPerson` methode van `PersonBC` de `Add` methode van `Repository` aanroept.
```csharp
[Test()]
public void AddPerson_WithProperData_Succeeds()
{
  var testRepository = A.Fake<Repository>();

  var bc = new PersonBC(testRepository);

  Assert.DoesNotThrowAsync(() => bc.AddPerson(new Person() { Id = Guid.NewGuid(), FirstName = "Bart", LastName = "Vries" }));
  A.CallTo(() => testRepository.Add<Person>(A<Person>._)).MustHaveHappenedOnceExactly();
}
```

Run de test en laat zien dat hij werkt. We gaan nu unit test voor `CrmValidations` schrijven, gebruik vs2022 `Create Unit Tests` optie om snel een class te genereren.

Helaas geeft deze een foutmelding. We kunnen alleen publics testen. We willen de class  niet public maken omdat andere dan door andere kan worden aangeroepen. We kunnen de class wel internal maken/houden en de assembly zichtbaar maken voor de test assembly. Dit doen we door in de `AssemblyInfo.cs` van de `CrmValidations` project de volgende regel toe te voegen:
`[assembly: InternalsVisibleTo("Afas.Bvr.Core.Tests")]`

Nu kun je wel de de volgende test toevoegen. 

```csharp
[TestFixture()]
public class CrmValidationsTests
{
  [TestCase(null, true)]
  [TestCase("", false)]
  [TestCase(" ", false)]
  [TestCase("😉", false)]
  [TestCase("\r", false)]
  [TestCase("\n", false)]
  [TestCase("\n\r", false)]
  [TestCase("(033) 43 41 800", true)]
  [TestCase("(033) 43 41 800 ", false)]
  [TestCase(" (033) 43 41 800", false)]
  public void ValidatePhoneNumberTests(string? input, bool success)
  {
    if(success)
    {
      Assert.DoesNotThrow(() => CrmValidations.ValidatePhoneNumber(input));

    }
    else
    {
      Assert.Throws<Exception>(() => CrmValidations.ValidatePhoneNumber(input));
    }
  }
}
```

# Na sheet 14: Repository unit tests

Creeer een unit test op `MSSqlRepository`.

```csharp
[TestFixture()]
[Property("Dependency", "MSSql")] // Let op, dit helpt om snel tests te filteren, omdat deze een sql dependency hebben
public class MSSqlRepositoryTests
{
  private const string _connectionString = "Server=.\\profitsqldev;Database=codedidemo;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";

  class Demo : RepositoryObjectWithGuidId
  {
    public string? Name { get; set; }
  }

  [Test()]
  public async Task GetOrDefault_WithoutItem_ReturnsNull()
  {
    var repo = new MSSqlRepository(_connectionString);

    var result = await repo.GetOrDefault<Demo>(Guid.NewGuid());
    Assert.That(result, Is.Null);
  }

  [Test()]
  public async Task Add_RunsWithoutFailure_HasItemInDB()
  {
    var repo = new MSSqlRepository(_connectionString);

    var id = Guid.NewGuid();

    var demo = new Demo { Id = id, Name = "Test" };
    await repo.Add<Demo>(demo);

    var result = await repo.GetOrDefault<Demo>(id);
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Id, Is.EqualTo(id));
    Assert.That(result.Name, Is.EqualTo("Test"));
  }
}
```

Is dit nu een unit test? Uiteindelijk is die vraag niet belangrijk, wel is belangrijk dat we hem zo klein en snel mogelijk hebben gemaakt en dat we deze tests maken.

Het zou mooi zijn als we de tests ook kunnen hergebruiken voor `AzureStorageTableRepository`. Dus dat gaan we doen!

pas aan 

```csharp
[TestFixture()]
[Property("Dependency", "MSSql")]
public class MSSqlRepositoryTests
```

naar 

```csharp
[TestFixture()]
[Property("Dependency", "MSSql")] // Let op, dit helpt om snel tests te filteren, omdat deze een sql dependency hebben
public class MSSqlRepositoryTests : RepositoryTests
{
  private readonly string _connectionString = "Server=.\\profitsqldev;Database=codedidemo;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";

  public override Repository CreateRepository()
  {
    return new MSSqlRepository(_connectionString);
  }
}

public abstract class RepositoryTests
{
  public abstract Repository CreateRepository();
```

Vervang nu de regels met `new MSSqlRepository(_connectionString)` naar `CreateRepository()`. Het testen werkt weer.

Nu kunnen we snel en simpel de `AzureStorageTableRepository` tests toevoegen.

```csharp
[TestFixture()]
[Property("Dependency", "AzureStorageTable")]
public class AzureStorageTableRepositoryTests : RepositoryTests
{
  public override Repository CreateRepository() => new AzureStorageTableRepository(@"https://codedidemo.table.core.windows.net/", @"sv=2022-11-02&ss=t&srt=sco&sp=rwdlacu&se=2028-12-11T23:55:39Z&st=2024-12-11T15:55:39Z&spr=https&sig=e684bQmmbwMXysmGBlbIlA4h365DFVDlJa1nVVeINOk%3D");
}
```

Draai de tests, we hebben direct een bug te pakken.

Verander `var entity = await tableClient.GetEntityAsync<TableEntity>(id.ToString(), id.ToString());` naar `var entity = await tableClient.GetEntityIfExistsAsync<TableEntity>(id.ToString(), id.ToString());`.

Run de tests opnieuw. Alles is gefixed.

# Na sheet 16: Configuration in Core DI

Let op: zoomen kan met WIN + '+' en zoom afsluiten met WIN + ESC.

Applicaties zullen niet zomaar wisselen tussen providers. Zo willen we in de Console app eigenlijk alleen de `AzureStorageTableRepository` implemeteren.  De complete `StorageSettings` willen we ook laten vervallen en een betere oplossing creeren.

1. Verwijder de `CreateRepository` functie.
2. Rename `StorageSettings` naar `AzureStorageTableSettings`.
3. Verwijder `StorageType` en `MsSqlConnectionString`.
4. Rename `AzureStorageTableEndpoint` naar `Endpoint`.
5. Rename `AzureStorageTableSasSignature` naar `SasSignature`.
6. Pass `appsettings.json` aan met dezelfde aanpassing.
6. Pas de constuctor van `AzureStorageTableRepository` aan naar `public AzureStorageTableRepository(AzureStorageTableSettings settings)` en maak de functie compilerend.
7. Pas de `Program.cs` aan naar:

```csharp
static async Task Main(string[] args)
{
  var builder = Host.CreateApplicationBuilder(args);

  builder.Services.AddSingleton<Repository>(provider =>
      new AzureStorageTableRepository(JsonSerializer.Deserialize<AzureStorageTableSettings>(
      File.ReadAllText("appsettings.json"))!));
```

De code compileerd weer en werkt weer. 

Hoover over `CreateApplicationBuilder` in `Program.cs`. Je ziet dat IConfiguration geladen worden word vanuit `appsettings.json`. We kunnen hier simpel gebruik van maken. 

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<AzureStorageTableSettings>(builder.Configuration);
builder.Services.AddSingleton<Repository, AzureStorageTableRepository>();
```

Run de code. Deze geeft nu een fout over de `AzureStorageTableSettings`. Dat komt doordat Core DI configuration met marker interfaces werkt.

Pas de constructor aan `AzureStorageTableRepository` naar:

```csharp
public AzureStorageTableRepository(IOptions<AzureStorageTableSettings> settings)
{
  _serviceClient = new TableServiceClient(
    new Uri(settings.Value.Endpoint),
    new AzureSasCredential(settings.Value.SasSignature));
}
```

Run de code. Deze werkt weer.
Ga naar de `Launch Profile` van het project en geef bij de `Command line arguments` in `/Endpoint="https://bvr.nl"`.
Run de code. De code gaat fout omdat de command line argument de appsettings heeft overschreven.
Verwijder de command line argument.
Open appsettings.json. We zien dat hierin een `SasSignature` staat. Dat is een geheim en willen we niet in de `appsettings.json` hebben staan, omdat deze in git kan komen of we kunnen hem per ongeluk aan iemand geven als we de bin map delen. Microsoft heeft een speciale oplossing voor developers.

Knip de `SasSignature` regel uit appsettings.
Druk op `Manage User Secrets` in de rechtermuisknop van het project.
Plak de `SasSignature` regel in de `secrets.json` file.
Run de code. Helaas werkt dit niet standaard in console apps. In asp.net core apps werkt het standaard wel. We moeten in de console app even [instellingen](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-9.0) toevoegen, zodat .net weet dat dit een development omgeving is.
Ga naar de `Launch Profile` van het project en geef bij de `Environment variabele` in `DOTNET_ENVIRONMENT=Development`.
Run de code.

Alleen de unit test compileerd niet meer, pas deze aan naar:
```csharp
public override Repository CreateRepository()
{
  var set = Options.Create<AzureStorageTableSettings>(Options.Create(new AzureStorageTableSettings() {
    Endpoint = @"https://codedidemo.table.core.windows.net/",
    SasSignature = @"sv=2022-11-02&ss=t&srt=sco&sp=rwdlacu&se=2028-12-11T23:55:39Z&st=2024-12-11T15:55:39Z&spr=https&sig=e684bQmmbwMXysmGBlbIlA4h365DFVDlJa1nVVeINOk%3D"
  });

  return new AzureStorageTableRepository(set);
}
```

# Na sheet 20: Logging in Core DI

Verwijder de gehele `Logging` map.
Pas `PersonBC` aan:

```csharp
public class PersonBC
{
  private readonly Repository _repository;
  private readonly ILogger? _logger;

  public PersonBC(Repository repository, ILogger<PersonBC>? logger = null)
```

Run de code. Deze werkt weer. Wat zijn de niet ingesprongen console regels? Dat zijn handmatig geschreven regels.

Hoover weer over `CreateApplicationBuilder` in `Program.cs`. Je ziet dat er een `ILogger` wordt toegevoegd die logd naar console, debug en eventlog.
Deze is ook standaard via IConfiguration te configureren. Pas `AppSettings.json` aan:

```json
{
  "Endpoint": "https://codedidemo.table.core.windows.net/",
  "Logging": {
    "LogLevel": {
      "Default": "Critical",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

Run de code. 

Kijk even naar de methodes op de ILogger. Er is zelfs een source generator voor betere logging:

```csharp
internal static partial class ILoggerExtensions
{
  [LoggerMessage(Message = "Person added {id}", Level = LogLevel.Critical)]
  public static partial void LogAddPerson(this ILogger logger, Guid id);
}
```

Pas nu de aanroep op `_logger?.LogInformation` aan naar `_logger.LogAddPerson(id);`.

Tip: Naak de logger niet nullable. Het is beter om een `NullLogger.Instance` te gebruiken. 
Pas de constructor van `PersonBC` aan naar `public PersonBC(Repository repository, ILogger<PersonBC> logger)`

Compileer de code. De unit test gaan nu fout. Pas deze aan naar `NullLogger<PersonBC>.Instance`.

Denk eraan dat je ook de logging zou kunnen testen in unit test.


Je kan [hier](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging-library-authors) meer lezen over logging.

# Na sheet 25: Overgeslagen

## Extension methodes op IServiceFactory

Kijk naar de code in `Program.cs`. Hoe moet weten dat we `AddSingleton` moeten gebruiken om `AzureStorageTableRepository` te registreren? En hoe weet je dat je dan ook `AzureStorageTableSettings` nodig hebt?
We kunnen dit makkelijker maken door een extension methode te maken op `IServiceCollection`.

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Afas.Bvr.Core.Repository;

public static class IServiceCollectionExtensions
{
  public static IServiceCollection AddAzureStorageTableRepository(this IServiceCollection services, IConfiguration namedConfigurationSection)
  {
    services.Configure<AzureStorageTableSettings>(namedConfigurationSection);
    services.AddSingleton<Repository, AzureStorageTableRepository>();
    return services;
  }
}
```

Let op: je hebt een package reference naar `Microsoft.Extensions.Options.ConfigurationExtensions` nodig.

Je kan [hier](https://learn.microsoft.com/en-us/dotnet/core/extensions/options-library-authors) meer lezen over options.

## TimeProvider

Een persoon kan alleen in het verleden geboren zijn. We vogen hiervoor een controle toe in `PersonBC`.

```csharp
if(person.BirthDate.HasValue && person.BirthDate.Value.ToDateTime(TimeOnly.MinValue) > DateTime.Now)
{
  throw new Exception("BirthDate cannot be in the future.");
}
```

Als we dit gaan unit testen is het echter niet meer deterministisch. Op een andere tijd geeft hij andere output. We kunnen dit oplossen door een `ITimeProvider` te gebruiken.

```csharp
public PersonBC(Repository repository, ILogger<PersonBC> logger, TimeProvider timeProvider)
{
  _repository = repository;
  _logger = logger;
  _timeProvider = timeProvider;
}

if(person.BirthDate.HasValue && person.BirthDate.Value.ToDateTime(TimeOnly.MinValue) > _timeProvider.GetLocalNow())
{
  throw new Exception("BirthDate cannot be in the future.");
}
```

Registreer de TimeProvider ook in `Program.cs`:

```csharp
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
```

Nu kunnen we wel een unit test toevoegen aan `PersonBCTests`.

```csharp
private FakeTimeProvider CreateTimeProvider()
{
  return new FakeTimeProvider(new DateTimeOffset(2024, 12, 19, 10, 15, 32, new TimeSpan(1, 0, 0)));
}

[Test()]
public void AddPerson_WithBirthDateInFuture_ThrowExceptions()
{
  var bc = new PersonBC(A.Fake<Repository>(), NullLogger<PersonBC>.Instance, CreateTimeProvider());

  Assert.ThrowsAsync<Exception>(() => bc.AddPerson(new Person() { Id = Guid.NewGuid(), FirstName = "Bart", LastName = "Vries", BirthDate = new DateOnly(2025, 12, 19) }));
}

[Test()]
public void AddPerson_WithBirthDateInPast_Succeeds()
{
  var testRepository = A.Fake<Repository>();
  var bc = new PersonBC(testRepository, NullLogger<PersonBC>.Instance, CreateTimeProvider());

  Assert.DoesNotThrowAsync(() => bc.AddPerson(new Person() { Id = Guid.NewGuid(), FirstName = "Bart", LastName = "Vries", BirthDate=new DateOnly(2023,12,19) }));
  A.CallTo(() => testRepository.Add(A<Person>._)).MustHaveHappenedOnceExactly();
}
```

Let op: je hebt een package reference naar `Microsoft.Extensions.TimeProvider.Testing` nodig.


Build de code en fix de laatste fouten. Draai de tests.

Tip:
Je ziet in `PersonBCTests` dat je voor veel tests een basis werkende persoon entititeit nodig hebt. Je zou hier een helper functie voor kunnenn maken.


```csharp
private Person CreatePerson(Action<Person>? changes = null)
{
  var p = new Person() { Id = Guid.NewGuid(), FirstName = "Bart", LastName = "Vries" };
  changes?.Invoke(p);
  return p;
}
```

Je kunt nu refactoren zodat je niet overal `new Person()` hoeft te schrijven.

## HttpClient 

We willen een externe API aanroepen om het telefoonnummer te valideren. We kunnen dit doen met `HttpClient`. Het is te adviseren deze te registreren in de DI container. Het is best practices om `HttpClient` te hergebruiken om SocketException te voorkomen.

```csharp
public PersonBC(Repository repository, ILogger<PersonBC> logger, TimeProvider timeProvider, HttpClient httpClient)
{
  _repository = repository;
  _logger = logger;
  _timeProvider = timeProvider;
  _httpClient = httpClient;
}
```

Voeg een controle toe in `public async Task AddPerson(Person person)`

```csharp
if(!string.IsNullOrEmpty(person.PhoneNumber) &&
  !await _httpClient.GetFromJsonAsync<bool>($"https://coredidemo.azurewebsites.net/phonenumbercheck.html?number={Uri.EscapeDataString(person.PhoneNumber)}"))
{
  throw new Exception("PhoneNumber cannot be in the future.");
}
```

Registreer de HttpClient ook in `Program.cs`:

```csharp
builder.Services.AddHttpClient();
```

Let op: je hebt een package reference naar `Microsoft.Extensions.Http` nodig.

Fix nu de fouten in de unit tests door overal `new HttpClient(A.Fake<HttpMessageHandler>())` te injecteren. Helaas werkt `A.Fake<HttpClient>()` niet omdat dit geen abstracte class is en dus de implementatie gewoon wordt aangeroepen.
De `HttpClient` is simpelweg niet gemaakt om op deze manier ondervangen te worden in unit tests. Er is wel een andere manier beschikbaar.

Maak nu een unit test:

```csharp
[Test()]
public void AddPerson_WithFilledValidPhoneNumber_Succeeds()
{
  var _mockMessageHandler = A.Fake<HttpMessageHandler>();

  A.CallTo(_mockMessageHandler)
  .WithReturnType<Task<HttpResponseMessage>>()
  .Returns(new HttpResponseMessage
  {
    StatusCode = HttpStatusCode.OK,
    Content = new StringContent("true")
  });

  var testRepository = A.Fake<Repository>();
  var bc = new PersonBC(testRepository, NullLogger<PersonBC>.Instance, CreateTimeProvider(), new HttpClient(A.Fake<HttpMessageHandler>()));

  Assert.DoesNotThrowAsync(() => bc.AddPerson(CreatePerson(p => p.PhoneNumber = "(06) 11")));
  A.CallTo(() => testRepository.Add(A<Person>._)).MustHaveHappenedOnceExactly();
}
```

Dit is een optie, zowel het injecteren van de hpptclient als het ondervangen daarvan is eigenlijk een externe dependency. Externe dependencies zou je het liefste kunnen faken. Dit doe je als volgt:

```csharp
public class PhonenumberChecker
{
  private readonly HttpClient _httpClient;

  public PhonenumberChecker(HttpClient httpClient)
  {
    _httpClient = httpClient;
  }

  public virtual async Task<bool> CheckPhoneNumber(string? phoneNumber)
  {
    return !string.IsNullOrEmpty(phoneNumber) &&
      await _httpClient.GetFromJsonAsync<bool>($"https://coredidemo.azurewebsites.net/phonenumbercheck.html?number={Uri.EscapeDataString(phoneNumber)}");
  }
}
```

Pas nu de constructor weer aan naar:

```csharp
public PersonBC(Repository repository, ILogger<PersonBC> logger, TimeProvider timeProvider, PhonenumberChecker phonenumberChecker)
```

Deze oplossing is wat beter aangezien te tests van de httprequest nu bij de unit tests van de `PhonenumberChecker` zitten. Bij de `PersonBC` tests kunnen we nu een `PhonenumberChecker` makkelijk faken.

De tests van `PhonenumberChecker` zullen er nu als volgt uit zien:

```csharp
[TestFixture()]
public class PhonenumberCheckerTests
{
  [Test()]
  public void CheckPhoneNumber_WhenServiceReturnTrue_ReturnsTrue()
  {
    var _mockMessageHandler = A.Fake<HttpMessageHandler>();

    A.CallTo(_mockMessageHandler)
    .WithReturnType<Task<HttpResponseMessage>>()
    .Returns(new HttpResponseMessage
    {
      StatusCode = HttpStatusCode.OK,
      Content = new StringContent("true")
    });

    var checker = new PhonenumberChecker(new HttpClient(_mockMessageHandler));

    Assert.That(() => checker.CheckPhoneNumber("(06) 11"), Is.True);
  }

  [Test()]
  public void CheckPhoneNumber_WhenServiceReturnFalse_ReturnsFalse()
  {
    var _mockMessageHandler = A.Fake<HttpMessageHandler>();

    A.CallTo(_mockMessageHandler)
    .WithReturnType<Task<HttpResponseMessage>>()
    .Returns(new HttpResponseMessage
    {
      StatusCode = HttpStatusCode.OK,
      Content = new StringContent("false")
    });

    var checker = new PhonenumberChecker(new HttpClient(_mockMessageHandler));

    Assert.That(() => checker.CheckPhoneNumber("(06) 11"), Is.False);
  }
}
```

Tip: Gebruik niet rechtstreeks `IHttpClientFactory`, deze is bedoelt als "onder water" object, maar gebruik daarvoor typed clients, [zie](https://www.milanjovanovic.tech/blog/the-right-way-to-use-httpclient-in-dotnet).

Je kan [hier](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-net-http-httpclient) meer lezen over het gebruik van httpclient.

Tip: Je ziet dat PhonenumberChecker geen abstract class of interface is, maar gewoon een implemetatie class met virtual functies. Dit is gedaan omdat in de runtime class er niet meerdere implementaties bestaan voor de PhonenumberChecker. 
De enige plek waar we een andere implementatie willen is aan de unit testing kant. Daarom geen base class, maar virtual functies op de implementie class. Hierdoor kun je in de runtime ook makkelijker debuggen omdat F12 (Go to definition) dan gewoon naar de code springt een geen eextra abstractie of indirecte aan de runtime toevoegt.

Tip: Als frameworks en het aantal BC's groeien kan het aantal classes welke je bij DI moet registreren erg groot worden. In veel van dergelijke frameworks zie je dat er een soort helper generic interface wordt gemaakt waarmee je auto registratie bij eerste noodzaak kan bereiken. Wil je ook iets dergelijks maken in je eigen framework neem dan contact op met BVR.

## Meters

We willen een live mee kunnen kijken hoeveel personen er zijn toegevoegd in een perfmon achtige constructie. We kunnen dit doen met `Meters`.

Voeg eerst een class toe die wel willen injecteren om de meters te gebruiken.

```csharp
public class CrmMeters
{
  private readonly Counter<int> _personsAdded;

  public CrmMeters(IMeterFactory meterFactory)
  {
    var meter = meterFactory.Create("Afas.Bvr.Crm");
    _personsAdded = meter.CreateCounter<int>("afas.bvr.crm.persons_added");
  }

  public void PersonsAdded(int quantity)
  {
    _personsAdded.Add(quantity);
  }
}
```

Injecteer de class in de `PersonBC`:

```csharp
public PersonBC(Repository repository, ILogger<PersonBC> logger, TimeProvider timeProvider, HttpClient httpClient, CrmMeters meters)
{
  _repository = repository;
  _logger = logger;
  _timeProvider = timeProvider;
  _httpClient = httpClient;
  _meters = meters;
}
```

Voeg nu onder aan de `AddPerson` methode de code toe `meters.PersonsAdded(1);`.

We moeten nu zorgen dat de `CrmMeters` geregistreerd wordt in de DI container en dat de meter een aantal maak wordt aangeroepen. Dit doen we in `Program.cs`:

```csharp
static async Task Main(string[] args)
{
  var builder = Host.CreateApplicationBuilder(args);

  builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
  builder.Services.AddAzureStorageTableRepository(builder.Configuration);
  builder.Services.AddSingleton<CrmMeters>();
  builder.Services.AddSingleton<PersonBC>();
  builder.Services.AddHttpClient();
  builder.Services.AddSingleton<PhonenumberChecker, WebPhonenumberChecker>();

  var host = builder.Build();

  var bc = host.Services.GetRequiredService<PersonBC>();

  var start = DateTime.UtcNow;
  while(DateTime.UtcNow - start < TimeSpan.FromSeconds(10))
  {
    var personId = Guid.NewGuid();
    Console.WriteLine("AddPerson");
    await bc.AddPerson(new Person { Id = personId, FirstName = "Bart", LastName = "Vries", Email = "bart.vries@afas.nl" });

    Console.WriteLine("GetPersonOrDefault");
    var retrieved = await bc.GetPersonOrDefault(personId);

    Console.WriteLine("DeletePerson");
    await bc.DeletePerson(personId);
  }

  Console.WriteLine("Done!");
}
```

Build het programma en draai de onderstaande command prompt

```bat
dotnet-counters monitor --counters Afas.Bvr.Crm -- ConsoleApp1 --SasSignature="sv=2022-11-02&ss=t&srt=sco&sp=rwdlacu&se=2028-12-11T23:55:39Z&st=2024-12-11T15:55:39Z&spr=https&sig=e684bQmmbwMXysmGBlbIlA4h365DFVDlJa1nVVeINOk%3D"
```

Tip: Omdat je nu met een prompt werkt worden de secrets niet gelezen, vandaar dat de `SasSignature` weer in de command line staat.

Tip: Microsoft heeft veel standaard counters welke vroeger in perfmon stonden. Haal `--counters Afas.Bvr.Crm` weg om deze te zien.

Denk eraan dat er veel verschillende type counter beschrikbaar zijn, op dit moment Counter, UpDownCounter, ObservableCounter, ObservableUpDownCounter, Gauge, ObservableGauge en Histogram.

Je kan [hier](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation) meer lezen over het gebruik van Meters.
