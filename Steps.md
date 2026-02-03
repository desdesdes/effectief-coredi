# Na sheet 8: Eerste stappen Core DI

Basis implementatie van .net core DI
```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(provider =>
    JsonSerializer.Deserialize<StorageSettings>(
    File.ReadAllText("appsettings.json"))!);

builder.Services.AddSingleton<ILogger, ConsoleLogger>();
builder.Services.AddSingleton<PersonBC>();

using var host = builder.Build();
await host.StartAsync();

var bc = host.Services.GetRequiredService<PersonBC>();
...
await host.StopAsync();
```

# Na sheet 12: PersonBC & CrmValidations unit tests

Nu kunnen de de unit test gaan schrijven
- Voeg aan `Afas.Bvr.Core.Tests` een referentie toe naar `Afas.Bvr.Crm`.
- Voeg aan `Afas.Bvr.Core.Tests` een class toe genaamd `PersonBCTests`.
- Voeg de onderstaande code toe aan de `PersonBCTests` class.
```csharp
[TestFixture()]
public class PersonBCTests
{
  [Test()]
  public void AddPerson_FirstNameStartWithSpace_ThrowsException()
  {
    var bc = new PersonBC(new StorageSettings());
  }
}
```

Vreemd `PersonBC` heeft helemaal geen afhankelijkheid van `StorageSettings`, maar van `Repository`, laten we dat aanpassen.

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

builder.Services.AddSingleton(provider =>
    Repository.CreateRepository(JsonSerializer.Deserialize<StorageSettings>(
    File.ReadAllText("appsettings.json"))!));

builder.Services.AddSingleton<ILogger, ConsoleLogger>();
builder.Services.AddSingleton<PersonBC>();

using var host = builder.Build();
await host.StartAsync();
```

Tip: Op `Host` en `WebApplication` vind je meerdere `Create` methodes, veel zijn deprecated. `Host.CreateApplicationBuilder()` of `WebApplication.CreateBuilder()` 
zijn de geadviseerde functies, zie [hier](https://github.com/dotnet/runtime/discussions/81090).

Pas de test als volgt aan:
```csharp
[Test()]
public void AddPerson_FirstNameStartWithSpace_ThrowsException()
{
  var bc = new PersonBC(A.Fake<Repository>());

  Assert.ThrowsAsync<Exception>(() => bc.AddPerson(new Person() { Id = Guid.NewGuid(), FirstName = "Bart", LastName = "Vries" }));
}
```
We gebruiken hier `FakeItEasy` om een `Repository` te faken. Dit is een library die het makkelijk maakt om objecten te faken. 
We kunnen nu de test schrijven zonder dat we een echte `Repository` nodig hebben. Aangezien de ILogger optional was kunnen we deze weglaten.

Run de test en laat zien dat deze faalt. Pas ook de input aan naar `" Bart"` en laat zien dat hij dan wel werkt.

We voegen nu ook een test toe die controleert of de `AddPerson` methode van `PersonBC` de `Add` methode van `Repository` aanroept.
```csharp
[Test()]
public void AddPerson_WithProperData_Succeeds()
{
  var testRepository = A.Fake<Repository>();

  var bc = new PersonBC(testRepository);

  Assert.DoesNotThrowAsync(() => bc.AddPerson(new Person() { Id = Guid.NewGuid(), FirstName = "Bart", LastName = "Vries" }));
  A.CallTo(() => testRepository.Add(A<Person>._)).MustHaveHappenedOnceExactly();
}
```

Run de test en laat zien dat hij werkt. We gaan nu unit test voor `CrmValidations` schrijven.
- Voeg aan `Afas.Bvr.Core.Tests` een class toe genaamd `CrmValidationsTests`.
- Voeg aan de class de volgende code toe:
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

Helaas geeft deze een foutmelding. We kunnen alleen publics testen. We willen de class niet public maken omdat andere dan door andere kan worden aangeroepen. 
We kunnen de class wel internal maken/houden en de assembly zichtbaar maken voor de test assembly. 
Dit doen we door een class genaamd `AssemblyInfo.cs` toe te voegen aan het `CrmValidations` project met de volgende regel:
`[assembly: InternalsVisibleTo("Afas.Bvr.Core.Tests")]`

Nu kun je wel de test wel uitvoeren.

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
{
  private const string _connectionString = "Server=.\\profitsqldev;Database=codedidemo;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";
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

Vervang nu de regels met `var repo = new MSSqlRepository(_connectionString);` naar `var repo = CreateRepository();`. Het testen werkt weer.

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

Applicaties zullen niet zomaar wisselen tussen providers. Zo willen we in de Console app eigenlijk alleen de `AzureStorageTableRepository` implemeteren.
De complete `StorageSettings` willen we ook laten vervallen en een betere oplossing creeren.

1. Verwijder de `CreateRepository` functie.
2. Rename `StorageSettings` naar `AzureStorageTableSettings`.
4. Verwijder `StorageType` en `MsSqlConnectionString`.
5. Maak de properties required.
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

Druk op F1 op `CreateApplicationBuilder` in `Program.cs`. Je ziet dat IConfiguration geladen worden wordt vanuit `appsettings.json`.
We kunnen hier simpel gebruik van maken. 

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<AzureStorageTableSettings>().BindConfiguration(string.Empty);
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
Tip 1: Bij BindConfiguration vind je een `string.Empty`, je kan hier de sectie opgeven, vaak is het handig deze sectienaam op de `AzureStorageTableSettings` class 
als `public const string Section = "AzureStorageTableSettings";` te definieren.

Tip 2: Achter `BindConfiguration()` kun je `ValidateDataAnnotations()` en/of `ValidateOnStart()` aanroepen om de settings te valideren.

Ga naar **Debug** > **ConsoleApp1 Debug Properties** en geef bij de `Command line arguments` in `/Endpoint="https://bvr.nl"`.
Run de code. De code gaat fout omdat de command line argument de appsettings heeft overschreven.
Verwijder de command line argument.

Open appsettings.json. We zien dat hierin een `SasSignature` staat. Dat is een geheim en willen we niet in de `appsettings.json` hebben staan,
omdat deze in git kan komen of we kunnen hem per ongeluk aan iemand geven als we de bin map delen. Microsoft heeft een speciale oplossing voor developers.

Knip de `SasSignature` regel uit appsettings.
Druk op `Manage User Secrets` in de rechtermuisknop van het project.
Plak de `SasSignature` regel in de `secrets.json` file.
Run de code. Helaas werkt dit niet standaard in console apps. In asp.net core apps werkt het standaard wel. 
We moeten in de console app even [instellingen](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-9.0) toevoegen, 
zodat .net weet dat dit een development omgeving is.
Ga naar **Debug** > **ConsoleApp1 Debug Properties** en geef bij de `Environment variabele` in `DOTNET_ENVIRONMENT=Development`.
Run de code.

Alleen de unit test compileert niet meer, pas deze aan naar:
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

Tip: Meestal eindigen we classes die we via IOptions injecteren met `Options`, dus `AzureStorageTableSettings` zou `AzureStorageTableOptions` kunnen heten.

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

Verwijder de regel met `builder.Services.AddSingleton<ILogger, ConsoleLogger>();` uit `Program.cs`.
Run de code. Deze werkt weer. Wat zijn de niet ingesprongen console regels? Dat zijn handmatig geschreven regels via `Console.WriteLine`.

Druk op F1 op `CreateApplicationBuilder` in `Program.cs`. Je ziet dat er een `ILogger` wordt toegevoegd die logd naar console, debug en eventlog.
Deze is ook standaard via IConfiguration te configureren. Pas `AppSettings.json` aan:

```json
{
  "MsSqlConnectionString": "Server=.\\profitsqldev;Database=codedidemo;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;",
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

Kijk even naar de methodes op de ILogger. Er is zelfs een source generator voor betere logging.

- Maak de `PersonBC` class partial.
- Voeg de voglende code aan `PersonBC` toe

```csharp
  [LoggerMessage(Message = "Person added {id}", Level = LogLevel.Critical)]
  public partial void LogAddPerson(Guid id);
}
```

Pas nu de aanroep op `_logger?.LogInformation` aan naar `LogAddPerson(id);`.

Run de code. 

Tip: Maak de logger niet nullable. Het is beter om een `NullLogger.Instance` te gebruiken in unit tests. 
Pas de constructor van `PersonBC` aan naar `public PersonBC(Repository repository, ILogger<PersonBC> logger)`

De unit test gaan nu fout. Pas deze aan naar zodat deze `NullLogger<PersonBC>.Instance` meegeven als ILogger.

Denk eraan dat je ook de logging zou kunnen testen in unit test.

Je kan [hier](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging-library-authors) meer lezen over logging.

# Na sheet 25: Overgeslagen

## Extension methodes op IServiceFactory

Kijk naar de code in `Program.cs`. Hoe moet weten dat we `AddSingleton` moeten gebruiken om `AzureStorageTableRepository` te registreren? 
En hoe weet je dat je dan ook `AzureStorageTableSettings` nodig hebt?
We kunnen dit makkelijker maken door een extension methode te maken op `IServiceCollection`.
Voeg de een nuget package referentie toe aan `Microsoft.Extensions.Options.ConfigurationExtensions` aan `Afas.Bvr.Core`.
Voeg nu de volgende class toe:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Afas.Bvr.Core.Storage;

public static class ServiceCollectionExtensions
{
  extension(IServiceCollection services)
  {
    public IServiceCollection AddAzureStorageTableRepository(IConfiguration namedConfigurationSection)
    {
      services.Configure<AzureStorageTableOptions>(namedConfigurationSection);
      services.AddSingleton<Repository, AzureStorageTableRepository>();
      return services;
    }
  }
}
```
Verwijder de AddOptions en `.AddSingleton<Repository, AzureStorageTableRepository>()` uit  `Program.cs` en pas de cde aan naar aan naar:
```csharp
builder.Services.AddAzureStorageTableRepository(builder.Configuration);
```

Je kan [hier](https://learn.microsoft.com/en-us/dotnet/core/extensions/options-library-authors) meer lezen over options.

## TimeProvider

Een persoon kan alleen in het verleden geboren zijn. We vogen hiervoor een controle toe in `PersonBC`.

```csharp
if(person.BirthDate.HasValue && person.BirthDate.Value.ToDateTime(TimeOnly.MinValue) > DateTime.Now)
{
  throw new Exception("BirthDate cannot be in the future.");
}
```

Als we dit gaan unit testen is het echter niet meer deterministisch. Op een andere tijd geeft hij andere output. 
We kunnen dit oplossen door een `ITimeProvider` te gebruiken.

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
Let op: je hebt een package reference naar `Microsoft.Extensions.TimeProvider.Testing` nodig.

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

Build de code en fix de laatste fouten. Draai de tests.

Tip:
Je ziet in `PersonBCTests` dat je voor veel tests een basis werkende persoon entititeit nodig hebt. Je zou hier een helper functie voor kunnen maken.


```csharp
private Person CreatePerson()
{
  return new Person() { Id = Guid.NewGuid(), FirstName = "Bart", LastName = "Vries" };
}
```

Je kunt nu refactoren zodat je niet overal `new Person() { Id = Guid.NewGuid(), FirstName = "Bart", LastName = "Vries" };` hoeft te schrijven.

## Configureren opties op basis van andere DI objecten

Soms wil je opties configureren op basis van andere DI objecten. Stel we willen een instelling van `AzureStorageTableSettings` bijwerken op basis van de `TimeProvider`.

Voeg aan `ConsoleApp1` de volgende class toe:

```csharp
internal class AzureStorageTableConfigurator : IConfigureOptions<AzureStorageTableOptions>
{
  private readonly TimeProvider _timeProvider;

  public AzureStorageTableConfigurator(TimeProvider timeProvider)
  {
    _timeProvider = timeProvider;
  }

  public void Configure(AzureStorageTableOptions options)
  {
    if(_timeProvider.GetUtcNow() > new DateTime(2025, 12, 31))
    {
      options.SasSignature = string.Empty;
    }
  }
}
```

Registreer de class in `Program.cs` net voor de regel `using var host = builder.Build();`:
```csharp
builder.Services.ConfigureOptions<AzureStorageTableConfigurator>();
```

Run de code. De code werkt niet omdat de `SasSignature` leeg is. Pas de datum vergelijking aan zodat de code weer werkt.

## HttpClient 


We willen een externe API aanroepen om te controleren of of de persoon niet bestaat in een extern systeem. We kunnen dit doen met `HttpClient`.
Het is te adviseren deze te registreren in de DI container. Het is best practices om `HttpClient` te hergebruiken om te voorkomen dat we uit connencties lopen.

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
var response = await _httpClient.GetFromJsonAsync<JsonObject>($"https://jsonplaceholder.typicode.com/users/1");
if(string.Equals(response?["name"]?.GetValue<string>(), $"{person.FirstName} {person.LastName}", StringComparison.Ordinal))
{
  throw new Exception($"Person with name '{person.FirstName} {person.LastName}' already exists in external system.");
}
```

Registreer de HttpClient ook in `Program.cs`:
Let op: je hebt een package reference naar `Microsoft.Extensions.Http` nodig.


```csharp
builder.Services.AddHttpClient();
```

Om de unit test te fixen zou je `A.Fake<HttpClient>()` willen gebruiken.
Helaas zijn de methodes op HttpClient niet virtueel en dus de implementatie gewoon wordt aangeroepen. 
De `HttpClient` is simpelweg niet gemaakt om op deze manier ondervangen te worden in unit tests. Er is wel een andere manier beschikbaar.

Wel kunnen we dit oplossen door `new HttpClient(A.Fake<HttpMessageHandler>())` te injecteren.

Fix nu een unit test door de onderstaande functie toe te voegen en `CreateHttpClient()` aan te roepen bij `new PersonBC(`:

```csharp
private HttpClient CreateHttpClient()
{
  var _mockMessageHandler = A.Fake<HttpMessageHandler>();

  A.CallTo(_mockMessageHandler)
    .WithReturnType<Task<HttpResponseMessage>>()
    .Returns(new HttpResponseMessage
    {
      StatusCode = HttpStatusCode.OK,
      Content = new StringContent("{ \"name\": \"Leanne Graham\" }")
    });

  return new HttpClient(_mockMessageHandler);
}
```

Tip: Als je als username "Bart Vries" meegeeft in de test zal de test falen, het is altijd slim om zowel een falende als een werkende test op te nemen 
zodat je borgd dat je testcode ook echt geraakt wordt.

Hoewel dit een werkende optie is, blijft dit vrij omslagtig. Daarnaast is het gebruik va de `HttpClient` om de service aan te roepen eigenlijk een 
implementatie detail en zou dus niet in de `PersonBC` moeten zitten. 

We kunnen dit beter ondervangen door een aparte class te maken waar de zowel de afhakelijkheid van de `HttpClient` als de implementatie van de externe service in zit. 
Deze externe afhakelijkheid kun je dan ook erg makkelijk faken. 

Voeg de volgende class toe aan `Afas.Bvr.Crm`:

```csharp
public class CrmDependencies
{
  private readonly HttpClient _httpClient;

  public CrmDependencies(HttpClient httpClient)
  {
    _httpClient = httpClient;
  }

  public virtual async Task ValidateExternalUserNameAsync(string? firstName, string? lastName)
  {
    var response = await _httpClient.GetFromJsonAsync<JsonObject>($"https://jsonplaceholder.typicode.com/users/1");
    if(string.Equals(response?["name"]?.GetValue<string>(), $"{firstName} {lastName}", StringComparison.Ordinal))
    {
      throw new Exception($"Person with name '{firstName} {lastName}' already exists in external system.");
    }
  }
}
```

Pas nu de constructor weer aan naar:

```csharp
public PersonBC(Repository repository, ILogger<PersonBC> logger, TimeProvider timeProvider, CrmDependencies depManager)
```

Pas de aanroep aan naar `await _depManager.ValidateExternalUserNameAsync(person.FirstName, person.LastName);`.

Deze oplossing is wat beter aangezien we `CrmDependencies` los kunnen testen en bij de `PersonBC` tests simpelweg `CrmDependencies` kunnen faken.
Verwijder de `CreateHttpClient()` uit de `PersonBCTests` en vervang de aanroep door een fake `A.Fake<Repository>()`
Voeg aan `Afas.Bvr.Core.Tests` De volgende test class toe:

```csharp
[TestFixture()]
public class CrmDependenciesTests
{
  private HttpClient CreateHttpClient()
  {
    var _mockMessageHandler = A.Fake<HttpMessageHandler>();

    A.CallTo(_mockMessageHandler)
      .WithReturnType<Task<HttpResponseMessage>>()
      .Returns(new HttpResponseMessage
      {
        StatusCode = HttpStatusCode.OK,
        Content = new StringContent("{ \"name\": \"Leanne Graham\" }")
      });

    return new HttpClient(_mockMessageHandler);
  }

  [Test]
  public async Task ValidateExternalUserNameAsync_WithExistingUser_ThrowsException()
  {
    var depManager = new CrmDependencies(CreateHttpClient());
    Assert.ThrowsAsync<Exception>(() => depManager.ValidateExternalUserNameAsync("Leanne", "Graham"));
  }

  [Test]
  public async Task ValidateExternalUserNameAsync_WithNonExistingUser_DoesNotThrow()
  {
    var depManager = new CrmDependencies(CreateHttpClient());
    Assert.DoesNotThrowAsync(() => depManager.ValidateExternalUserNameAsync("Bart", "Vries"));
  }
}
```

Tip: Gebruik niet rechtstreeks `IHttpClientFactory`, deze is bedoelt als "onder water" object, maar gebruik daarvoor typed clients, 
[zie](https://www.milanjovanovic.tech/blog/the-right-way-to-use-httpclient-in-dotnet).

Je kan [hier](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-net-http-httpclient) meer lezen over het gebruik van httpclient.

Tip: Je ziet dat CrmDependencies geen abstract class of interface is, maar gewoon een implementatie class met virtual functies. 
Dit is gedaan omdat in de runtime er niet meerdere implementaties bestaan voor de CrmDependencies. 

De enige plek waar we een andere implementatie willen is aan de unit testing kant. Daarom geen base class, maar virtual functies op de implementie class. 
Hierdoor kun je in de runtime ook makkelijker debuggen omdat F12 (Go to definition) dan gewoon naar de code springt een geen extra abstractie of indirecte aan de 
runtime toevoegt. Voeg geen indirectie toe zonder dat dit echt nodig is.

Tip: Als frameworks en het aantal BC's groeien kan het aantal classes welke je bij DI moet registreren erg groot worden. Dit is niet altijd wenselijk en in grotere 
frameworks zie je dan ook dat er een aanvulling op het standaard di framework worden gebouwd om hierin te voorzien. Dit kan bijvoorbeeld door BC altijd te constructen 
via een factory class welke constructor parameters controleeert op een attribuut of interface en deze vervolgens via `ActivatorUtilities.CreateInstance(`.

Wil je ook iets dergelijks maken in je eigen framework overleg dan altijd met specialisten.

## Meters

We willen een live mee kunnen kijken hoeveel personen er zijn toegevoegd in een perfmon achtige constructie. We kunnen dit doen met `Meters`.

Voeg eerst een class toe aan `Afas.Bvr.Crm` die wel willen injecteren om de meters te gebruiken.

```csharp
public class CrmMeters
{
  private readonly Counter<int> _personsAdded;

  public CrmMeters(IMeterFactory meterFactory)
  {
    var meter = meterFactory.Create("Afas.Bvr.Crm");
    _personsAdded = meter.CreateCounter<int>("afas.bvr.crm.persons_added");
  }

  public virtual void PersonsAdded(int quantity)
  {
    _personsAdded.Add(quantity);
  }
}
```

Injecteer de class in de `PersonBC`:

```csharp
public PersonBC(Repository repository, ILogger<PersonBC> logger, TimeProvider timeProvider, CrmDependencies depManager, CrmMeters meters)
{
  _repository = repository;
  _logger = logger;
  _timeProvider = timeProvider;
  _httpClient = httpClient;
  _meters = meters;
}
```

Voeg nu onder aan de `AddPerson` methode de code toe `meters.PersonsAdded(1);`.

We moeten nu zorgen dat de `CrmMeters` geregistreerd wordt in de DI container en dat de meter een aantal maak wordt aangeroepen. Dit doen we door `builder.Services.AddSingleton<CrmMeters>();` toe te voegen aan `Program.cs`.
Pas de unit tests aan zodat deze een fake `CrmMeters` meegeven.

Pas het aanmaken van de personen aan zodat elke seconde een persoon wordt toegevoegd voor 50 seconden.

```csharp
Guid personId = Guid.NewGuid();
for (int i = 0; i < 50; i++)
{
  Console.WriteLine("AddPerson");
  await bc.AddPerson(new Person { Id = personId, FirstName = "Bart", LastName = "Vries", Email = "bart.vries@afas.nl" });
  personId = Guid.NewGuid();
  await Task.Delay(1000);
}
```

Build en lat het programma lopen. Terwijl het programma loopt open je een command prompt en voer je de volgende commando's uit om de meters te bekijken:

```cmd
dotnet tool install --global dotnet-counters
dotnet-counters monitor --counters Afas.Bvr.Crm --name ConsoleApp1
```

Tip: Microsoft heeft veel standaard counters welke vroeger in perfmon stonden. Haal `--counters Afas.Bvr.Crm` weg om deze te zien.

Denk eraan dat er veel verschillende type counter beschrikbaar zijn, op dit moment Counter, UpDownCounter, ObservableCounter, ObservableUpDownCounter, Gauge, 
ObservableGauge en Histogram.

Je kan [hier](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation) meer lezen over het gebruik van Meters.
