# Give-AID – NGO Website (Aptech eProject)

ASP.NET Core MVC 8 (C#) + SQL Server (Entity Framework Core 8) — **.NET 8 / Visual Studio 2022** ke liye

Give-AID ek NGO umbrella website hai: log cause choose karke donate karte hain (dummy card payment ke saath), register karte hain, programmes join karte hain, dosto ko invite karte hain aur queries bhejte hain. Admin panel se poori website ka content manage hota hai.

---

## 1. Kya install hona chahiye

| Software | Version |
|---|---|
| Visual Studio 2022 (17.8 ya naya) | koi bhi edition (Community free hai), **"ASP.NET and web development"** workload ke saath |
| .NET SDK | 8.0 (Visual Studio 2022 ke saath khud aa jata hai) |
| SQL Server | **LocalDB** (Visual Studio ke saath aata hai) ya SQL Server Express |
| Internet | Sirf pehli dafa NuGet packages download karne ke liye |

Website ki saari CSS, JavaScript, icons aur fonts `wwwroot/lib` mein hain, isliye chalne ke liye internet zaroori nahi.

## 2. Run kaise karein (Visual Studio)

1. RAR/Zip extract karein aur **`GiveAid.sln`** double-click karke kholein.
2. Visual Studio khud NuGet packages restore karega (EF Core SqlServer). Agar na kare to: *Solution par right-click → Restore NuGet Packages*.
3. **F5** (ya green "GiveAid" button) dabayein.
4. Browser mein website khul jayegi: `https://localhost:7070`

**Pehli dafa chalne par** database `GiveAidDB` khud ban jata hai aur usmein sample data aa jata hai (admin, 6 causes, NGOs, programmes, partners, gallery, 3 members, donations). Koi SQL script run karne ki zaroorat nahi.

### Command line se (optional)
```
cd GiveAid
dotnet run
```

## 3. Login details

| Kaun | Kahan | Username / E-mail | Password |
|---|---|---|---|
| Admin | `/Admin` (footer mein "Admin panel" link) | `admin` **ya** `admin@giveaid.org` | `Admin@123` |
| Member | `/Account/Login` | `ayesha@example.com` | `Member@123` |
| Member | | `usman@example.com`, `mariam@example.com` | `Member@123` |

**Test cards** (dummy payment, koi paisa nahi katta):

| Card | Number | Expiry | CVV |
|---|---|---|---|
| Visa | 4111 1111 1111 1111 | koi bhi future date, e.g. 12/30 | 123 |
| MasterCard | 5555 5555 5555 4444 | 12/30 | 123 |
| American Express | 3782 822463 10005 | 12/30 | 1234 |

Galat number (e.g. 4111 1111 1111 1112), purani expiry ya galat CVV dein to validation error aayega — browser (JavaScript) mein bhi aur server (C#) par bhi.

## 4. Database / connection string

`appsettings.json` mein:

```json
"ConnectionStrings": {
  "GiveAidDB": "Server=(localdb)\\MSSQLLocalDB;Database=GiveAidDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

Agar aapke paas **SQL Server Express** hai to `Server=` badal dein:

```
Server=.\\SQLEXPRESS;Database=GiveAidDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

SQL login (username/password) wala server ho to:
```
Server=YOUR_SERVER;Database=GiveAidDB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True
```

- **`Database/GiveAidDB.sql`** – poore database ka script (12 tables, primary/foreign keys, indexes). SSMS mein manually database banana ho to yeh run karein, phir website chalayein – seeder data daal dega.
- **Database reset karna ho**: SSMS mein `GiveAidDB` delete kar dein; agli dafa website chalegi to naya ban jayega.

### Submission ke liye database backup (.bak)
Project Specification kehta hai database backup bhi submit karna hai. Website ek dafa chala lein, phir SSMS mein:

*GiveAidDB par right-click → Tasks → Back Up… → Destination: Disk → OK*

ya yeh query:
```sql
BACKUP DATABASE GiveAidDB TO DISK = 'C:\Backup\GiveAidDB.bak' WITH FORMAT, INIT;
```
LocalDB ke liye SSMS mein server name `(localdb)\MSSQLLocalDB` likh kar connect karein.

## 5. E-mail (Invite friends / query reply) – optional

`appsettings.json` → `"Smtp"` section. Default mein `Host` khaali hai, is surat mein:
- invitation database mein save hoti hai aur member ko **"Open it in your e-mail app"** link milta hai (mailto), jo unke apne email app mein ready-made mail khol deta hai;
- admin ki reply database mein save hoti hai aur member ko dashboard / Help Centre mein nazar aati hai.

Real email bhejni ho (Gmail example – Google account mein "App password" banana padega):
```json
"Smtp": {
  "Host": "smtp.gmail.com", "Port": 587, "EnableSsl": true,
  "UserName": "yourname@gmail.com", "Password": "your-16-char-app-password",
  "FromEmail": "yourname@gmail.com", "FromName": "Give-AID"
}
```

## 6. Requirements → kahan implement hua

### Home page menus
Home, Donate, Help Centre, About us (drop-down), Our Partners, Register — sab top menu mein hain (`Views/Shared/_Layout.cshtml`). Saath mein Programmes, Gallery, Contact us bhi. Home page par koi khaali jagah nahi: hero + quick donation, causes grid, upcoming programmes, latest donations, gallery, partners, invite band.

### ADMIN module (`/Admin`)
| Requirement | Page / file |
|---|---|
| Donation list & categories manage karna | Admin → **Causes** (add/edit/delete, goal, active/hidden) – `Areas/Admin/Controllers/CausesController.cs` |
| Partner companies + logos | Admin → **Partners** (logo upload) – `PartnersController.cs` |
| About us: What we do, Our mission, Our team, Career with us, Our achievements, Our supporters, Read about us – editable | Admin → **Pages & content** – `PagesController.cs` (Home intro, Help Centre, Contact us bhi) |
| Contact us | Admin → Pages & content → Contact us (text), messages Admin → **Queries** mein aate hain |
| View registered members | Admin → **Members** (search, details, donations, invitations, enable/disable) |
| Donate link: amount + cause | Admin → **Donations** (cause/date filter, total) |
| List of other associated NGOs | Admin → **Associated NGOs** (add/edit/delete) |
| Programmes: Education, Health Care, Specially challenged children… | Admin → **Programmes** (image upload, category, NGO, dates) |
| Reply to queries | Admin → **Queries** → Reply |
| Gallery upload | Admin → **Gallery** |
| Interests for upcoming programmes | Admin → **Programme interests** |

### USER module
| Requirement | Page / file |
|---|---|
| Register (personal + professional data) | `/Account/Register` – `AccountController.cs` |
| Donate to any cause, dummy card payment with proper validation | `/Donate` – `DonateController.cs`, `Helpers/CardValidator.cs`, `wwwroot/js/donate.js` |
| Invite friends by mail | `/Invite` (login zaroori) – `InviteController.cs` |
| Raise queries | `/Help` aur `/Home/Contact` – `HelpController.cs`, `HomeController.cs` |
| Send interest for upcoming programmes | `/Programmes/Details/{id}` → "I want to take part" – `ProgrammesController.cs` |
| Programme gallery | `/Gallery` |
| My account (donation history, receipts, replies) | `/Account/Dashboard` |

### Card validation (dummy payment)
- Card brand pehchaan (Visa, MasterCard, American Express, Discover, UnionPay)
- Har brand ke liye sahi length (Amex 15, Visa 13/16/19 …)
- **Luhn (mod 10) check-digit** algorithm
- Expiry `MM/YY`, expired card reject
- CVV: Amex 4 digit, baaqi 3 digit
- Name on card sirf letters
- Amount Rs 100 – Rs 5,000,000
- Security: **poora card number aur CVV kabhi database mein save nahi hota**, sirf brand + last 4 digits. Receipt par unique transaction number (`GA-yyyyMMdd-XXXXXX`).

## 7. Security points (viva ke liye)
- Passwords **PBKDF2-SHA256** (100,000 iterations + random salt) se hash – `Helpers/PasswordHasher.cs`
- Admin aur member ki **alag login cookies** (`AdminCookie`, `MemberCookie`)
- Har POST form par **Anti-forgery token** (`[ValidateAntiForgeryToken]`)
- `[Bind]` se over-posting se bachao
- Upload: sirf JPG/PNG/GIF/WEBP, max 5 MB, random file name
- Admin ke page text HTML-encode hote hain (script injection nahi ho sakta)
- Login ke baad sirf local URL par redirect (open-redirect protection)
- Donation ke baad Post/Redirect/Get – refresh karne se dobara donation nahi hoti

## 8. Folder structure

```
GiveAid/
├── Program.cs                 startup: database, login cookies, routes
├── appsettings.json           connection string, SMTP
├── Models/                    12 database tables (entities)
│   └── ViewModels/            form models with validation rules
├── Data/
│   ├── GiveAidContext.cs      EF Core DbContext (tables + relationships)
│   └── DbSeeder.cs            sample data on first run
├── Helpers/                   password hashing, card validator, file upload, text formatter
├── Services/EmailSender.cs    SMTP mail
├── Controllers/               USER module (Home, About, Account, Donate, Programmes, Ngos, Partners, Gallery, Help, Invite)
├── Areas/Admin/               ADMIN module (controllers + views)
├── Views/                     USER module pages (Razor .cshtml)
├── wwwroot/
│   ├── css/theme3d.css (website), site.css + admin.css (admin panel)
│   ├── js/fx.js, donate.js, site.js (+ .min.js), js/dist/ (bundled 3D)
│   ├── images/                sample programme, gallery and partner images
│   ├── uploads/               images uploaded from admin panel
│   └── lib/                   three.js, Bootstrap 5.3, Bootstrap Icons, jQuery, validation, fonts
└── Database/GiveAidDB.sql     SQL Server script
```

## 9. 3D theme (frontend)

Public website ka "Lantern Night" heavy 3D theme hai (admin panel simple light theme mein hai):

| Effect | Kahan | File |
|---|---|---|
| WebGL hero: 7,000 roshni ke particles ka sculpture jo **heart → globe → 6 cause clusters** mein morph hota hai, beech mein chamakta 3D heart, 6 orbiting orbs (trails ke saath), upar uthti lanterns, **bloom glow** | Home | `ClientSrc/3d/hero3d.js` |
| Hero interaction: mouse ke paas particles hat jaate hain; scene par click karein to sculpture blast ho kar wapas judta hai; scroll par bikhar jata hai | Home | `hero3d.js` |
| Poori website ke peeche fixed 3D space: tairte low-poly crystals, rings aur hearts, scroll par parallax | Har page | `ClientSrc/3d/space3d.js` |
| WebGL particle wave | Har inner page ka header | `ClientSrc/3d/wave3d.js` |
| Headline ke letters 3D mein flip ho kar aate hain | Har page ka heading | `wwwroot/js/fx.js` |
| 3D page transition (page palat kar agla page aata hai) | Chrome / Edge | `theme3d.css` (View Transitions) |
| 3D tilt + glare cards, 3D scroll reveal, animated counters | Poori website | `fx.js` |
| 3D gallery ring (drag / arrow keys), tilted logo marquee, ghoomta cube | Home | `fx.js`, `theme3d.css` |
| Live 3D credit card (CVV par palat-ta hai) + amount ke hisaab se 3D sikkon ka stack | Donate | `wwwroot/js/donate.js` |
| Coin animation + confetti | Receipt | `fx.js` |
| Extruded 3D "404" | Not found page | `theme3d.css` |

- **three.js** ke sirf istemal hone wale hisse `wwwroot/js/dist` mein bundle hain (bloom add-ons ke saath), internet ki zaroorat nahi.
- Achhe graphics card wale PC par 60 fps chalta hai. Scene screen se bahar ho ya tab hidden ho to animation ruk jati hai.
- Physics frame-rate se independent hai, isliye slow PC par bhi shapes sahi banti hain (sirf kam smooth).
- OS mein "Reduce motion" on ho to animations band ho jati hain; WebGL na chale to page normal dikhta hai.

## 11. AI Programme Planner (`/Admin/Planner`)

Admin ek line likhta hai (English ya Roman Urdu), jaise
*"Free eye camp in Mithi for 1,500 patients, second week of November"*.
Planner khud yeh sab check karta hai:

1. Goal samajhna: category, jagah, dates, kitne log, zaban.
2. Sahi NGO dhoondhna (pichle programmes + focus area + province).
3. Calendar check: un dates par koi aur programme to nahi.
4. Mausam: Open-Meteo, free hai aur key nahi chahiye. Internet na ho to "Please check" dikhata hai.
5. Volunteers dhoondhna. AI ko sirf naam/shehar jata hai, email/phone nahi.
6. Budget: pichle programmes ke asli cost lines se, per person hisaab.
7. **Proposal:** draft programme page aur volunteer invitations.

**Human-in-the-loop:** read steps khud chalte hain. Lekin program banana aur
e-mail bhejna sirf admin ke **Approve** dabane par hota hai.
Approve hone par programme **hidden draft** banta hai (`IsPublished = false`).
Website par sirf "Publish now" ke baad dikhta hai. Invitations sirf published
programme ke liye jati hain. Har step `PlannerSteps` table mein save hota hai
(audit trail: kis ne, kab approve/reject kiya).

### Modes (`appsettings.json` → `Planner`)

| Provider | Kya chahiye | Note |
|---|---|---|
| `Demo` (default) | Kuch nahi | Built-in rules, offline chalta hai. Viva ke liye best. |
| `Claude` | `Planner:Claude:ApiKey` | Anthropic Messages API + tool use. Default model `claude-sonnet-5-5`. |
| `OpenAI` | `BaseUrl` + `Model` (+ key) | Koi bhi OpenAI-compatible server: OpenAI, Groq, OpenRouter, ya local **Ollama** (free, key nahi). |

Agar key/model khali ho to planner khud Demo mode par aa jata hai aur page par batata hai.

**API key kabhi appsettings.json mein commit na karein.** User-secrets use karein:

```
cd GiveAid
dotnet user-secrets init
dotnet user-secrets set "Planner:Provider" "Claude"
dotnet user-secrets set "Planner:Claude:ApiKey" "sk-ant-..."
```

Free option (local Ollama):

```
ollama pull llama3.1
dotnet user-secrets set "Planner:Provider" "OpenAI"
dotnet user-secrets set "Planner:OpenAI:BaseUrl" "http://localhost:11434/v1"
dotnet user-secrets set "Planner:OpenAI:Model" "llama3.1"
```

### Naye tables / columns

* `Programmes.IsPublished`, `Programmes.Beneficiaries`
* `ProgrammeCostLines`: budget ke liye. Seeder sample data bhi dalta hai.
* `PlannerSessions`, `PlannerSteps`

Purana database ho to bhi kuch karna nahi parta. `Data/SchemaUpgrader.cs`
app start par missing columns/tables khud bana deta hai.
`Database/GiveAidDB.sql` bhi update hai.

### Viva points

* **Agentic AI:** model khud tools choose karta hai (loop). Har tool call validate hota hai. Ghalat input par error wapas model ko jata hai aur woh theek karta hai. `MaxToolCalls` ek limit hai.
* **Safety:** write actions sirf proposals hain. Admin approve karta hai. Drafts hidden rehte hain.
* **Prompt injection guard:** DB/web ka data "data" maana jata hai, instructions nahi.
* **Privacy:** volunteers ka email/phone AI ko nahi bheja jata.
* **Honest numbers:** budget pichle asli kharchon se aata hai, AI apne se nahi banata.
* **Code:** `Services/Planner/*`, `Areas/Admin/Controllers/PlannerController.cs`, `Areas/Admin/Views/Planner/*`.

## 12. "Nova" admin theme (futuristic backend)

Admin panel ab default **Nova** night theme mein khulta hai:

* **3D constellation background**: `ClientSrc/3d/admin3d.js` (three.js). Har point ek donor/volunteer, lines NGOs. Tab chhupa ho to ruk jata hai; "reduce motion" wale users ko still frame milta hai.
* **Glass panels**: `wwwroot/css/admin-nova.css`. Sab rules `html.nova` ke andar hain, is liye purana light look bhi bacha hai.
* **Day / Night button** (top bar): choice browser mein yaad rehti hai.
* **Command palette**: `Ctrl + K` (ya `/`) dabayein, likhein "programme", Enter. Har page aur action (Plan with AI, Add programme, Add NGO...) yahin se.
* **Live clock** (Karachi time) top bar mein.
* **Dashboard entrance**: session mein pehli dafa numbers count-up hote hain aur chart ki bars upar uthti hain.
* **Login page**: 3D galaxy ke saamne glass login card, ek scan-line animation.

Files: `wwwroot/css/admin-nova.css`, `wwwroot/js/admin-nova.js`, `ClientSrc/3d/admin3d.js` (bundled to `wwwroot/js/dist`),
`Areas/Admin/Views/Shared/_NovaHead.cshtml` (theme flash se bachata hai).

## 13. Pictures aur compression (chhota size, tez website)

**Nayi pictures:** gallery (`wwwroot/images/gallery/g1-g8.webp`) aur programmes
(`wwwroot/images/programmes/*.webp`) ab 3D-rendered "photos" hain, WebP format mein
(har ek sirf 11-27 KB). Purane database mein `.svg` raste hon to `SchemaUpgrader`
unhein khud `.webp` kar deta hai. Asli photos lagani hon: Admin → Gallery → Upload photo,
ya Admin → Programmes → Edit → Picture.

**Compression jo lagi hai:**

| Kya | Pehle | Ab |
|---|---|---|
| Sample pictures | SVG drawings | WebP, total ~280 KB |
| three.js | poori library 672 KB | sirf istemal hone wale hisse, 512 KB (`wwwroot/js/dist`) |
| Apni CSS (live site) | 4 files | 1 minified file har area ke liye (`public.min.css`, `admin.min.css`) |
| Apni JS (live site) | readable | `*.min.js` |
| Source maps (.map), icon font ka purana .woff | ~1 MB | hata diye |
| Har page/CSS/JS ka transfer | bina compression | **Brotli/Gzip** (`Program.cs`), ~75% kam |
| Browser cache | nahi | 30 din (`asp-append-version` se update fauran milta hai) |

**Development vs live:** Visual Studio mein (Development) readable `css/*.css` aur `js/*.js` load hoti hain,
taake aap asani se edit kar sakein. Publish/Production mein `.min` files.
CSS/JS ya 3D code (`ClientSrc/3d`) badlein to dobara banane ke liye (Node.js chahiye):

```
cd GiveAid/ClientSrc
npm install
npm run build
```

## Admin login nahi ho raha?

1. Address `https://localhost:7070/Admin` kholein (footer ka "Admin panel" link).
2. Username `admin` **ya** email `admin@giveaid.org` (capital/small letter se farq nahi padta).
3. Password bilkul `Admin@123` — capital **A**, aakhir mein `@123`. Aankh (👁) wale button se dekh lein ke sahi type hua.
4. Phir bhi na ho to admin account reset karein: SSMS mein `(localdb)\MSSQLLocalDB` se connect karke
   ```sql
   USE GiveAidDB; DELETE FROM Admins;
   ```
   chalayein, phir website dobara F5 se chalayein — default admin (`admin` / `Admin@123`) khud dobara ban jayega.

## 10. Common problems

| Problem | Hal |
|---|---|
| *"A network-related or instance-specific error… SQL Server"* | LocalDB installed nahi / chal nahi raha. Visual Studio Installer → Individual components → "SQL Server Express LocalDB" install karein, ya connection string SQLEXPRESS par badlein. |
| NuGet restore error | Internet check karein, phir *Tools → NuGet Package Manager → Package Manager Settings → Clear All NuGet Cache(s)* aur dobara build. |
| HTTPS certificate warning | Pehli dafa Visual Studio certificate trust karne ka poochta hai – "Yes" karein. |
| Sample images ki jagah apni photos | Admin panel se Programmes / Gallery / Partners mein upload karein. |
| Database mein koi column change kiya | Purana `GiveAidDB` delete karein, website dobara chalayein. |

## Documentation

The `Documentation` folder next to the solution contains:

- `GiveAid_Project_Report.docx` – eProject report (requirements, design, ERD, use case, DFDs, site map, algorithms, GUI standards, screens, testing, checklists, glossary)
- `GiveAid_User_Guide.docx` – step-by-step guide for visitors, members and administrators
- `GiveAid_Presentation.pptx` – viva presentation with animations and speaker notes (Roman Urdu)

## Deploying online (free) – MonsterASP.NET

1. Create a free account at https://www.monsterasp.net and create a **website** (you get a `*.runasp.net` address) and an **MSSQL database**.
2. Copy `GiveAid/appsettings.Production.example.json` to `appsettings.Production.json` and put the database server, name, user and password in it. On MonsterASP create this file in the website's wwwroot with the File Manager; it is not in the repository, so publishing never overwrites it.
3. In the control panel of the website, open **Deploy (WebDeploy)** and download the publish profile (`.publishsettings`).
4. In Visual Studio: right-click the **GiveAid** project → **Publish** → **Import Profile** → choose the downloaded file → **Publish**.
5. Open your `runasp.net` address. On the first visit the tables and sample data are created automatically. Admin: `/Admin` – admin / Admin@123 (change it after the first login).
6. Optional: ask MonsterASP support to switch on HTTPS for the free site.
