using GiveAid.Helpers;
using GiveAid.Models;

namespace GiveAid.Data
{
    /// <summary>
    /// Inserts starting data the first time the website runs, so every page has content.
    /// Each block runs only if its table is empty, so admin changes are never overwritten.
    ///
    /// Default logins:
    ///   Admin  : admin / Admin@123            (URL: /Admin)
    ///   Member : ayesha@example.com / Member@123
    /// </summary>
    public static class DbSeeder
    {
        public static void Seed(GiveAidContext db)
        {
            SeedAdmin(db);
            SeedCauses(db);
            SeedPages(db);
            SeedPartners(db);
            SeedNgos(db);
            SeedProgrammes(db);
            SeedGallery(db);
            SeedMembersAndActivity(db);
            SeedProgrammeCosts(db);
        }

        // ------------------------------------------------------------------
        private static void SeedAdmin(GiveAidContext db)
        {
            // If the default "admin" account exists but its password data is damaged
            // (for example edited by hand in SQL Server), reset it to Admin@123.
            var existing = db.Admins.FirstOrDefault(a => a.Username == "admin");
            if (existing != null)
            {
                if (existing.PasswordHash.Split('.').Length != 3)
                {
                    existing.PasswordHash = PasswordHasher.Hash("Admin@123");
                    db.SaveChanges();
                }
                return;
            }
            if (db.Admins.Any()) return;   // other admin accounts exist: leave them alone
            db.Admins.Add(new Admin
            {
                FullName = "Site Administrator",
                Username = "admin",
                Email = "admin@giveaid.org",
                PasswordHash = PasswordHasher.Hash("Admin@123")
            });
            db.SaveChanges();
        }

        // ------------------------------------------------------------------
        private static void SeedCauses(GiveAidContext db)
        {
            if (db.Causes.Any()) return;
            db.Causes.AddRange(
                new Cause { Name = "Children", Icon = "balloon-heart", GoalAmount = 1500000, DisplayOrder = 1,
                    Description = "Meals, winter clothing and safe shelter for street-connected and orphaned children." },
                new Cause { Name = "Education", Icon = "book", GoalAmount = 2500000, DisplayOrder = 2,
                    Description = "School fees, books and trained teachers for children who would otherwise leave school." },
                new Cause { Name = "Disabled", Icon = "universal-access", GoalAmount = 1200000, DisplayOrder = 3,
                    Description = "Wheelchairs, hearing aids, therapy sessions and accessible classrooms." },
                new Cause { Name = "Women", Icon = "person-standing-dress", GoalAmount = 1000000, DisplayOrder = 4,
                    Description = "Skills training, micro-loans and legal help so women can earn and decide for themselves." },
                new Cause { Name = "Youth", Icon = "rocket-takeoff", GoalAmount = 800000, DisplayOrder = 5,
                    Description = "Vocational courses, internships and sports clubs that keep young people on a hopeful path." },
                new Cause { Name = "Elderly", Icon = "house-heart", GoalAmount = 900000, DisplayOrder = 6,
                    Description = "Medicines, eye camps and day-care centres for senior citizens living alone." }
            );
            db.SaveChanges();
        }

        // ------------------------------------------------------------------
        private static void SeedPages(GiveAidContext db)
        {
            if (db.ContentPages.Any()) return;
            int order = 0;
            ContentPage P(string slug, string section, string title, string subtitle, string body) =>
                new ContentPage { Slug = slug, Section = section, Title = title, Subtitle = subtitle, Body = body, DisplayOrder = order++ };

            db.ContentPages.AddRange(
                P("home", "Home", "Small gifts, pooled together, change whole neighbourhoods.",
                  "Give-AID brings dozens of NGOs under one roof. Pick a cause, give any amount, and follow exactly where it goes.",
                  "Give-AID was set up with the support of leading companies so that every rupee donated reaches the people who need it most."),

                P("what-we-do", "About", "What we do",
                  "One platform, many NGOs, one promise: your donation goes where you choose.",
                  "Give-AID is a non-governmental umbrella organisation. We do not replace local NGOs - we connect them with donors, volunteers and companies who want to help.\n\n## Raising funds\nWe run donation drives, charity events and corporate giving programmes. Every donor chooses a cause, and the money is released only to NGOs working on that cause.\n\n## Running programmes\nTogether with our partner NGOs we organise:\n- Free schools and scholarship schemes\n- Medical and eye camps in low-income areas\n- Therapy and learning centres for specially challenged children\n- Skills training for women and young people\n- Day-care and health checks for the elderly\n\n## Reporting back\nEvery programme publishes photos, spending and results in our gallery and annual report."),

                P("our-mission", "About", "Our mission",
                  "A dignified life for every person, whatever they were born into.",
                  "Our mission is to make giving simple, transparent and effective, so that help reaches children, women, the disabled, young people and the elderly without delay.\n\n## Our values\n- Transparency: donors can see which cause and programme their money supports.\n- Dignity: we work with communities, not for them.\n- Partnership: we strengthen local NGOs instead of competing with them.\n- Accountability: accounts are audited every year by an independent firm."),

                P("our-team", "About", "Our team",
                  "A small staff, a large circle of volunteers.",
                  "## Board of trustees\n- Dr. Sameena Qureshi - Chairperson, paediatrician\n- Mr. Faisal Ahmed - Treasurer, chartered accountant\n- Ms. Nadia Hussain - Trustee, education specialist\n\n## Management\n- Omar Siddiqui - Executive Director\n- Hira Baig - Head of Programmes\n- Kamran Ali - Partnerships & Fundraising\n- Sana Mirza - Volunteer Coordinator\n\nMore than 400 volunteers help us every year at camps, schools and events."),

                P("careers", "About", "Career with us",
                  "Work where your skills count twice.",
                  "We look for people who care about results as much as about causes.\n\n## Current openings\n- Programme Officer (Education) - Karachi, full time\n- Field Coordinator (Health Camps) - Lahore, full time\n- Communications Intern - remote, 3 months\n\n## How to apply\nSend your CV and a short cover letter to careers@giveaid.org with the job title in the subject line. Shortlisted candidates are contacted within two weeks."),

                P("achievements", "About", "Our achievements",
                  "What our donors made possible.",
                  "- 12,000+ children kept in school through scholarships\n- 38 free medical and eye camps held in the last year\n- 1,150 wheelchairs and hearing aids distributed\n- 2,400 women trained in tailoring, IT and small business\n- 6 day-care centres opened for senior citizens\n\n## Recognition\nGive-AID received the National Philanthropy Award for transparency in 2024."),

                P("supporters", "About", "Our supporters",
                  "Individuals, families and companies who give regularly.",
                  "Thousands of people give to Give-AID every month - students who give a hundred rupees, families who give their Zakat and Sadaqah, and companies who match their employees' donations.\n\nOur founding corporate supporters helped us set up the platform and still cover all administration costs, so that public donations go fully to programmes.\n\nSee the Our Partners page for the companies that support us."),

                P("read-about-us", "About", "Read about us",
                  "News, reports and stories.",
                  "## In the news\n- Give-AID's winter drive delivers 5,000 blankets in Karachi and Quetta\n- Local NGOs join hands on a single donation platform\n- Eye camp in Thar screens 1,200 patients in three days\n\n## Reports\nOur annual report and audited accounts are available on request from info@giveaid.org."),

                P("help-centre", "Help", "Help Centre",
                  "Answers to common questions. Can't find yours? Ask us below.",
                  "## How do I donate?\nOpen Donate, choose a cause, enter the amount and your card details. You receive a receipt with a transaction number immediately.\n\n## Is my card information safe?\nWe never store your full card number or CVV. Only the last four digits are kept for your receipt.\n\n## Do I need an account?\nNo. Guests can donate too, but registered members can see their donation history, invite friends and join programmes faster.\n\n## Can I choose where my money goes?\nYes. Every donation is linked to the cause you pick and is used only for that cause.\n\n## How can I volunteer?\nOpen any upcoming programme and use the 'I want to take part' form."),

                P("contact-us", "Contact", "Contact us",
                  "We usually reply within one working day.",
                  "## Head office\nGive-AID Foundation\nPlot 14, Shahrah-e-Faisal\nKarachi, Pakistan\n\n## Phone\n+92 21 3456 7890 (Mon - Sat, 9 am - 6 pm)\n\n## E-mail\ninfo@giveaid.org\ndonations@giveaid.org")
            );
            db.SaveChanges();
        }

        // ------------------------------------------------------------------
        private static void SeedPartners(GiveAidContext db)
        {
            if (db.Partners.Any()) return;
            string[,] data =
            {
                { "Indus Steel Works",    "Corporate",  "Founding partner. Funds our scholarship programme in Karachi and Hyderabad." },
                { "Crescent Bank",        "Corporate",  "Matches every employee donation and sponsors the annual charity run." },
                { "Mehran Textiles",      "Corporate",  "Provides winter clothing and runs sewing workshops for our women's programme." },
                { "Karakoram Telecom",    "Technology", "Free SMS alerts for donors and internet for our community schools." },
                { "Sindh Pharma",         "Corporate",  "Supplies medicines for medical camps at cost price." },
                { "Daily Roshni",         "Media",      "Publishes our campaigns and donor stories free of charge." },
                { "Ravi Foods",           "Corporate",  "Supports the children's meal programme in 14 schools." },
                { "BrightPath Software",  "Technology", "Built and maintains Give-AID's donation platform pro bono." }
            };
            for (int i = 0; i < data.GetLength(0); i++)
            {
                db.Partners.Add(new Partner
                {
                    Name = data[i, 0], PartnerType = data[i, 1], Description = data[i, 2],
                    Website = "https://www." + data[i, 0].ToLower().Replace(" ", "") + ".com",
                    LogoPath = $"/images/partners/partner{i + 1}.svg",
                    DisplayOrder = i + 1
                });
            }
            db.SaveChanges();
        }

        // ------------------------------------------------------------------
        private static void SeedNgos(GiveAidContext db)
        {
            if (db.Ngos.Any()) return;
            db.Ngos.AddRange(
                new Ngo { Name = "Roshni Education Trust", RegistrationNo = "SEC-2009-0412", FocusArea = "Education", City = "Karachi", EstablishedYear = 2009,
                    ContactPerson = "Nadia Hussain", Email = "contact@roshnitrust.org", Phone = "+92 21 3587 1122",
                    Description = "Runs 22 low-fee community schools in Karachi's katchi abadis and gives scholarships to girls who reach matric." },
                new Ngo { Name = "Sahara Disability Foundation", RegistrationNo = "PB-2012-1187", FocusArea = "Disabled", City = "Lahore", EstablishedYear = 2012,
                    ContactPerson = "Bilal Anwar", Email = "info@saharafoundation.org", Phone = "+92 42 3571 9090",
                    Description = "Provides physiotherapy, speech therapy and assistive devices, and runs an inclusive learning centre for specially challenged children." },
                new Ngo { Name = "Ujala Women's Collective", RegistrationNo = "SEC-2015-0078", FocusArea = "Women", City = "Hyderabad", EstablishedYear = 2015,
                    ContactPerson = "Rukhsana Memon", Email = "hello@ujala.org.pk", Phone = "+92 22 2781 4400",
                    Description = "Skills training, micro-finance and legal aid for women in rural Sindh." },
                new Ngo { Name = "Kiran Children's Welfare", RegistrationNo = "SEC-2006-0931", FocusArea = "Children", City = "Karachi", EstablishedYear = 2006,
                    ContactPerson = "Asif Raza", Email = "kiran@kiranwelfare.org", Phone = "+92 21 3499 2210",
                    Description = "Night shelters, meals and reunification with families for street-connected children." },
                new Ngo { Name = "Shafqat Elder Care", RegistrationNo = "ICT-2014-0550", FocusArea = "Elderly", City = "Islamabad", EstablishedYear = 2014,
                    ContactPerson = "Dr. Tahira Malik", Email = "care@shafqat.org", Phone = "+92 51 2890 3344",
                    Description = "Day-care centres, home visits and free eye and diabetes check-ups for senior citizens." },
                new Ngo { Name = "Naujawan Youth Forum", RegistrationNo = "KP-2017-0219", FocusArea = "Youth", City = "Peshawar", EstablishedYear = 2017,
                    ContactPerson = "Hamza Khan", Email = "team@naujawan.org", Phone = "+92 91 5701 8800",
                    Description = "Vocational training, career counselling and community sports leagues for young people aged 15-25." }
            );
            db.SaveChanges();
        }

        // ------------------------------------------------------------------
        private static void SeedProgrammes(GiveAidContext db)
        {
            if (db.Programmes.Any()) return;
            int Ngo(string name) => db.Ngos.First(n => n.Name == name).Id;
            var today = DateTime.Today;

            db.Programmes.AddRange(
                new Programme { Title = "Back-to-School Scholarship Drive", Category = "Education", NgoId = Ngo("Roshni Education Trust"),
                    Venue = "Roshni Community School, Orangi Town, Karachi", StartDate = today.AddDays(12), EndDate = today.AddDays(14),
                    Summary = "Enrolling 500 out-of-school children and handing out uniforms, books and a full year's fees.",
                    Details = "Families from Orangi and Baldia bring their children for admission tests and counselling.\n\nVolunteers are needed to help with registration, book distribution and a reading corner for younger children.\n\n## What your donation covers\n- Rs 6,000: one child's fees for a year\n- Rs 2,500: uniform, shoes and school bag\n- Rs 1,500: a full set of textbooks",
                    ImagePath = "/images/programmes/education.webp" },
                new Programme { Title = "Free Eye Camp - Tharparkar", Category = "Health Care", NgoId = Ngo("Shafqat Elder Care"),
                    Venue = "Government High School, Mithi", StartDate = today.AddDays(25), EndDate = today.AddDays(27),
                    Summary = "Three days of free eye tests, spectacles and cataract surgery referrals for villagers of Thar.",
                    Details = "Our medical team of ophthalmologists and optometrists will screen up to 1,500 patients.\n\n- Free eye examinations\n- Free reading glasses\n- Cataract patients referred for free surgery\n\nWe need volunteers who speak Sindhi or Dhatki to help with patient registration.",
                    ImagePath = "/images/programmes/health.webp" },
                new Programme { Title = "Inclusive Learning Week", Category = "Specially Challenged Children", NgoId = Ngo("Sahara Disability Foundation"),
                    Venue = "Sahara Learning Centre, Model Town, Lahore", StartDate = today.AddDays(40), EndDate = today.AddDays(46),
                    Summary = "Therapy sessions, sign-language basics and art activities for children with special needs and their parents.",
                    Details = "A week of open sessions where parents meet therapists and teachers.\n\n## Activities\n- Speech and physiotherapy assessments\n- Sign-language classes for families\n- Art and music workshop run by children\n- Wheelchair and hearing-aid fitting",
                    ImagePath = "/images/programmes/special.webp" },
                new Programme { Title = "Hunar Women's Skills Fair", Category = "Women Empowerment", NgoId = Ngo("Ujala Women's Collective"),
                    Venue = "Sindh Museum Grounds, Hyderabad", StartDate = today.AddDays(55),
                    Summary = "Women trained in our centres display and sell embroidery, tailoring and home-made products.",
                    Details = "Over 120 women from rural Sindh will run their own stalls. Visitors can buy products directly and sponsor a woman's next training course.",
                    ImagePath = "/images/programmes/women.webp" },
                new Programme { Title = "Winter Warmth Drive", Category = "Children", NgoId = Ngo("Kiran Children's Welfare"),
                    Venue = "Kiran Night Shelter, Saddar, Karachi", StartDate = today.AddDays(-45), EndDate = today.AddDays(-40),
                    Summary = "Blankets, jackets and hot meals for 3,000 children living on the street.",
                    Details = "Completed. 3,120 children received a jacket, a blanket and a hot meal. Thank you to every donor and volunteer.",
                    ImagePath = "/images/programmes/children.webp" },
                new Programme { Title = "Senior Citizens Health Day", Category = "Health Care", NgoId = Ngo("Shafqat Elder Care"),
                    Venue = "F-9 Park, Islamabad", StartDate = today.AddDays(-20),
                    Summary = "Free diabetes, blood pressure and eye checks for people over 60.",
                    Details = "Completed. 640 senior citizens were checked and 85 were referred for follow-up treatment.",
                    ImagePath = "/images/programmes/elderly.webp" },
                new Programme { Title = "Naujawan Football League", Category = "Youth Development", NgoId = Ngo("Naujawan Youth Forum"),
                    Venue = "Qayyum Stadium, Peshawar", StartDate = today.AddDays(-70), EndDate = today.AddDays(-60),
                    Summary = "Sixteen neighbourhood teams, one trophy, and career counselling between matches.",
                    Details = "Completed. 240 players took part, and 60 joined our vocational training programme after the league.",
                    ImagePath = "/images/programmes/youth.webp" }
            );
            db.SaveChanges();
        }

        // ------------------------------------------------------------------
        /// <summary>
        /// Budget lines and people served for the sample programmes. The AI Programme Planner
        /// estimates new budgets from these real numbers instead of inventing them.
        /// Runs on older databases too (matched by programme title), only while the table is empty.
        /// </summary>
        private static void SeedProgrammeCosts(GiveAidContext db)
        {
            if (db.ProgrammeCostLines.Any()) return;

            var data = new Dictionary<string, (int People, (string Item, decimal Amount)[] Lines)>
            {
                ["Back-to-School Scholarship Drive"] = (500, new[] {
                    ("School fees (1 year)", 3000000m), ("Uniforms, shoes and bags", 1250000m),
                    ("Textbooks", 750000m), ("Admission tests and counselling", 60000m) }),
                ["Free Eye Camp - Tharparkar"] = (1500, new[] {
                    ("Doctors' and optometrists' travel", 220000m), ("Reading glasses", 420000m),
                    ("Medicines and eye drops", 180000m), ("Camp set-up (tents, generator)", 150000m),
                    ("Patient transport", 90000m), ("Registration desk and volunteer meals", 60000m) }),
                ["Inclusive Learning Week"] = (150, new[] {
                    ("Therapists' fees", 240000m), ("Wheelchairs and hearing aids", 600000m),
                    ("Art and sign-language material", 45000m), ("Venue and refreshments", 70000m) }),
                ["Hunar Women's Skills Fair"] = (120, new[] {
                    ("Stalls and venue", 180000m), ("Raw materials", 240000m),
                    ("Travel for participants", 96000m), ("Promotion", 40000m) }),
                ["Winter Warmth Drive"] = (3120, new[] {
                    ("Jackets", 2496000m), ("Blankets", 1872000m), ("Hot meals", 468000m), ("Transport", 85000m) }),
                ["Senior Citizens Health Day"] = (640, new[] {
                    ("Doctors' fees", 90000m), ("Diabetes and BP test kits", 128000m),
                    ("Medicines", 160000m), ("Eye check-ups", 48000m), ("Venue and chairs", 35000m) }),
                ["Naujawan Football League"] = (240, new[] {
                    ("Ground rent", 120000m), ("Kits and balls", 192000m), ("Referees", 48000m),
                    ("Trophies and prizes", 60000m), ("Career counselling sessions", 40000m) })
            };

            foreach (var (title, info) in data)
            {
                var programme = db.Programmes.FirstOrDefault(p => p.Title == title);
                if (programme == null) continue;          // the admin renamed or deleted it
                programme.Beneficiaries ??= info.People;
                foreach (var (item, amount) in info.Lines)
                    db.ProgrammeCostLines.Add(new ProgrammeCostLine { ProgrammeId = programme.Id, Item = item, Amount = amount });
            }
            db.SaveChanges();
        }

        // ------------------------------------------------------------------
        private static void SeedGallery(GiveAidContext db)
        {
            if (db.GalleryImages.Any()) return;
            int Prog(string title) => db.Programmes.First(p => p.Title == title).Id;
            var winter = Prog("Winter Warmth Drive");
            var health = Prog("Senior Citizens Health Day");
            var league = Prog("Naujawan Football League");

            db.GalleryImages.AddRange(
                new GalleryImage { Title = "Blanket distribution", Caption = "Volunteers packing blankets at the Saddar shelter.", ImagePath = "/images/gallery/g1.webp", ProgrammeId = winter },
                new GalleryImage { Title = "Hot meals", Caption = "Evening meal service during the winter drive.", ImagePath = "/images/gallery/g2.webp", ProgrammeId = winter },
                new GalleryImage { Title = "Eye check-up", Caption = "An optometrist checks a patient's vision.", ImagePath = "/images/gallery/g3.webp", ProgrammeId = health },
                new GalleryImage { Title = "Blood pressure desk", Caption = "Free BP and sugar tests for senior citizens.", ImagePath = "/images/gallery/g4.webp", ProgrammeId = health },
                new GalleryImage { Title = "League final", Caption = "The final match of the Naujawan Football League.", ImagePath = "/images/gallery/g5.webp", ProgrammeId = league },
                new GalleryImage { Title = "Career counselling", Caption = "Players meet trainers from our vocational centre.", ImagePath = "/images/gallery/g6.webp", ProgrammeId = league },
                new GalleryImage { Title = "Reading corner", Caption = "Children at a Roshni community school library.", ImagePath = "/images/gallery/g7.webp" },
                new GalleryImage { Title = "Sewing class", Caption = "Women's skills training in Hyderabad.", ImagePath = "/images/gallery/g8.webp" }
            );
            db.SaveChanges();
        }

        // ------------------------------------------------------------------
        private static void SeedMembersAndActivity(GiveAidContext db)
        {
            if (db.Members.Any()) return;

            var ayesha = new Member { FullName = "Ayesha Khan", Email = "ayesha@example.com", PasswordHash = PasswordHasher.Hash("Member@123"),
                Phone = "0300-1234567", Gender = "Female", DateOfBirth = new DateTime(1996, 4, 12), City = "Karachi", Country = "Pakistan",
                Address = "House 21, Block 5, Gulshan-e-Iqbal", Occupation = "Software Engineer", Organization = "BrightPath Software",
                Qualification = "BS Computer Science", AreasOfInterest = "Education, Children", RegisteredAt = DateTime.Now.AddDays(-60) };
            var usman = new Member { FullName = "Usman Tariq", Email = "usman@example.com", PasswordHash = PasswordHasher.Hash("Member@123"),
                Phone = "0321-7654321", Gender = "Male", DateOfBirth = new DateTime(1989, 11, 3), City = "Lahore", Country = "Pakistan",
                Occupation = "Chartered Accountant", Organization = "Crescent Bank", Qualification = "ACCA", AreasOfInterest = "Disabled, Elderly",
                RegisteredAt = DateTime.Now.AddDays(-35) };
            var mariam = new Member { FullName = "Mariam Siddiqui", Email = "mariam@example.com", PasswordHash = PasswordHasher.Hash("Member@123"),
                Phone = "0333-5558899", Gender = "Female", City = "Islamabad", Country = "Pakistan",
                Occupation = "Doctor", Organization = "PIMS Hospital", Qualification = "MBBS", AreasOfInterest = "Health Care, Women",
                RegisteredAt = DateTime.Now.AddDays(-12) };
            db.Members.AddRange(ayesha, usman, mariam);
            db.SaveChanges();

            int Cause(string n) => db.Causes.First(c => c.Name == n).Id;
            var rnd = new Random(7);
            (string name, string email, int? member, string cause, decimal amount, int daysAgo, bool anon)[] gifts =
            {
                ("Ayesha Khan", ayesha.Email, ayesha.Id, "Education", 10000, 58, false),
                ("Usman Tariq", usman.Email, usman.Id, "Disabled", 25000, 33, false),
                ("Mariam Siddiqui", mariam.Email, mariam.Id, "Women", 5000, 10, false),
                ("Bilal Ahmed", "bilal@example.com", null, "Children", 2500, 9, false),
                ("Anonymous donor", "anon1@example.com", null, "Elderly", 50000, 8, true),
                ("Sara Javed", "sara@example.com", null, "Education", 7500, 6, false),
                ("Ayesha Khan", ayesha.Email, ayesha.Id, "Children", 3000, 5, false),
                ("Hamid Raza", "hamid@example.com", null, "Youth", 15000, 4, false),
                ("Zainab Ali", "zainab@example.com", null, "Education", 120000, 3, false),
                ("Usman Tariq", usman.Email, usman.Id, "Elderly", 10000, 2, false),
                ("Fatima Noor", "fatima@example.com", null, "Children", 1000, 1, false),
                ("Imran Sheikh", "imran@example.com", null, "Women", 20000, 0, false)
            };
            foreach (var g in gifts)
            {
                var date = DateTime.Now.AddDays(-g.daysAgo).AddHours(-rnd.Next(1, 9));
                db.Donations.Add(new Donation
                {
                    TransactionRef = $"GA-{date:yyyyMMdd}-{rnd.Next(0x100000, 0xFFFFFF):X6}",
                    CauseId = Cause(g.cause), MemberId = g.member, DonorName = g.name, DonorEmail = g.email,
                    Amount = g.amount, CardType = rnd.Next(2) == 0 ? "Credit" : "Debit",
                    CardBrand = rnd.Next(2) == 0 ? "Visa" : "MasterCard", CardHolder = g.name.ToUpper(),
                    CardLast4 = rnd.Next(1000, 9999).ToString(), IsAnonymous = g.anon, DonatedAt = date
                });
            }

            db.Queries.AddRange(
                new SupportQuery { MemberId = ayesha.Id, Name = ayesha.FullName, Email = ayesha.Email, Subject = "Tax certificate for my donation",
                    Message = "Can I get a certificate for my Education donation for tax purposes?", Status = "Answered",
                    Reply = "Yes. Our accounts team will e-mail your certificate within 3 working days.",
                    CreatedAt = DateTime.Now.AddDays(-50), RepliedAt = DateTime.Now.AddDays(-49) },
                new SupportQuery { Name = "Kashif Mehmood", Email = "kashif@example.com", Subject = "Volunteering on weekends",
                    Message = "I am a university student. Can I volunteer only on Saturdays and Sundays?", CreatedAt = DateTime.Now.AddDays(-1) }
            );

            var eyeCamp = db.Programmes.First(p => p.Title == "Free Eye Camp - Tharparkar");
            db.ProgrammeInterests.Add(new ProgrammeInterest { ProgrammeId = eyeCamp.Id, MemberId = mariam.Id, Name = mariam.FullName,
                Email = mariam.Email, Phone = mariam.Phone, ParticipationType = "Volunteer", Message = "I can help with eye screening for two days." });

            db.SaveChanges();
        }
    }
}
