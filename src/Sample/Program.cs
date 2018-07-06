using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.NodaTime;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var store = new DocumentStore()
            {
                Database = "InboxPlus_LOCAL",
                Urls = new[] {"http://localhost:8081"}, // 4.0.6-patch-40047
            };

            store.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            store.Initialize();

            using (var session = store.OpenSession())
            {
                session.Store(new Person()
                {
                    Instant = SystemClock.Instance.GetCurrentInstant(),
                    Name = "Jack Bauer",
                });
                session.SaveChanges();
            }
            
            new ServerSideNodaTime_Index().Execute(store);
        }
    }
    
    public class ServerSideNodaTime_Index : AbstractIndexCreationTask<Person>
    {
        public ServerSideNodaTime_Index()
        {
            Map = persons => from person in persons
                let zones = DateTimeZoneProviders.Tzdb
                let createdAt = person.Instant.AsInstant().InZone(zones["Europe/Zurich"]).LocalDateTime.Resolve()
                select new
                {
                    person.Name,
                    createdAt,
                };

            AdditionalSources = new Dictionary<string, string> {
                { "Raven.Client.NodaTime", NodaTimeCompilationExtension.AdditionalSourcesRavenBundlesNodaTime },
                { "Raven.Client.NodaTime2", NodaTimeCompilationExtension.AdditionalSourcesNodaTime }
            };
        }
    }

    public class Person
    {
        public string Name { get; set; }
        public Instant Instant { get; set; }
    }
}