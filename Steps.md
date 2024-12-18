# Na sheet 9: Eerste stappen Core DI

Basis implementatie van .net core DI
```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<StorageSettings>(provider =>
    JsonSerializer.Deserialize<StorageSettings>(
    File.ReadAllText("appsettings.json"))!);

builder.Services.AddSingleton<ILogger, ConsoleLogger>();
builder.Services.AddSingleton<PersonBC>();

var host = builder.Build();
```

# Na sheet 13 en 14: PersonBC & CrmValidations unit tests

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

Nu kunnen de de unit test gaan schrijven, gebruik vs2022 `Create Unit Tests` optie om snel een class te genereren:
```csharp
  [Test()]
public void AddPerson_FirstNameStartWithSpace_ThrowExceptions()
{
var testRepository = A.Fake<Repository>();

var bc = new PersonBC(testRepository);

Assert.ThrowsAsync<Exception>(() => bc.AddPerson(new Person() { Id = Guid.NewGuid(), FirstName = "Bart" }));
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
A.CallTo(() => testRepository.Add<Guid, Person>(A<Person>._)).MustHaveHappenedOnceExactly();
}
```

Run de test en laat zien dat hij werkt. We gaan nu unit test voor `CrmValidations` schrijven, gebruik vs2022 `Create Unit Tests` optie om snel een class te genereren.

Helaas geeft deze een foutmelding. We kunnen alleen publics testen. We willen de class  niet public maken omdat andere dan door andere kan worden aangeroepen. We kunnen de class wel internal maken/houden en de assembly zichtbaar maken voor de test assembly. Dit doen we door in de `AssemblyInfo.cs` van de `CrmValidations` project de volgende regel toe te voegen:
`[assembly: InternalsVisibleTo("CrmValidations.Tests")]`

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

# Na sheet 15: Repository unit tests

Creeer een unit test op `MSSqlRepository`.

```csharp
[TestFixture()]
[Category("Dep:MSSql")] // Let op, dit helpt om snel tests te filteren, omdat deze een sql dependency hebben
public class MSSqlRepositoryTests
{
  private const string _connectionString = "Server=.\\profitsqldev;Database=codedidemo;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";

  class Demo : RepositoryObject<Guid>
  {
    public string? Name { get; set; }
  }

  [Test()]
  public async Task GetOrDefault_WithoutItem_ReturnsNull()
  {
    var repo = new MSSqlRepository(_connectionString);

    var result = await repo.GetOrDefault<Guid, Demo>(Guid.NewGuid());
    Assert.That(result, Is.Null);
  }

  [Test()]
  public async Task Add_RunsWithoutFailure_HasItemInDB()
  {
    var repo = new MSSqlRepository(_connectionString);

    var id = Guid.NewGuid();

    var demo = new Demo { Id = id, Name = "Test" };
    await repo.Add<Guid, Demo>(demo);

    var result = await repo.GetOrDefault<Guid, Demo>(id);
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
[Category("Dep:MSSql")]
public class MSSqlRepositoryTests
```

naar 

```csharp
[TestFixture()]
[Category("Dep:MSSql")]// Let op, dit helpt om snel tests te filteren, omdat deze een sql dependency hebben
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
[Category("Dep:AzureStorageTable")]
public class AzureStorageTableRepositoryTests : RepositoryTests
{
  public override Repository CreateRepository() => new AzureStorageTableRepository(@"https://codedidemo.table.core.windows.net/", @"sv=2022-11-02&ss=t&srt=sco&sp=rwdlacu&se=2028-12-11T23:55:39Z&st=2024-12-11T15:55:39Z&spr=https&sig=e684bQmmbwMXysmGBlbIlA4h365DFVDlJa1nVVeINOk%3D");
}
```

Draai de tests, we hebben direct een bug te pakken.

Verander `var entity = await tableClient.GetEntityAsync<TableEntity>(id.ToString(), id.ToString());` naar `var entity = await tableClient.GetEntityIfExistsAsync<TableEntity>(id.ToString(), id.ToString());`.

Run de tests opnieuw. Alles is gefixed.

# Na sheet 17: Configuration in Core DI

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
Run de code. Helaas werkt dit niet standaard in console apps. In asp.net core werkt het standaard wel. We moeten in de console app even toevoegen dat dit een development omgeving is.
Ga naar de `Launch Profile` van het project en geef bij de `Environment variabele` in `DOTNET_ENVIRONMENT=Development`.
Run de code.

Alleen de unit test compileerd niet meer, pas deze aan naar:
```csharp
public override Repository CreateRepository()
{
  var set = Options.Create<AzureStorageTableSettings>(new AzureStorageTableSettings() {
    Endpoint = @"https://codedidemo.table.core.windows.net/",
    SasSignature = @"sv=2022-11-02&ss=t&srt=sco&sp=rwdlacu&se=2028-12-11T23:55:39Z&st=2024-12-11T15:55:39Z&spr=https&sig=e684bQmmbwMXysmGBlbIlA4h365DFVDlJa1nVVeINOk%3D"
  });

  return new AzureStorageTableRepository(set);
}
```

# Na sheet 21: Logging in Core DI

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