# Fenestra — Roadmap de Features (Tiers 1–3)

## Context

Fenestra já é um framework de hospedagem WPF razoavelmente completo (tray, toast, auto-start, hotkeys, theme detection, taskbar progress, single-instance, event bus, registry config, window state). Após revisão da superfície atual, identificamos 10 features que preenchem gaps reais e se alinham ao escopo ("framework de hospedagem + interop Windows para apps WPF"), sem invadir território de libs especializadas (MVVM, logging, crash reporting).

Este documento é um **roadmap de design** — cada feature traz API sketch, localização de arquivos, padrões a reutilizar e gotchas. Não é uma implementação passo-a-passo de cada uma; cada feature deve, ao ser pegada, virar seu próprio plano detalhado. A intenção é alinhar o *shape* das features antes de começar.

**Decisões do usuário (já capturadas):**
- Auto-updater: **removido** do plano (scope creep).
- Strongly-typed settings: **IOptionsMonitor<T> + RegistryWatcher**.
- Splash screen: **ambos** — wrapper simples + opção custom.

**Importante — achado da exploração:** O IPC para Single Instance (forwarding de args da 2ª pra 1ª instância) **já está implementado** em `src/Fenestra.Windows.Wpf/Services/SingleInstanceGuard.cs` via NamedPipe + Mutex, despachando para `ISingleInstanceApp.OnArgumentsReceived()`. Este item saiu do Tier 1; ação apenas de verificação/documentação (ver seção final).

---

## Padrões a Reutilizar (comuns a todas as features)

Antes de desenhar cada feature, fixar os idioms do projeto:

| Padrão | Localização | Quando usar |
|---|---|---|
| `Add*(this IServiceCollection)` fluente | `src/Fenestra.Windows/Extensions/*.cs`, `src/Fenestra.Windows.Wpf/Extensions/*.cs` | Toda feature opt-in |
| Hook manual via `Attach()`/`Register()` chamado no startup | `FenestraApp.OnStartup` / `FenestraApplication.RunCore` (linhas ~169–220) | Features que precisam rodar no ciclo de vida (não usar `IHostedService`) |
| `FenestraComponent` base + sync Dispose + finalizer | `src/Fenestra.Core/FenestraComponent.cs` | Toda classe com recursos nativos |
| `HwndSource` + `AddHook(WndProc)` (message-only window) | `src/Fenestra.Windows.Wpf/Services/GlobalHotkeyService.cs:15-62` | Features que precisam de window messages (power, session, IPC-COM) |
| `RegistryWatcher` + `RegNotifyChangeKeyValue` | `src/Fenestra.Windows/Services/RegistryWatcher.cs` | Detectar mudanças em HKCU (reutilizado por Theme, vai ser reutilizado por Settings) |
| `IRegistryConfig` + `[RegistrySection]` reflection | `src/Fenestra.Windows/Services/RegistryConfigService.cs` | Persistir estado opcional |
| `[DllImport]` (não `LibraryImport`) em pasta `Native/` | `src/Fenestra.Windows/Native/`, `src/Fenestra.Windows.Wpf/Native/` | Todo P/Invoke. Usar `SafeHandle` quando há handle |
| `ShellLink` + `AppShortcutManager` | `src/Fenestra.Windows/Services/AppShortcutManager.cs` | Features que precisam de shortcut no Start Menu |
| TFMs: `Core`/`Windows` = `net6.0;net472`. `Windows.Wpf` = `net6.0-windows;net472` | Directory.Build.props | Regra: código WPF-dependente vai em `Windows.Wpf`; interop Win32 puro vai em `Windows` |

---

## Tier 1 — Alto valor, pouco código

### T1.1 — Credential Vault

**Problema:** `IRegistryConfig` não é adequado pra segredos (tokens de API, senhas). Windows tem Credential Manager (advapi32 `CredRead`/`CredWrite`/`CredDelete`) que armazena por-usuário, criptografado via DPAPI, acessível via UI padrão.

**API sketch:**
```csharp
public interface ICredentialVault
{
    void Store(string target, string username, string secret);
    StoredCredential? Read(string target);
    bool Delete(string target);
    IReadOnlyList<string> Enumerate(string? targetFilter = null);
}

public sealed record StoredCredential(string Target, string Username, string Secret);
```

**Registro:**
```csharp
builder.Services.AddWindowsCredentialVault();  // in Fenestra.Windows
```

**Arquivos a criar:**
- `src/Fenestra.Windows/ICredentialVault.cs`
- `src/Fenestra.Windows/Services/CredentialVault.cs`
- `src/Fenestra.Windows/Native/CredMan.cs` — P/Invoke `advapi32.dll`: `CredReadW`, `CredWriteW`, `CredDeleteW`, `CredFreeW`, `CredEnumerateW`, struct `CREDENTIAL`
- `src/Fenestra.Windows/Extensions/ServiceCollectionExtensions.cs` — `AddWindowsCredentialVault()`

**Padrões reutilizados:**
- P/Invoke centralizado em `Native/` (estilo `RegistryWatcher.cs`)
- `SafeHandle` para o ptr retornado por `CredRead` (liberar via `CredFree`)
- `FenestraComponent` base não é necessário (stateless)

**Gotchas:**
- `CREDENTIAL` contém `CredentialBlob` como `byte[]` de tamanho `CredentialBlobSize`; fazer marshalling cuidadoso (`Marshal.Copy`).
- Usar `CredentialType.Generic` (1) por padrão; documentar `DomainPassword` se alguém pedir.
- Prefixar `target` com `AppInfo.AppId` internamente pra namespace isolation.
- **Não** logar o secret em lugar nenhum (nem em exception message).

**Testes:** unit tests não dão — é puro Win32. Integration test opcional escrevendo/lendo um target dummy e limpando.

**TFM:** `Fenestra.Windows` (sem WPF).

---

### T1.2 — Power & Session Events

**Problema:** Apps de tray/background precisam reagir a sleep/resume/lock/unlock/battery. Hoje Fenestra não expõe nada. Polling é errado — Windows manda `WM_POWERBROADCAST` e `WM_WTSSESSION_CHANGE` para janelas registradas.

**API sketch:**
```csharp
public interface ISystemEventsService
{
    event EventHandler<PowerModeChangedEventArgs>? PowerModeChanged;   // Suspend/Resume
    event EventHandler<SessionChangedEventArgs>? SessionChanged;       // Lock/Unlock/Logon/Logoff
    event EventHandler<BatteryStatusChangedEventArgs>? BatteryChanged; // AC/Battery/LowPower

    PowerLineStatus CurrentPowerLineStatus { get; }
    bool IsSessionLocked { get; }
}

public enum PowerModeChange { Suspend, Resume, StatusChange }
public enum SessionChange { Lock, Unlock, Logon, Logoff, RemoteConnect, RemoteDisconnect }
```

**Registro:**
```csharp
builder.Services.AddWpfSystemEvents();
```

**Arquivos a criar:**
- `src/Fenestra.Windows/ISystemEventsService.cs` + event args
- `src/Fenestra.Windows.Wpf/Services/SystemEventsService.cs` — implementação com `HwndSource` message-only
- `src/Fenestra.Windows.Wpf/Native/NativeMethods.cs` — adicionar `WM_POWERBROADCAST=0x0218`, `WM_WTSSESSION_CHANGE=0x02B1`, `WTSRegisterSessionNotification`, `WTSUnRegisterSessionNotification`, `GetSystemPowerStatus`
- `src/Fenestra.Windows.Wpf/Extensions/WpfServiceCollectionExtensions.cs` — `AddWpfSystemEvents()`

**Padrões reutilizados:**
- `HwndSource` + `WndProc` hook, **exatamente** como `GlobalHotkeyService.cs:15-62`
- `FenestraComponent` base para disposal (unregister session notifications + release HwndSource)

**Gotchas:**
- WTS precisa de `WTSRegisterSessionNotification(hwnd, NOTIFY_FOR_THIS_SESSION)` após o HwndSource existir — **unregister** no Dispose é obrigatório (senão memory leak no winlogon).
- `WM_POWERBROADCAST` tem sub-eventos em `wParam`: `PBT_APMSUSPEND=0x4`, `PBT_APMRESUMESUSPEND=0x7`, `PBT_APMPOWERSTATUSCHANGE=0xA`. Mapear para enum.
- Service precisa ser **eagerly instanciado** (não lazy) senão ninguém registra os notifications. Adicionar ao hook de startup em `RunCore`/`OnStartup` via `services.GetService<ISystemEventsService>()` no momento do `OnReady`.
- Alternativa considerada: `Microsoft.Win32.SystemEvents` (já na BCL). Rejeitada porque cria seu próprio thread de mensagens e não integra com o Dispatcher WPF — casos reportados de events perdidos. Usar HwndSource direto é mais confiável.

**TFM:** `Fenestra.Windows.Wpf` (precisa de HwndSource).

---

### T1.3 — Idle Detection

**Problema:** "Usuário ficou ocioso por N minutos" é feature clássica de apps de produtividade/chat/gamificação. Win32 expõe `GetLastInputInfo` (user32) que retorna o tick do último input (mouse/teclado) global.

**API sketch:**
```csharp
public interface IIdleDetectionService
{
    TimeSpan IdleTime { get; }
    bool IsIdle { get; }  // true se IdleTime >= Threshold
    TimeSpan Threshold { get; set; }
    event EventHandler? BecameIdle;
    event EventHandler? BecameActive;
}

public sealed class IdleDetectionOptions
{
    public TimeSpan Threshold { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
}
```

**Registro:**
```csharp
builder.Services.AddWpfIdleDetection(opts => opts.Threshold = TimeSpan.FromMinutes(10));
```

**Arquivos a criar:**
- `src/Fenestra.Windows/IIdleDetectionService.cs`
- `src/Fenestra.Windows.Wpf/Services/IdleDetectionService.cs` — usa `DispatcherTimer` pra poll
- `src/Fenestra.Windows.Wpf/Native/NativeMethods.cs` — adicionar `GetLastInputInfo` + struct `LASTINPUTINFO`, `GetTickCount`
- Options class + extension `AddWpfIdleDetection(configure)`

**Padrões reutilizados:**
- Options pattern (ver `AddWpfMinimizeToTray` em `WpfServiceCollectionExtensions.cs:26-34`)
- `FenestraComponent` base; Dispose para timer

**Gotchas:**
- `GetTickCount` é 32-bit, envelope em ~49 dias. Comparar com `unchecked` subtraction pra tratar overflow.
- `DispatcherTimer` roda no UI thread — barato, não cria thread extra.
- Events `BecameIdle`/`BecameActive` devem ser raised no dispatcher (o timer já tá lá, OK).
- Default `PollInterval=5s` é o ponto doce: imperceptível e não polui CPU.

**TFM:** `Fenestra.Windows.Wpf` (usa `DispatcherTimer`). Alternativa: colocar em `Fenestra.Windows` com `System.Threading.Timer` e deixar o consumer fazer marshal — mais verboso. Fica em Wpf.

---

### T1.4 — Splash Screen (wrapper + custom)

**Problema:** Apps com carregamento longo (leitura de config, abrir DB, login inicial) precisam de feedback visual antes da `MainWindow` aparecer. WPF tem `System.Windows.SplashScreen` nativo mas é puramente estático (imagem); custom requer sincronização com o builder.

**API sketch (duas superfícies):**
```csharp
// Opção A: wrapper simples (imagem estática)
builder.UseSplashScreen("Assets/splash.png");  // embedded resource
// Fenestra cria SplashScreen nativo antes do Host start, chama Close() após OnReady.

// Opção B: splash customizado
builder.UseSplashScreen<MySplashWindow>();
// MySplashWindow : Window, ISplashScreen
// Fenestra instancia via reflection (não DI — antes do Host), Show(), e chama Close() após OnReady.

public interface ISplashScreen
{
    void Show();
    void Close();
    void ReportProgress(string status, double? percent = null);  // opcional
}
```

**Fluxo de startup modificado:**
```
CreateBuilder()
  └─> UseSplashScreen(...)  -> guarda SplashOptions
Build()
  └─> FenestraApplication
Run()
  └─> RunCore()
        ├─> CreateWpfApplication()
        ├─> ShowSplashIfConfigured()           <-- NOVO
        ├─> StartAsync() (Host)
        ├─> ResolveMainWindow()
        ├─> OnReady()
        ├─> mainWindow.Show()
        └─> CloseSplashIfConfigured()          <-- NOVO (fade out opcional)
```

**Arquivos a criar:**
- `src/Fenestra.Core/ISplashScreen.cs`
- `src/Fenestra.Windows.Wpf/Services/WpfSplashCoordinator.cs` — orquestra abrir/fechar
- `src/Fenestra.Windows.Wpf/Services/ImageSplashScreen.cs` — wrapper sobre `System.Windows.SplashScreen`
- `src/Fenestra.Windows.Wpf/Extensions/WpfFenestraBuilderExtensions.cs` — `UseSplashScreen(path)` e `UseSplashScreen<T>()`

**Arquivos a modificar:**
- `src/Fenestra.Windows.Wpf/FenestraApplication.cs` — hook em `RunCore` (~linha 185 e ~linha 212, antes/depois de OnReady)
- `src/Fenestra.Windows.Wpf/FenestraApp.cs` — mesmo hook no `OnStartup`

**Gotchas:**
- WPF `SplashScreen` nativo precisa do `.png` como `Resource` no csproj; documentar.
- Custom splash **não** pode ser resolvido via DI — o Host ainda não subiu quando o splash aparece. Instanciar via `Activator.CreateInstance`.
- Se o progress reporting for usado, o splash coordinator precisa expor uma referência alcançável durante a `Configure` do Host (via `IProgress<string>` no DI?) — ponto a decidir na implementação.
- Fechar splash deve ser **após** `mainWindow.Show()` para evitar flash de desktop.

**TFM:** `Fenestra.Windows.Wpf`.

---

## Tier 2 — Maior trabalho, alto valor visual/distribuição

### T2.1 — Mica/Acrylic Backdrop & Immersive Dark Mode

**Problema:** Win11 backdrop (Mica/Acrylic/Tabbed) + barra de título dark são o "look moderno" esperado. Hoje Fenestra não expõe nada — usuário tem que fazer P/Invoke por conta própria.

**API sketch:**
```csharp
public static class WindowChromeExtensions
{
    public static void EnableMica(this Window window, MicaVariant variant = MicaVariant.Auto);
    public static void EnableAcrylic(this Window window);
    public static void EnableTabbed(this Window window);
    public static void UseImmersiveDarkMode(this Window window, bool darkMode);
    public static void ExtendClientIntoTitleBar(this Window window);
}

public enum MicaVariant { Auto, Mica, MicaAlt }
```

**Uso:**
```csharp
public MainWindow()
{
    InitializeComponent();
    this.EnableMica();
    this.UseImmersiveDarkMode(true);  // ou bind com IThemeService existente
}
```

**Arquivos a criar:**
- `src/Fenestra.Windows.Wpf/Extensions/WindowChromeExtensions.cs`
- `src/Fenestra.Windows.Wpf/Native/DwmApi.cs` — `DwmSetWindowAttribute`, constantes `DWMWA_SYSTEMBACKDROP_TYPE=38`, `DWMWA_USE_IMMERSIVE_DARK_MODE=20`, `DWMWA_MICA_EFFECT=1029` (legacy Win10)

**Padrões reutilizados:**
- Apenas extension methods estáticos, sem DI. Não precisa de builder method.

**Gotchas:**
- Requer Windows 10 build 18985+ (dark mode) e Windows 11 build 22000+ (Mica). Checar via `Environment.OSVersion` ou RtlGetVersion.
- Mica só funciona com backdrop **transparente** — a janela precisa setar `Background = null` no XAML.
- Binding com `IThemeService` (já existe) permite auto-update quando usuário troca tema. Oferecer extension `BindToTheme(IThemeService)`.
- Em versões antigas de Windows, falhar silenciosamente (`hr != 0` → retornar false).
- Documentar que não é compatível com `WindowStyle=None` + custom chrome trivial.

**TFM:** `Fenestra.Windows.Wpf`.

---

### T2.2 — Jump Lists, Recent Files, Taskbar Overlay Icon

**Problema:** A família "taskbar integration" está meio-feita. `ITaskbarProvider`/`ITaskbarProgress` existem. Faltam: **JumpList** (custom tasks e recent files no right-click do ícone na taskbar), **overlay icon** (badge pequeno no ícone, ex.: contador de notificações).

**API sketch:**
```csharp
public interface IJumpListService
{
    void SetTasks(params JumpTask[] tasks);        // itens fixos tipo "Novo Doc", "Abrir pasta"
    void AddRecentFile(string path);               // adiciona à lista Recent
    void ClearRecentFiles();
    void Apply();                                  // commit — JumpList nativo precisa disso
}

public interface ITaskbarOverlayService
{
    void SetOverlay(string iconPath, string? accessibilityText = null);
    void SetOverlay(Icon icon, string? accessibilityText = null);
    void Clear();
}
```

**Registro:**
```csharp
builder.Services.AddWpfJumpList();
builder.Services.AddWpfTaskbarOverlay();
```

**Arquivos a criar:**
- `src/Fenestra.Windows.Wpf/IJumpListService.cs`, `ITaskbarOverlayService.cs`
- `src/Fenestra.Windows.Wpf/Services/JumpListService.cs` — usa `System.Windows.Shell.JumpList`
- `src/Fenestra.Windows.Wpf/Services/TaskbarOverlayService.cs` — usa COM `ITaskbarList3::SetOverlayIcon` (já temos infra COM — ver `NotificationActivatorServer`)
- Extensions + registrations

**Padrões reutilizados:**
- `System.Windows.Shell.JumpList` é .NET BCL — nada de P/Invoke pra Jump List.
- Para overlay icon: padrão COM como `NotificationActivatorServer.cs:18-36` (`CoCreateInstance(CLSID_TaskbarList)`).
- `AppInfo.AppId` (AUMID) é chave — sem AUMID correto, jump list não aparece. `AppShortcutManager` já garante AUMID no shortcut.

**Gotchas:**
- JumpList precisa que o app tenha um **shortcut com AUMID no Start Menu** — Fenestra já cria via `AppShortcutManager`. Dependência cruzada: `IJumpListService` deve falhar silenciosamente se shortcut não existir (ou chamar `AppShortcutManager.CreateOrUpdateShortcut()` preventivamente).
- Recent files precisam ser arquivos que o app **realmente abre** — se o usuário clicar e o app não aceitar arg, UX quebra. Combinar com Single Instance IPC (que já forwarda args).
- Overlay icon precisa de `HWND` da main window — pegar via `new WindowInteropHelper(mainWindow).Handle`. Service precisa aguardar main window existir. Usar `IWindowManager` ou hook em `OnReady`.
- Taskbar overlay tem max size ~16x16; documentar.

**TFM:** `Fenestra.Windows.Wpf`.

---

### T2.3 — Protocol Handler & File Association Registration

**Problema:** Registrar `myapp://` ou `.foo` files para abrir com o app hoje exige escrever no registry manualmente. Combina com Single Instance IPC (já existente) pra forwardar o arg.

**API sketch:**
```csharp
public interface IAppRegistrationService
{
    void RegisterProtocol(string scheme, string? friendlyName = null);
    void UnregisterProtocol(string scheme);
    bool IsProtocolRegistered(string scheme);

    void RegisterFileAssociation(string extension, string progId, string? friendlyName = null, string? iconPath = null);
    void UnregisterFileAssociation(string extension);
}
```

**Registro:**
```csharp
builder.Services.AddWindowsAppRegistration();
// Uso em OnReady ou em um primeiro-run handler:
registration.RegisterProtocol("myapp");
registration.RegisterFileAssociation(".foo", "MyApp.FooDoc");
```

**Arquivos a criar:**
- `src/Fenestra.Windows/IAppRegistrationService.cs`
- `src/Fenestra.Windows/Services/AppRegistrationService.cs`
- Extensions

**Padrões reutilizados:**
- Writes em `HKCU\SOFTWARE\Classes\...` — mesmo padrão que `ToastActivationRegistrar.cs:75-80`:
  ```csharp
  var keyPath = $@"SOFTWARE\Classes\{scheme}\shell\open\command";
  using var key = Registry.CurrentUser.CreateSubKey(keyPath);
  key.SetValue(null, $"\"{exePath}\" \"%1\"");
  ```
- `AppInfo` para obter `ExePath` consistente
- Single Instance IPC existente cuida do forwarding do `%1` pra instância rodando

**Gotchas:**
- **HKCU, não HKLM** — não requer admin, mas só afeta o usuário atual. Documentar.
- Para file association completa: `HKCU\Software\Classes\.{ext}` → default = progId; `HKCU\Software\Classes\{progId}\shell\open\command` → command line; `HKCU\Software\Classes\{progId}\DefaultIcon` → icon.
- Depois de registrar, chamar `SHChangeNotify(SHCNE_ASSOCCHANGED, ...)` pra Explorer atualizar.
- Protocol handlers precisam de value `"URL Protocol"=""` (vazio) na raiz do scheme pra Windows reconhecer.
- Garantir que o `ISingleInstanceApp.OnArgumentsReceived` do usuário trata o caso do arg ser uma URL `myapp://...` — documentar com exemplo.

**TFM:** `Fenestra.Windows` (puro registry, sem WPF).

---

### T2.4 — First-Run & Upgrade Detection

**Problema:** Padrão clássico: "é a primeira vez que esse usuário roda o app?" (mostrar onboarding) e "o usuário atualizou de uma versão anterior?" (migrar settings, mostrar changelog). Hoje cada dev faz manualmente gravando uma flag no registry.

**API sketch:**
```csharp
public interface IAppLifecycleService
{
    bool IsFirstRun { get; }                      // true na primeira execução EVER
    bool IsFirstRunOfVersion { get; }              // true na primeira execução desta versão
    Version? PreviousVersion { get; }              // null se IsFirstRun
    DateTimeOffset FirstInstallDate { get; }       // persistida desde a primeira vez
    int LaunchCount { get; }                       // incrementa a cada startup
}
```

**Registro:**
```csharp
builder.Services.AddWindowsAppLifecycle();
// Uso em OnReady:
if (lifecycle.IsFirstRun) ShowOnboarding();
else if (lifecycle.IsFirstRunOfVersion) ShowChangelog(lifecycle.PreviousVersion);
```

**Arquivos a criar:**
- `src/Fenestra.Windows/IAppLifecycleService.cs`
- `src/Fenestra.Windows/Services/AppLifecycleService.cs`
- Extension

**Padrões reutilizados:**
- `IRegistryConfig` — salvar numa section `"Lifecycle"` com `FirstInstallDate`, `LastVersion`, `LaunchCount`. Ver `RegistryWindowPositionStorage.cs:1-68` como exemplo de section.
- `AppInfo.Version` como referência

**Gotchas:**
- Service precisa ser **eagerly instantiated** e ler+gravar no constructor (ou em um `Initialize()` chamado no OnReady). Lazy não serve porque o dev pode não resolvê-lo a tempo.
- Incrementar `LaunchCount` antes do dev ler, para que `LaunchCount == 1` signifique "primeira vez".
- `IsFirstRunOfVersion` calcula `AppInfo.Version != stored.LastVersion`. Após leitura, atualizar stored.
- Documentar que reinstalar o app (sem limpar HKCU) **não** zera IsFirstRun — é by design.

**TFM:** `Fenestra.Windows`.

---

## Tier 3 — Arquitetural

### T3.1 — Strongly-Typed Settings (IOptionsMonitor<T> + RegistryWatcher)

**Problema:** `IRegistryConfig` já tem `[RegistrySection]` com reflection read/write, mas é imperativo (`config.GetSection<T>()`). Devs .NET esperam `IOptions<T>`/`IOptionsMonitor<T>` com hot-reload. Decisão do usuário: integrar com o padrão MS.

**API sketch:**
```csharp
builder.Services.AddRegistrySettings<MyAppSettings>();
// → resolve como IOptions<MyAppSettings>, IOptionsSnapshot<MyAppSettings>, IOptionsMonitor<MyAppSettings>

[RegistrySection]
public class MyAppSettings
{
    public string ServerUrl { get; set; } = "https://default";
    public int RefreshSeconds { get; set; } = 30;
    public WindowSection Window { get; set; } = new();
}

// Uso:
public class MyService(IOptionsMonitor<MyAppSettings> settings)
{
    void DoWork()
    {
        var url = settings.CurrentValue.ServerUrl;
        settings.OnChange(s => _logger.Info("Settings changed: {Url}", s.ServerUrl));
    }
}
```

**Arquivos a criar:**
- `src/Fenestra.Windows/Settings/RegistryConfigurationSource.cs` : `IConfigurationSource`
- `src/Fenestra.Windows/Settings/RegistryConfigurationProvider.cs` : `ConfigurationProvider`
- `src/Fenestra.Windows/Extensions/ServiceCollectionExtensions.cs` — `AddRegistrySettings<T>()`

**Padrões reutilizados:**
- `IRegistryConfig` para leitura reflection-based (já existe)
- `RegistryWatcher` (`src/Fenestra.Windows/Services/RegistryWatcher.cs`) para hot reload — dispara `OnReload()` do `ConfigurationProvider`
- `Microsoft.Extensions.Configuration` + `Microsoft.Extensions.Options` — BCL padrão

**Escolha de design:**
Implementar como **ConfigurationProvider** (não como direct IOptions binding). Vantagem: o mesmo `T` fica disponível via `IConfiguration`, `IOptions<T>`, `IOptionsMonitor<T>` de graça. `ConfigurationProvider` carrega valores do registry como pares `key=value` achatados, e o bind do MS cuida do resto.

**Fluxo:**
```
AddRegistrySettings<T>()
  ├─> services.Configure<T>(config.GetSection(sectionName))
  ├─> registra RegistryConfigurationProvider em IConfigurationBuilder
  └─> RegistryWatcher inicia; ao disparar, ConfigurationProvider.Load() + OnReload() → IOptionsMonitor notifica
```

**Gotchas:**
- Sections aninhadas: flatten para `Parent:Child:Value` (padrão `IConfiguration`). Reflection já faz recursão em `RegistryConfigService.cs:188-210` — adaptar pra flatten em vez de sub-objects.
- Write-back: `IOptions` é read-only por design. Escrita continua via `IRegistryConfig`. Documentar que `AddRegistrySettings<T>()` é read + watch only; pra escrever, injetar `IRegistryConfig`.
- Hot-reload debounce: `RegistryWatcher` pode disparar em rajada — adicionar debounce de ~200ms. (Já é comum em provider do MS com `reloadDelay`.)
- Incluir `RegistryConfigurationProvider` **antes** do `appsettings.json` na chain (ou depois? documentar prioridade — provavelmente depois, pra registry sobrescrever appsettings).

**TFM:** `Fenestra.Windows`.

---

### T3.2 — Localization

**Problema:** Apps multi-idioma em WPF usam `.resx` + `CultureInfo.CurrentUICulture`, mas falta plumbing pra persistir escolha do usuário e trocar em runtime. Fenestra pode oferecer helper mínimo.

**API sketch:**
```csharp
public interface ILocalizationService
{
    CultureInfo CurrentCulture { get; }
    IReadOnlyList<CultureInfo> SupportedCultures { get; }
    void SetCulture(CultureInfo culture);                 // persiste + dispara event
    event EventHandler<CultureChangedEventArgs>? CultureChanged;
}
```

**Registro:**
```csharp
builder.Services.AddWindowsLocalization(opts =>
{
    opts.Supported = new[] { "en-US", "pt-BR", "es-ES" };
    opts.Default = "en-US";
});
```

**Arquivos a criar:**
- `src/Fenestra.Core/ILocalizationService.cs`
- `src/Fenestra.Windows/Services/LocalizationService.cs`
- `src/Fenestra.Windows/LocalizationOptions.cs`
- Extension

**Padrões reutilizados:**
- `IRegistryConfig` pra persistir `SelectedCulture`
- `CultureInfo.CurrentCulture`/`CurrentUICulture` — BCL standard

**Escolha de design:**
Fenestra **não** vai implementar binding XAML de strings localizadas (território de MarkupExtension, muita lib especializada já faz isso — ex.: `WPFLocalizeExtension`). Só fornece:
1. Detectar/persistir/trocar cultura
2. Event pra app re-renderizar
3. Setar `Thread.CurrentThread.CurrentUICulture` em todos os threads WPF após troca

**Gotchas:**
- Trocar cultura em runtime **não** atualiza janelas abertas automaticamente. Documentar que o app deve re-criar views após `CultureChanged`, ou usar uma lib de binding.
- Persistir cultura **antes** de mostrar `MainWindow` — ler no startup e aplicar. Ponto de hook: antes de `ResolveMainWindow` em `RunCore`.
- Fallback: se cultura persistida não está em `Supported`, usar `Default`.
- Evitar dep cíclica com `IRegistryConfig` (service está em `Fenestra.Windows`, `IRegistryConfig` também — OK).

**TFM:** `Fenestra.Windows` (precisa de `IRegistryConfig`). Interface em `Fenestra.Core` pra permitir implementações alternativas.

---

## Ação de Verificação — Single Instance IPC

**NÃO é uma nova feature**, mas o README não documenta o comportamento:

- Verificar `SingleInstanceGuard.cs` end-to-end — confirmar que args chegam mesmo em todos os cenários (launch-via-protocol, launch-via-file-association, launch-via-jumplist).
- Adicionar seção no README / doc `docs/single-instance.md` explicando como implementar `ISingleInstanceApp.OnArgumentsReceived`.
- Opcional: adicionar sample em `samples/` demonstrando recepção de args.

Este item fica fora do roadmap de implementação mas é pré-requisito para documentar com honestidade as features T2.2 (jump lists), T2.3 (protocol/file association).

---

## Ordem de Execução Recomendada

Do menos dependente pro mais dependente:

1. **T1.1 Credential Vault** — isolado, zero deps
2. **T1.3 Idle Detection** — isolado, zero deps
3. **T1.2 Power & Session Events** — isolado, introduz padrão Hwnd-based reutilizável
4. **T2.1 Mica/Acrylic** — isolado, zero deps; pode ser feito a qualquer momento
5. **T2.4 First-Run Detection** — depende só de `IRegistryConfig` (já existe)
6. **T3.1 Strongly-Typed Settings** — depende do RegistryWatcher/IRegistryConfig; fundacional pro resto
7. **T3.2 Localization** — depende de T3.1 ou direto de IRegistryConfig
8. **T1.4 Splash Screen** — mexe no ciclo de vida do `FenestraApplication`/`FenestraApp`; fazer quando a superfície core estiver estável
9. **Verificação Single Instance IPC** (doc-only)
10. **T2.3 Protocol Handler & File Association** — depende de Single Instance IPC (já existe, mas precisa estar documentado)
11. **T2.2 Jump Lists & Overlay Icon** — depende de T2.3 (recent files precisa de file association coerente) + `AppShortcutManager` existente

## Verification (end-to-end, a ser repetido por feature)

Para cada feature implementada:

1. **Build:** `dotnet build Fenestra.slnx` — garantir que todas as TFMs (net472, net6.0, net6.0-windows) compilam.
2. **Unit tests:** onde possível, adicionar em `tests/Fenestra.Tests/`. Features que são puro P/Invoke (CredentialVault, PowerEvents) podem não ter unit tests — OK, integration testing manual.
3. **Integration em sample:** adicionar uso em `samples/Fenestra.Sample.AppStyle` ou `samples/Fenestra.Sample.BuilderStyle` — serve como smoke test e documentação viva.
4. **Doc:** criar `docs/{feature}.md` no mesmo estilo dos existentes (`docs/global-hotkeys.md`, `docs/tray-icon.md`).
5. **README:** adicionar linha na tabela de Features + corrigir link pra `./docs/{feature}.md` (lembrar que os links atuais do README estão quebrados — `./foo.md` em vez de `./docs/foo.md`; bug pré-existente que vale consertar oportunisticamente).

## Critérios de Sucesso do Roadmap

- Nenhuma feature quebra compat: builder methods existentes continuam funcionando.
- Todas as novas features são **opt-in** (nenhum `Add*()` é chamado por default em algum layer do builder).
- Cada nova feature tem TFM mínima: se não usa WPF, vai em `Fenestra.Windows`, nunca em `Fenestra.Windows.Wpf`.
- Zero novas dependências NuGet externas (tudo em `Microsoft.Extensions.*` já referenciado ou BCL + Win32).
- Cada feature tem sample funcionando + doc.md + linha no README.
