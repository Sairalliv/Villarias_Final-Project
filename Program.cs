using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Villarias_Final_Project.Program;

namespace Villarias_Final_Project
{
    internal class Program
    {
        public interface ILoginable
        {
            bool Login(string email, string password);
            void Logout();
        }
        public interface IEventActions
        {
            void ViewEvents(List<Program.Event> events);
        }
        public class User : ILoginable, IEventActions
        {
            private string Name, Email, Password, Role;
            public string name { get { return Name; } set { Name = value; } }
            public string email { get { return Email; } set { Email = value; } }
            public string password { get { return Password; } set { Password = value; } }
            public string role { get { return Role; } set { Role = value; } }
            public User(string name, string email, string password, string role)
            {
                Name = name;
                Email = email;
                Password = password;
                Role = role;
            }
            public virtual void ViewEvents(List<Event> events)
            {
                Console.WriteLine("\nAll Events:");
                foreach (var e in events)
                {
                    Console.WriteLine($"- {e.Title} (Status: {e.Status})");  
                }
            }
            public bool Login(string email, string password)
            {
                return Email == email && Password == password;
            }
            public void Logout()
            {
                Console.WriteLine();
                Console.WriteLine(CenterText($"{Name} has logged out."));
            }
        }
        public class Volunteer : User
        {
            [JsonIgnore]
            private List<Event> JoinedEvents;
            private List<string> JoinedEventTitles;  
            public Volunteer(string name, string email, string password) : base(name, email, password, "Volunteer")
            {
                JoinedEvents = new List<Event>();
                JoinedEventTitles = new List<string>();  
            }
            public List<string> GetJoinedEventTitles() => JoinedEventTitles;  
            public void SetJoinedEventTitles(List<string> titles) { JoinedEventTitles = titles ?? new List<string>(); }
            public override void ViewEvents(List<Event> events)
            {
                List<Event> available = new List<Event>();
                foreach (Event e in events)
                {
                    if (!e.IsEventFull() && e.IsRegistrationOpen() && e.Status == "Approved")
                        available.Add(e);
                }
                Console.WriteLine("\nAvailable Events (Joinable Only):");
                DisplayEventsTable(available);
            }
            public void JoinEvent(Event e)
            {
                if (!e.IsEventFull() && e.IsRegistrationOpen() && e.Status == "Approved" && !JoinedEvents.Contains(e))
                {
                    JoinedEvents.Add(e);
                    JoinedEventTitles.Add(e.Title);  
                    e.AddVolunteer(this);
                }
            }
            public void CancelEvent(string eventName)
            {
                Event e = JoinedEvents.Find(ev => ev.Title == eventName);
                if (e != null)
                {
                    JoinedEvents.Remove(e);
                    JoinedEventTitles.Remove(eventName);  
                    e.RemoveVolunteer(this.name);
                }
            }
            public List<Event> ViewMyEvents()
            {
                return JoinedEvents;
            }
        }
        public class EventManager : User
        {
            private List<Event> CreatedEvents = new List<Event>();
            private List<string> CreatedEventTitles = new List<string>();

            public EventManager(string name, string email, string password) : base(name, email, password, "EventManager")
            {
                CreatedEvents = new List<Event>();
                CreatedEventTitles = new List<string>();
            }

            public void SetCreatedEventTitles(List<string> titles)
            {
                CreatedEventTitles = titles ?? new List<string>();
            }

            public List<string> GetCreatedEventTitles()
            {
                return CreatedEventTitles;
            }

            public override void ViewEvents(List<Event> events)
            {
                Console.WriteLine("\nAll Events:");
                DisplayEventsTable(events);  
            }

            public void CreateEvent(Event e, EventSystem system)
            {
                CreatedEvents.Add(e);
                CreatedEventTitles.Add(e.Title);
                system.SubmitEventForApproval(e);
            }

            public void EditEvent(string eventName, Event updatedEvent)
            {
                Event e = CreatedEvents.Find(ev => ev.Title == eventName);
                if (e != null)
                {
                    string oldTitle = e.Title;
                    e.Title = updatedEvent.Title;
                    e.Description = updatedEvent.Description;
                    e.Location = updatedEvent.Location;
                    e.Date = updatedEvent.Date;
                    e.Deadline = updatedEvent.Deadline;
                    e.MaxVolunteers = updatedEvent.MaxVolunteers;
                    if (oldTitle != updatedEvent.Title)  
                    {
                        CreatedEventTitles.Remove(oldTitle);
                        CreatedEventTitles.Add(updatedEvent.Title);
                    }
                }
            }
            public void DeleteEvent(string eventName, EventSystem system)
            {
                CreatedEvents.RemoveAll(e => e.Title == eventName);
                CreatedEventTitles.Remove(eventName);
                var systemEvents = system.GetAllEvents();
                systemEvents.RemoveAll(e => e.Title == eventName);
                var pendingEvents = system.GetPendingEvents();
                pendingEvents.RemoveAll(e => e.Title == eventName);
            }
            public List<Volunteer> ViewVolunteers(string eventName)
            {
                Event e = CreatedEvents.Find(ev => ev.Title == eventName);
                if (e != null)
                    return e.RegisteredVolunteers;
                else
                    return new List<Volunteer>();
            }
            public List<Event> GetCreatedEvents()
            {
                return CreatedEvents;
            }
            public void ClearCreatedEvents()
            {
                CreatedEvents.Clear();
            }
            public void AddCreatedEvent(Event e)
            {
                CreatedEvents.Add(e);
            }
        }
        public class LGU : User
        {
            public LGU(string name, string email, string password) : base(name, email, password, "LGU") { }
            public override void ViewEvents(List<Event> events)
            {
                Console.WriteLine("\nAll Events:");
                DisplayEventsTable(events, showStatus: true, showDeadline: true); 
            }
            public void ApproveEvent(string eventName, EventSystem system)
            {
                Event e = system.FindPendingEvent(eventName);
                if (e != null)
                {
                    e.Status = "Approved";
                    system.GetPendingEvents().Remove(e);  
                    system.AddEvent(e);  
                    Console.WriteLine($"Event '{eventName}' approved.");
                }
                else
                {
                    Console.WriteLine("Event not found or not pending.");
                }
            }
            public void DenyEvent(string eventName, EventSystem system)
            {
                Event e = system.FindPendingEvent(eventName);
                if (e != null)
                {
                    e.Status = "Denied";
                    system.GetDeniedEvents().Add(e);  
                    system.GetPendingEvents().Remove(e);  
                    Console.WriteLine($"Event '{eventName}' denied.");
                }
                else
                {
                    Console.WriteLine("Event not found or not pending.");
                }
            }
            public List<Event> ViewPendingEvents(EventSystem system)
            {
                return system.GetPendingEvents();
            }
        }
        public class Event
        {
            private string title;
            private string description;
            private DateTime date;
            private DateTime deadline;
            private string location;
            private int maxVolunteers;
            private List<Volunteer> registeredVolunteers;
            private string status; 
            public string Title { get => title; set => title = value; }
            public string Description { get => description; set => description = value; }
            public DateTime Date { get => date; set => date = value; }
            public DateTime Deadline { get => deadline; set => deadline = value; }
            public string Location { get => location; set => location = value; }
            public int MaxVolunteers { get => maxVolunteers; set => maxVolunteers = value; }
            [JsonIgnore]
            public List<Volunteer> RegisteredVolunteers { get => registeredVolunteers; set => registeredVolunteers = value; }
            public string Status { get => status; set => status = value; }
            public Event()
            {
                registeredVolunteers = new List<Volunteer>();
                status = "Pending";
                location = "";
            }
            public Event(string title, string description, DateTime date, DateTime deadline, int maxVolunteers, string location)
            {
                this.title = title;
                this.description = description;
                this.date = date;
                this.deadline = deadline;
                this.maxVolunteers = maxVolunteers;
                this.registeredVolunteers = new List<Volunteer>();
                this.status = "Pending";
                this.location = location;
            }
            public void AddVolunteer(Volunteer v)
            {
                if (!IsEventFull() && IsRegistrationOpen() && !registeredVolunteers.Contains(v))
                    registeredVolunteers.Add(v);
            }
            public void RemoveVolunteer(string volunteerName)
            {
                registeredVolunteers.RemoveAll(v => v.name == volunteerName);
            }
            public bool IsEventFull() => registeredVolunteers.Count >= maxVolunteers;
            public bool IsRegistrationOpen() => DateTime.Now < deadline;
        }
        public class EventSystem
        {
            private List<User> Users = new List<User>();
            private List<Event> Events = new List<Event>();  
            private List<Event> PendingEvents = new List<Event>();
            private List<Event> DeniedEvents = new List<Event>();
            public EventSystem()
            {
                Users.Add(new LGU("LGU Admin", "lgu@admin.com", "lgu123"));
            }
            public void SignUp(string name, string email, string password, string role)
            {
                if (Users.Exists(u => u.email == email))
                {
                    Console.WriteLine("Error: Email already exists.");
                    return;
                }
                if (role != "Volunteer" && role != "EventManager")
                {
                    Console.WriteLine("Error: Invalid role. You can only sign up as 'Volunteer' or 'EventManager'.");
                    return;
                }
                User user;
                if (role == "Volunteer")
                    user = new Volunteer(name, email, password);
                else if (role == "EventManager")
                    user = new EventManager(name, email, password);
                else
                    user = null;  
                if (user != null)
                {
                    user.name = name;
                    user.email = email;
                    user.password = password;
                    user.role = role;
                    Users.Add(user);
                }
            }
            public User Login(string email, string password)
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    Console.WriteLine("\nError: Please enter both email and password.");
                    return null;
                }
                foreach (User u in Users)
                {
                    if (u.Login(email, password))
                        return u;
                }
                Console.WriteLine("\nError: Invalid email or password. Please try again.");
                return null;
            }
            public void LoadData()
            {
                FileHandler fh = new FileHandler();
                Users = fh.ReadUsers();
                Events = fh.ReadEvents();
                PendingEvents = fh.ReadPendingEvents();
                DeniedEvents = fh.ReadDeniedEvents();
                if (!Users.Exists(u => u.email == "lgu@admin.com"))
                {
                    Users.Add(new LGU("LGU Admin", "lgu@admin.com", "lgu123"));
                }
                foreach (var user in Users)
                {
                    if (user is EventManager em)
                    {
                        em.ClearCreatedEvents();  
                        foreach (var title in em.GetCreatedEventTitles())
                        {
                            var ev = Events.Find(e => e.Title == title) ?? PendingEvents.Find(e => e.Title == title);
                            if (ev != null && !em.GetCreatedEvents().Contains(ev))  
                            {
                                em.AddCreatedEvent(ev);
                            }
                        }
                    }
                    if (user is Volunteer vol)
                    {
                        foreach (var title in new List<string>(vol.GetJoinedEventTitles()))
                        {
                            var ev = Events.Find(e => e.Title == title);
                            if (ev != null)
                            {
                                vol.JoinEvent(ev);
                            }
                        }
                    }
                }
                Console.WriteLine($"Debug: Loaded {Events.Count} approved events, {PendingEvents.Count} pending, {DeniedEvents.Count} denied.");
            }
            public void SaveData()
            {
                try
                {
                    FileHandler fh = new FileHandler();
                    fh.SaveUsers(Users);
                    fh.SaveEvents(Events);
                    fh.SavePendingEvents(PendingEvents);
                    fh.SaveDeniedEvents(DeniedEvents);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving data: {ex.Message}");
                }
            }
            public Event FindEvent(string name)
            {
                foreach (Event e in Events)
                {
                    if (e.Title == name)
                        return e;
                }
                return null;
            }
            public Event FindPendingEvent(string name)
            {
                return PendingEvents.Find(e => e.Title == name);
            }
            public List<Event> GetPendingEvents()
            {
                return PendingEvents;
            }
            public void SubmitEventForApproval(Event e)
            {
                if (PendingEvents.Exists(ev => ev.Title == e.Title) ||
                    Events.Exists(ev => ev.Title == e.Title) ||
                    DeniedEvents.Exists(ev => ev.Title == e.Title))
                {
                    Console.WriteLine("Error: An event with this title already exists.");
                    return;
                }
                PendingEvents.Add(e);
            }
            public void DisplayAllEvents()
            {
                Console.WriteLine($"Total events in Events list: {Events.Count}");
                DisplayEventsTable(Events);  
            }
            public List<Event> GetAllEvents() => Events;
            public List<Event> GetDeniedEvents() => DeniedEvents;
            public void AddEvent(Event e)
            {
                if (Events.Exists(ev => ev.Title == e.Title) ||
                    PendingEvents.Exists(ev => ev.Title == e.Title) ||
                    DeniedEvents.Exists(ev => ev.Title == e.Title))
                {
                    Console.WriteLine("Error: An event with this title already exists.");
                    return;
                }
                Events.Add(e);
            }
        }
        public class FileHandler
        {
            private string UsersFilePath = "users.json";
            private string EventsFilePath = "events.json";
            private string PendingEventsFilePath = "pending_events.json";
            private string DeniedEventsFilePath = "denied_events.json";
            public List<User> ReadUsers()
            {
                if (!File.Exists(UsersFilePath)) return new List<User>();
                string json = File.ReadAllText(UsersFilePath);
                try
                {
                    var userData = JsonSerializer.Deserialize<List<UserData>>(json) ?? new List<UserData>();
                    List<User> users = new List<User>();
                    foreach (var data in userData)
                    {
                        User user;
                        if (data.Role == "Volunteer")
                        {
                            user = new Volunteer(data.Name, data.Email, data.Password);
                            if (user is Volunteer vol)
                            {
                                vol.SetJoinedEventTitles(data.JoinedEventTitles);  
                            }
                        }
                        else if (data.Role == "EventManager")
                        {
                            user = new EventManager(data.Name, data.Email, data.Password);
                            if (user is EventManager em)
                            {
                                em.SetCreatedEventTitles(data.CreatedEventTitles);
                            }
                        }
                        else if (data.Role == "LGU")
                        {
                            user = new LGU(data.Name, data.Email, data.Password);
                        }
                        else
                            continue;
                        users.Add(user);
                    }
                    return users;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading users: {ex.Message}. Returning empty list.");
                    return new List<User>();
                }
            }
            public void SaveUsers(List<User> users)
            {
                var userData = new List<UserData>();
                foreach (var user in users)
                {
                    var data = new UserData { Name = user.name, Email = user.email, Password = user.password, Role = user.role };
                    if (user is EventManager em)
                    {
                        data.CreatedEventTitles = em.GetCreatedEventTitles();
                    }
                    if (user is Volunteer vol)  
                    {
                        data.JoinedEventTitles = vol.GetJoinedEventTitles();
                    }
                    userData.Add(data);
                }
                string json = JsonSerializer.Serialize(userData);
                File.WriteAllText(UsersFilePath, json);
            }
            public List<Event> ReadEvents()
            {
                if (!File.Exists(EventsFilePath)) return new List<Event>();
                string json = File.ReadAllText(EventsFilePath);
                try
                {
                    return JsonSerializer.Deserialize<List<Event>>(json) ?? new List<Event>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading events: {ex.Message}. Returning empty list.");
                    return new List<Event>();
                }
            }
            public void SaveEvents(List<Event> events)
            {
                try
                {
                    string json = JsonSerializer.Serialize(events);
                    File.WriteAllText(EventsFilePath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving events: {ex.Message}");
                }
            }
            public List<Event> ReadPendingEvents()
            {
                if (!File.Exists(PendingEventsFilePath)) return new List<Event>();
                string json = File.ReadAllText(PendingEventsFilePath);
                try
                {
                    return JsonSerializer.Deserialize<List<Event>>(json) ?? new List<Event>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading pending events: {ex.Message}. Returning empty list.");
                    return new List<Event>();
                }
            }
            public void SavePendingEvents(List<Event> pendingEvents)
            {
                string json = JsonSerializer.Serialize(pendingEvents);
                File.WriteAllText(PendingEventsFilePath, json);
            }
            public List<Event> ReadDeniedEvents()
            {
                if (!File.Exists(DeniedEventsFilePath)) return new List<Event>();
                string json = File.ReadAllText(DeniedEventsFilePath);
                try
                {
                    return JsonSerializer.Deserialize<List<Event>>(json) ?? new List<Event>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading denied events: {ex.Message}. Returning empty list.");
                    return new List<Event>();
                }
            }
            public void SaveDeniedEvents(List<Event> deniedEvents)
            {
                string json = JsonSerializer.Serialize(deniedEvents);
                File.WriteAllText(DeniedEventsFilePath, json);
            }
        }
        public class UserData
        {
            private string name;
            private string email;
            private string password;
            private string role;
            private List<string> createdEventTitles;
            private List<string> joinedEventTitles;

            public string Name { get { return name; } set { name = value; } }
            public string Email { get { return email; } set { email = value; } }
            public string Password { get { return password; } set { password = value; } }
            public string Role { get { return role; } set { role = value; } }
            public List<string> CreatedEventTitles { get { return createdEventTitles ?? new List<string>(); } set { createdEventTitles = value; } }
            public List<string> JoinedEventTitles { get { return joinedEventTitles ?? new List<string>(); } set { joinedEventTitles = value; } }
        }
        static void DisplayEventsTable(List<Event> events, bool showStatus = false, bool showDeadline = false, bool showNumbers = false)
        {
            if (events.Count == 0)
            {
                Console.WriteLine("No events to display.");
                return;
            }
            var table = new Table();
            table.Border = TableBorder.Rounded;  
            if (showNumbers)
                table.AddColumn("No.");
            table.AddColumn("Title");
            table.AddColumn("Location");
            table.AddColumn("Date");
            table.AddColumn("Max Volunteers");
            if (showStatus && showDeadline)
            {
                table.AddColumn("Deadline");
                table.AddColumn("Status");
            }
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                var row = new List<string>();
                if (showNumbers)
                    row.Add((i + 1).ToString());
                row.Add(e.Title.Length > 40 ? e.Title.Substring(0, 37) + "..." : e.Title);
                row.Add(e.Location.Length > 45 ? e.Location.Substring(0, 42) + "..." : e.Location);
                row.Add(e.Date.ToShortDateString());
                row.Add(e.MaxVolunteers.ToString());
                if (showStatus && showDeadline)
                {
                    row.Add(e.Deadline.ToShortDateString());
                    row.Add(e.Status);
                }
                table.AddRow(row.ToArray());
            }
            AnsiConsole.Write(table);
        }
        static string CenterText(string text)
        {
            int consoleWidth = Console.WindowWidth;
            int textLength = text.Length;
            int spaces = (consoleWidth - textLength) / 2;
            return new string(' ', Math.Max(0, spaces)) + text;
        }
        static void VolunteerMenu(Volunteer volunteer, EventSystem system)
        {
            bool running = true;
            while (running)
            {
                Console.Clear();
                Console.WriteLine(CenterText($"Welcome, {volunteer.name} ({volunteer.role})"));
                Console.WriteLine(CenterText("Volunteer Menu:"));
                Console.WriteLine(CenterText("[1] View Available Events (Joinable Only)"));
                Console.WriteLine(CenterText("[2] Join Event"));
                Console.WriteLine(CenterText("[3] Cancel Joined Event"));
                Console.WriteLine(CenterText("[4] View Joined Events"));
                Console.WriteLine(CenterText("[5] Logout"));  
                Console.Write(CenterText("Choose an option: "));
                string choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        Console.Clear();
                        Console.WriteLine("=== Available Events (Joinable Only) ===\n");
                        volunteer.ViewEvents(system.GetAllEvents());  
                        Console.WriteLine("\nPress any key to return...");
                        Console.ReadKey();
                        break;
                    case "2":
                        Console.Clear();
                        Console.WriteLine("=== Join an Event ===\n");
                        List<Event> joinableEvents = new List<Event>();
                        foreach (Event e in system.GetAllEvents())
                        {
                            if (!e.IsEventFull() && e.IsRegistrationOpen() && e.Status == "Approved")
                                joinableEvents.Add(e);
                        }
                        if (joinableEvents.Count == 0)
                        {
                            Console.WriteLine("No joinable events available.");
                        }
                        else
                        {
                            DisplayEventsTable(joinableEvents, showNumbers: true);
                            Console.Write("\nEnter the number of the event to join: ");
                            if (int.TryParse(Console.ReadLine(), out int joinChoice) && joinChoice >= 1 && joinChoice <= joinableEvents.Count)
                            {
                                Event selectedEvent = joinableEvents[joinChoice - 1];
                                Console.WriteLine($"\nEvent: {selectedEvent.Title}");
                                Console.WriteLine($"Description: {selectedEvent.Description}");
                                Console.Write("Do you want to join this event? (y/n): ");
                                if (Console.ReadLine().ToLower() == "y")
                                {
                                    if (volunteer.ViewMyEvents().Contains(selectedEvent))
                                    {
                                        Console.WriteLine("You have already joined this event.");
                                    }
                                    else
                                    {
                                        volunteer.JoinEvent(selectedEvent);
                                        system.SaveData();
                                        Console.WriteLine("Joined successfully!");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Action cancelled.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Invalid number.");
                            }
                        }
                        Console.WriteLine("\nPress any key to return...");
                        Console.ReadKey();
                        break;
                    case "3":
                        Console.Clear();
                        Console.WriteLine("=== Cancel Joined Event ===\n");
                        var myEventsToCancel = volunteer.ViewMyEvents();
                        if (myEventsToCancel.Count == 0)
                        {
                            Console.WriteLine("You have not joined any events yet.");
                        }
                        else
                        {
                            DisplayEventsTable(myEventsToCancel, showNumbers: true);  
                            Console.Write("\nEnter the number of the event to cancel: ");  
                            if (int.TryParse(Console.ReadLine(), out int cancelChoice) && cancelChoice >= 1 && cancelChoice <= myEventsToCancel.Count)
                            {
                                Event selectedEvent = myEventsToCancel[cancelChoice - 1];  
                                Console.WriteLine($"\nEvent: {selectedEvent.Title}");
                                Console.WriteLine($"Description: {selectedEvent.Description}");
                                Console.Write("Do you want to cancel this event? (y/n): ");
                                if (Console.ReadLine().ToLower() == "y")
                                {
                                    volunteer.CancelEvent(selectedEvent.Title);  
                                    system.SaveData();  
                                }
                                else
                                {
                                    Console.WriteLine("Action cancelled.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Invalid number.");
                            }
                        }
                        Console.WriteLine("\nPress any key to return...");
                        Console.ReadKey();
                        break;
                    case "4":
                        Console.Clear();
                        Console.WriteLine("=== Your Joined Events ===\n");
                        var myEvents = volunteer.ViewMyEvents();
                        if (myEvents.Count == 0)
                        {
                            Console.WriteLine("You haven’t joined any events yet.");
                        }
                        else
                        {
                            DisplayEventsTable(myEvents);
                        }
                        Console.WriteLine("\nPress any key to return...");
                        Console.ReadKey();
                        break;
                    case "5":
                        running = false;
                        volunteer.Logout();
                        break;
                    default:
                        Console.WriteLine("Invalid option. Try again.");
                        Console.WriteLine("\nPress any key to return...");
                        Console.ReadKey();
                        break;
                }
            }
        }
        static void EventManagerMenu(EventManager em, EventSystem system)
        {
            bool loggedIn = true;
            while (loggedIn)
            {
                Console.Clear();
                Console.WriteLine();
                Console.WriteLine(CenterText($"--- Event Manager Menu ({em.name}) ---"));
                Console.WriteLine(CenterText("1. Create Event"));
                Console.WriteLine(CenterText("2. Edit Event"));
                Console.WriteLine(CenterText("3. Delete Event"));
                Console.WriteLine(CenterText("4. View Volunteers for Event"));
                Console.WriteLine(CenterText("5. View All Events"));
                Console.WriteLine(CenterText("6. Logout"));
                Console.Write(CenterText("Choose an option: "));
                string option = Console.ReadLine();
                switch (option)
                {
                    case "1":
                        Console.Clear();
                        Console.WriteLine("\n=== Create Event ===");
                        Event newEvent = new Event();
                        Console.Write("Enter event title: ");
                        newEvent.Title = Console.ReadLine();
                        Console.Write("Enter description: ");
                        newEvent.Description = Console.ReadLine();
                        Console.Write("Enter location: ");
                        newEvent.Location = Console.ReadLine();
                        try
                        {
                            Console.Write("Enter date (yyyy-mm-dd): ");
                            newEvent.Date = DateTime.Parse(Console.ReadLine());
                            Console.Write("Enter deadline (yyyy-mm-dd): ");
                            newEvent.Deadline = DateTime.Parse(Console.ReadLine());
                            if (newEvent.Deadline >= newEvent.Date)
                            {
                                Console.WriteLine("\nError: Deadline must be before the event date.");
                                Console.WriteLine("Press any key to return...");
                                Console.ReadKey();
                                break;
                            }
                            if (newEvent.Date <= DateTime.Now || newEvent.Deadline <= DateTime.Now)  
                            {
                                Console.WriteLine("\nError: Event date and deadline must be in the future.");
                                Console.WriteLine("Press any key to return...");
                                Console.ReadKey();
                                break;
                            }
                            Console.Write("Enter max volunteers: ");
                            newEvent.MaxVolunteers = int.Parse(Console.ReadLine());
                            if (newEvent.MaxVolunteers <= 0)
                            {
                                Console.WriteLine("\nError: Max volunteers must be greater than zero.");
                                Console.WriteLine("Press any key to return...");
                                Console.ReadKey();
                                break;
                            }
                            if (string.IsNullOrWhiteSpace(newEvent.Title) || string.IsNullOrWhiteSpace(newEvent.Description) || string.IsNullOrWhiteSpace(newEvent.Location))  
                            {
                                Console.WriteLine("\nError: Title, description, and location cannot be empty.");
                                Console.WriteLine("Press any key to return...");
                                Console.ReadKey();
                                break;
                            }
                            em.CreateEvent(newEvent, system);
                            Console.WriteLine("\nEvent created and submitted for approval!");
                        }
                        catch (FormatException)
                        {
                            Console.WriteLine("\nError: Invalid input format. Please use the correct format (e.g., yyyy-mm-dd for dates, whole numbers for volunteers).");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"\nUnexpected error: {ex.Message}");
                        }
                        Console.WriteLine("Press any key to return...");
                        Console.ReadKey();
                        break;
                    case "2":
                        Console.Clear();
                        Console.WriteLine("\n=== Edit Event ===");
                        List<Event> myEvents = em.GetCreatedEvents();
                        if (myEvents.Count == 0)
                        {
                            Console.WriteLine("\nYou haven’t created any events yet.");
                            Console.WriteLine("Press any key to return...");
                            Console.ReadKey();
                            break;
                        }
                        Console.WriteLine("\nYour Events:");
                        DisplayEventsTable(myEvents, showStatus: true, showDeadline: true, showNumbers: true);
                        Console.Write("\nEnter the number of the event you want to edit: ");
                        if (int.TryParse(Console.ReadLine(), out int eventChoice) && eventChoice >= 1 && eventChoice <= myEvents.Count)
                        {
                            Event editEvent = myEvents[eventChoice - 1];
                            Console.WriteLine($"\nEvent: {editEvent.Title}");
                            Console.WriteLine($"Description: {editEvent.Description}");
                            Console.Write("Do you want to edit this event? (y/n): ");
                            if (Console.ReadLine().ToLower() == "y")
                            {
                                Event updatedEvent = new Event();
                                updatedEvent.Title = editEvent.Title;  
                                updatedEvent.Description = editEvent.Description;
                                updatedEvent.Location = editEvent.Location;
                                updatedEvent.Date = editEvent.Date;
                                updatedEvent.Deadline = editEvent.Deadline;
                                updatedEvent.MaxVolunteers = editEvent.MaxVolunteers;

                                Console.WriteLine($"\nEditing: {editEvent.Title}");
                                Console.Write("Enter new title (leave blank to keep current): ");
                                string newTitle = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(newTitle))
                                    updatedEvent.Title = newTitle;
                                Console.Write("Enter new description (leave blank to keep current): ");
                                string newDesc = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(newDesc))
                                    updatedEvent.Description = newDesc;
                                Console.Write("Enter new date (yyyy-mm-dd, leave blank to keep current): ");
                                string newDateStr = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(newDateStr))
                                {
                                    if (DateTime.TryParse(newDateStr, out DateTime newDate))
                                        updatedEvent.Date = newDate;
                                    else
                                        Console.WriteLine("Invalid date format — keeping old date.");
                                }
                                Console.Write("Enter new deadline (yyyy-mm-dd, leave blank to keep current): ");
                                string newDeadlineStr = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(newDeadlineStr))
                                {
                                    if (DateTime.TryParse(newDeadlineStr, out DateTime newDeadline))
                                        updatedEvent.Deadline = newDeadline;
                                    else
                                        Console.WriteLine("Invalid date format — keeping old deadline.");
                                }
                                Console.Write("Enter new location (leave blank to keep current): ");
                                string newLocationStr = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(newLocationStr))
                                    updatedEvent.Location = newLocationStr;
                                Console.Write("Enter new max volunteers (leave blank to keep current): ");
                                string newMaxStr = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(newMaxStr))
                                {
                                    if (int.TryParse(newMaxStr, out int newMax) && newMax > 0)
                                        updatedEvent.MaxVolunteers = newMax;
                                    else
                                        Console.WriteLine("Invalid number — keeping old max volunteers.");
                                }
                                if (updatedEvent.Deadline >= updatedEvent.Date || updatedEvent.MaxVolunteers <= 0)
                                {
                                    Console.WriteLine("Error: Invalid event data after edit (deadline must be before date, max volunteers > 0). Changes not applied.");
                                    Console.WriteLine("Press any key to return...");
                                    Console.ReadKey();
                                    break;
                                }
                                em.EditEvent(editEvent.Title, updatedEvent);  
                                Console.WriteLine("\nEvent updated successfully!");
                            }
                            else
                            {
                                Console.WriteLine("Action cancelled.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("\nInvalid selection.");
                        }
                        Console.WriteLine("Press any key to return...");
                        Console.ReadKey();
                        break;
                    case "3":
                        Console.Clear();
                        Console.WriteLine("\n=== Delete Event ===");
                        List<Event> managerEvents = em.GetCreatedEvents();
                        if (managerEvents.Count == 0)
                        {
                            Console.WriteLine("\nYou haven’t created any events yet.");
                            Console.WriteLine("Press any key to return...");
                            Console.ReadKey();
                            break;
                        }
                        Console.WriteLine("\nYour Events:");
                        DisplayEventsTable(managerEvents, showStatus: true, showDeadline: true, showNumbers: true);
                        Console.Write("\nEnter the number of the event you want to delete: ");
                        if (int.TryParse(Console.ReadLine(), out int deleteChoice) && deleteChoice >= 1 && deleteChoice <= managerEvents.Count)
                        {
                            Event selectedEvent = managerEvents[deleteChoice - 1];
                            Console.WriteLine($"\nEvent: {selectedEvent.Title}");
                            Console.WriteLine($"Description: {selectedEvent.Description}");
                            Console.Write("Do you want to delete this event? (y/n): ");
                            if (Console.ReadLine().ToLower() == "y")
                            {
                                em.DeleteEvent(selectedEvent.Title, system);
                                Console.WriteLine($"\nEvent '{selectedEvent.Title}' deleted successfully!");
                            }
                            else
                            {
                                Console.WriteLine("Action cancelled.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("\nInvalid selection. Please choose a valid event number.");
                        }
                        Console.WriteLine("Press any key to return...");
                        Console.ReadKey();
                        break;
                    case "4":
                        Console.Clear();
                        Console.WriteLine("\n=== View Volunteers for Event ===");
                        List<Event> myEventsList = em.GetCreatedEvents();
                        if (myEventsList.Count == 0)
                        {
                            Console.WriteLine("\nYou haven’t created any events yet.");
                            Console.WriteLine("Press any key to return...");
                            Console.ReadKey();
                            break;
                        }
                        Console.WriteLine("\nYour Events:");
                        DisplayEventsTable(myEventsList, showStatus: true, showDeadline: true, showNumbers: true);
                        Console.Write("\nEnter the number of the event to view volunteers: ");
                        if (int.TryParse(Console.ReadLine(), out int viewChoice) && viewChoice >= 1 && viewChoice <= myEventsList.Count)
                        {
                            Event selectedEvent = myEventsList[viewChoice - 1];
                            Console.WriteLine($"\nEvent: {selectedEvent.Title}");
                            Console.WriteLine($"Description: {selectedEvent.Description}");
                            Console.Write("Do you want to view volunteers for this event? (y/n): ");
                            if (Console.ReadLine().ToLower() == "y")
                            {
                                List<Volunteer> volunteers = em.ViewVolunteers(selectedEvent.Title);
                                Console.WriteLine($"\nVolunteers for '{selectedEvent.Title}':");
                                if (volunteers.Count == 0)
                                    Console.WriteLine("No volunteers have joined this event yet.");
                                else
                                    foreach (Volunteer vol in volunteers)
                                        Console.WriteLine($"- {vol.name}");
                            }
                            else
                            {
                                Console.WriteLine("Action cancelled.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("\nInvalid selection. Please choose a valid event number.");
                        }
                        Console.WriteLine("Press any key to return...");
                        Console.ReadKey();
                        break;
                    case "5":
                        em.ViewEvents(system.GetAllEvents());  
                        Console.WriteLine("Press any key to return...");
                        Console.ReadKey();
                        break;
                    case "6":
                        em.Logout();
                        loggedIn = false;
                        break;
                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }
        }
        static void LGUMenu(LGU lgu, EventSystem system)
        {
            bool running = true;
            while (running)
            {
                Console.Clear();
                Console.WriteLine(CenterText($"Welcome, {lgu.name} ({lgu.role})"));
                Console.WriteLine(CenterText("LGU Menu:"));
                Console.WriteLine(CenterText("[1] View Pending Events"));
                Console.WriteLine(CenterText("[2] Approve Event"));
                Console.WriteLine(CenterText("[3] Deny Event"));
                Console.WriteLine(CenterText("[4] View All Approved Events"));
                Console.WriteLine(CenterText("[5] View Denied Events"));
                Console.WriteLine(CenterText("[6] Logout"));
                Console.Write(CenterText("Choose an option: "));
                string choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        Console.Clear();
                        Console.WriteLine("=== Pending Events ===\n");
                        var pendingEvents = lgu.ViewPendingEvents(system);
                        if (pendingEvents.Count == 0)
                        {
                            Console.WriteLine("No pending events.");
                        }
                        else
                        {
                            DisplayEventsTable(pendingEvents, showStatus: true, showDeadline: true);
                        }
                        Console.WriteLine("\nPress any key to return...");
                        Console.ReadKey();
                        break;
                    case "2":
                        Console.Clear();
                        Console.WriteLine("=== Approve Event ===\n");
                        var approveEvents = lgu.ViewPendingEvents(system);
                        if (approveEvents.Count == 0)
                        {
                            Console.WriteLine("No pending events to approve.");
                        }
                        else
                        {
                            DisplayEventsTable(approveEvents, showStatus: true, showDeadline: true, showNumbers: true);
                            Console.Write("\nEnter the number of the event to approve: ");
                            if (int.TryParse(Console.ReadLine(), out int approveChoice) && approveChoice >= 1 && approveChoice <= approveEvents.Count)
                            {
                                Event selectedEvent = approveEvents[approveChoice - 1];
                                Console.WriteLine($"\nEvent: {selectedEvent.Title}");
                                Console.WriteLine($"Description: {selectedEvent.Description}");
                                Console.Write("Do you want to approve this event? (y/n): ");
                                if (Console.ReadLine().ToLower() == "y")
                                {
                                    lgu.ApproveEvent(selectedEvent.Title, system);
                                    system.SaveData();  
                                    Console.WriteLine("Event approved!");
                                }
                                else
                                {
                                    Console.WriteLine("Action cancelled.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Invalid number.");
                            }
                        }
                        Console.WriteLine("\nPress any key to return...");
                        Console.ReadKey();
                        break;
                    case "3":
                        Console.Clear();
                        Console.WriteLine("=== Deny Event ===\n");
                        var denyEvents = lgu.ViewPendingEvents(system);
                        if (denyEvents.Count == 0)
                        {
                            Console.WriteLine("No pending events to deny.");
                        }
                        else
                        {
                            DisplayEventsTable(denyEvents, showStatus: true, showDeadline: true, showNumbers: true);
                            Console.Write("\nEnter the number of the event to deny: ");
                            if (int.TryParse(Console.ReadLine(), out int denyChoice) && denyChoice >= 1 && denyChoice <= denyEvents.Count)
                            {
                                Event selectedEvent = denyEvents[denyChoice - 1];
                                Console.WriteLine($"\nEvent: {selectedEvent.Title}");
                                Console.WriteLine($"Description: {selectedEvent.Description}");
                                Console.Write("Do you want to deny this event? (y/n): ");
                                if (Console.ReadLine().ToLower() == "y")
                                {
                                    lgu.DenyEvent(selectedEvent.Title, system);
                                    system.SaveData();  
                                    Console.WriteLine("Event denied!");
                                }
                                else
                                {
                                    Console.WriteLine("Action cancelled.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Invalid number.");
                            }
                        }
                        Console.WriteLine("\nPress any key to return...");
                        Console.ReadKey();
                        break;
                    case "4":
                        Console.Clear();
                        Console.WriteLine("=== All Approved Events ===\n");
                        var allEvents = system.GetAllEvents();
                        DisplayEventsTable(allEvents);  
                        Console.WriteLine("\nPress any key to return...");
                        Console.ReadKey();
                        break;
                    case "5":  
                        Console.Clear();
                        Console.WriteLine("=== Denied Events ===\n");
                        var deniedEvents = system.GetDeniedEvents();
                        if (deniedEvents.Count == 0)
                        {
                            Console.WriteLine("No denied events.");
                        }
                        else
                        {
                            DisplayEventsTable(deniedEvents, showStatus: true, showDeadline: true);
                        }
                        Console.WriteLine("\nPress any key to return...");
                        Console.ReadKey();
                        break;
                    case "6":
                        running = false;
                        lgu.Logout();
                        break;
                    default:
                        Console.WriteLine("Invalid option. Try again.");
                        Console.WriteLine("\nPress any key to return...");
                        Console.ReadKey();
                        break;
                }
            }
        }
        static void Main(string[] args)
        {
            EventSystem system = new EventSystem();
            system.LoadData();
            bool running = true;
            while (running)
            {
                Console.Clear();
                Console.WriteLine();
                Console.WriteLine(CenterText("__ __   ___   _      __ __  ____   ______    ___    ___  ____"));
                Console.WriteLine(CenterText("|  |  | /   \\ | |    |  |  ||    \\ |      |  /  _]  /  _]|    \\ "));
                Console.WriteLine(CenterText("|  |  ||     || |    |  |  ||  _  ||      | /  [_  /  [_ |  D  )"));
                Console.WriteLine(CenterText("|  |  ||  O  || |___ |  |  ||  |  ||_|  |_||    _]|    _]|    / "));
                Console.WriteLine(CenterText("|  :  ||     ||     ||  :  ||  |  |  |  |  |   [_ |   [_ |    \\ "));
                Console.WriteLine(CenterText(" \\   / |     ||     ||     ||  |  |  |  |  |     ||     ||  .  \\"));
                Console.WriteLine(CenterText("  \\_/   \\___/ |_____| \\__,_||__|__|  |__|  |_____||_____||__|\\_|"));
                Console.WriteLine(CenterText("___ ___   ____  ____    ____   ____    ___  ___ ___    ___  ____   ______"));
                Console.WriteLine(CenterText("|   |   | /    ||    \\  /    | /    |  /  _]|   |   |  /  _]|    \\ |      |"));
                Console.WriteLine(CenterText("| _   _ ||  o  ||  _  ||  o  ||   __| /  [_ | _   _ | /  [_ |  _  ||      |"));
                Console.WriteLine(CenterText("|  \\_/  ||     ||  |  ||     ||  |  ||    _]|  \\_/  ||    _]|  |  ||_|  |_|"));
                Console.WriteLine(CenterText("|   |   ||  _  ||  |  ||  _  ||  |_ ||   [_ |   |   ||   [_ |  |  |  |  |  "));
                Console.WriteLine(CenterText("|   |   ||  |  ||  |  ||  |  ||     ||     ||   |   ||     ||  |  |  |  |  "));
                Console.WriteLine(CenterText("|___|___||__|__||__|__||__|__||___,_||_____||___|___||_____||__|__|  |__|  "));
                Console.WriteLine(CenterText("  _____ __ __  _____ ______    ___  ___ ___"));
                Console.WriteLine(CenterText(" / ___/|  |  |/ ___/|      |  /  _]|   |   |"));
                Console.WriteLine(CenterText("(   \\_ |  |  (   \\_ |      | /  [_ | _   _ |"));
                Console.WriteLine(CenterText(" \\__  ||  ~  |\\__  ||_|  |_||    _]|  \\_/  |"));
                Console.WriteLine(CenterText(" /  \\ ||___, |/  \\ |  |  |  |   [_ |   |   |"));
                Console.WriteLine(CenterText(" \\    ||     |\\    |  |  |  |     ||   |   |"));
                Console.WriteLine(CenterText("  \\___||____/  \\___|  |__|  |_____||___|___|"));
                Console.WriteLine();
                Console.WriteLine(CenterText("1. Sign Up"));
                Console.WriteLine(CenterText("2. Login"));
                Console.WriteLine(CenterText("3. Display All Available Events"));
                Console.WriteLine(CenterText("4. Exit"));
                Console.Write(CenterText("Choose an option: "));  
                string choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        Console.Clear();
                        Console.Write("Enter name: ");
                        string name = Console.ReadLine();
                        Console.Write("Enter email: ");
                        string email = Console.ReadLine();
                        Console.Write("Enter password: ");
                        string password = Console.ReadLine();
                        Console.Write("Enter role (Volunteer/EventManager): ");
                        string role = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))  
                            Console.WriteLine("\nError: All fields are required!");
                        else if (role != "Volunteer" && role != "EventManager")
                            Console.WriteLine("\nError: Role must be either 'Volunteer' or 'EventManager'.");
                        else
                        {
                            system.SignUp(name, email, password, role);
                            Console.WriteLine("\nSuccessfully signed up!");
                        }
                        Console.WriteLine();
                        Console.Write(CenterText("Press any key to return to the main menu..."));
                        Console.ReadKey();
                        break;
                    case "2":
                        Console.Clear();
                        Console.Write("Enter email: ");
                        string logEmail = Console.ReadLine();
                        Console.Write("Enter password: ");
                        string logPassword = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(logEmail) || string.IsNullOrWhiteSpace(logPassword))
                            Console.WriteLine("\nError: Please enter both email and password.");
                        else
                        {
                            User user = system.Login(logEmail, logPassword);
                            if (user != null)
                            {
                                Console.WriteLine($"Welcome, {user.name} ({user.role})!");
                                if (user.role == "Volunteer")
                                    VolunteerMenu((Volunteer)user, system);
                                else if (user.role == "EventManager")
                                    EventManagerMenu((EventManager)user, system);
                                else if (user.role == "LGU")
                                    LGUMenu((LGU)user, system);
                            }
                        }
                        Console.WriteLine();
                        Console.Write(CenterText("Press any key to return to the main menu..."));
                        Console.ReadKey();
                        break;
                    case "3":
                        Console.Clear();
                        Console.WriteLine();
                        system.DisplayAllEvents();
                        Console.WriteLine();
                        Console.Write(CenterText("Press any key to return to the main menu..."));
                        Console.ReadKey();
                        break;
                    case "4":
                        Console.Clear();
                        system.SaveData();
                        Console.WriteLine("\n\n\n");
                        Console.WriteLine(CenterText(" ______  __ __   ____  ____   __  _      __ __   ___   __ __ "));
                        Console.WriteLine(CenterText("|      ||  |  | /    ||    \\ |  |/ ]    |  |  | /   \\ |  |  |"));
                        Console.WriteLine(CenterText("|      ||  |  ||  o  ||  _  ||  ' /     |  |  ||     ||  |  |"));
                        Console.WriteLine(CenterText("|_|  |_||  _  ||     ||  |  ||    \\     |  ~  ||  O  ||  |  |"));
                        Console.WriteLine(CenterText("  |  |  |  |  ||  _  ||  |  ||     |    |___, ||     ||  :  |"));
                        Console.WriteLine(CenterText("  |  |  |  |  ||  |  ||  |  ||  .  |    |     ||     ||     |"));
                        Console.WriteLine(CenterText(" |__|  |__|__||__|__||__|__||__|\\_|    |____/  \\___/  \\__,_|"));
                        Console.WriteLine();
                        Console.WriteLine(CenterText("Villarias, Riian Ray C."));
                        Console.WriteLine(CenterText("BSCpE"));
                        Console.WriteLine(CenterText("CPE261 - H1"));
                        Console.WriteLine("\n\n\n");
                        running = false;
                        Console.WriteLine("Exiting system...");
                        break;
                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }
        }
    }
}
