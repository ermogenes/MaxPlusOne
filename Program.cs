using System.Security.Cryptography;

const int Repetitions = 25;
const int ChecksNeeded = 3;
const int WaitToCommitMs = 10;
const int MinRandomWaitMs = 50;
const int MaxRandomWaitMs = 500;

const bool ActivateWaitTimer = true;
const bool JoinThreadsTwoByTwo = true;
const bool Verbose = true;

var sharedRepository = new Repo();
sharedRepository.Items.Add(new Item { sequentialId = 0 });

List<Thread> threads = new();

for (int c = 1; c <= Repetitions; c++)
{
    Console.WriteLine($"\n------ Loop #{c}");

    var t1 = new Thread(new ThreadStart(AddNewItem));
    var t2 = new Thread(new ThreadStart(AddNewItem));

    t1.Name = $"t1/{c}";
    t2.Name = $"t2/{c}";

    t1.Start();
    t2.Start();

    if (JoinThreadsTwoByTwo)
    {
        t1.Join();
        t2.Join();
    }

    threads.Add(t1);
    threads.Add(t2);
}

foreach (var thread in threads) thread.Join();

Console.WriteLine($"--- Final score ---");
Console.WriteLine($"Items Count = {sharedRepository.Items.Count()}");
Console.WriteLine($"Unique Ids  = {sharedRepository.Items.GroupBy(l => l.sequentialId).Count()}");
Console.WriteLine($"Max Id      = {sharedRepository.Items.Max(l => l.sequentialId)}");

if (Verbose)
{
    foreach (var colision in sharedRepository.Items.GroupBy(l => l.sequentialId))
    {
        if (colision.Count() != 1)
            Console.WriteLine($"Id {colision.Key} was taken on {colision.Count()} items.");
    }
}

void AddNewItem()
{
    Console.WriteLine($"\nStarting {Thread.CurrentThread.Name}...");
    Item newItem = new();

    int newId = GetNextItemId();
    lock (sharedRepository)
    {
        newItem.sequentialId = newId;
        Thread.Sleep(WaitToCommitMs);
        sharedRepository.Items.Add(newItem);
    }
    Console.WriteLine($"\t\t{Thread.CurrentThread.Name} added item #{newItem.sequentialId}");
}

int GetNextItemId()
{
    if (Verbose) Console.WriteLine($"\t\t{Thread.CurrentThread.Name} is inferring next id...");

    int nextId = 0;
    bool isUnique = false;

    if (ActivateWaitTimer)
    {
        while (!isUnique)
        {
            lock (sharedRepository)
            {
                nextId = sharedRepository.Items.Max(l => l.sequentialId) + 1;
            }

            for (int checks = 1; checks <= ChecksNeeded; checks++)
            {
                int randomWait = RandomNumberGenerator.GetInt32(MinRandomWaitMs, MaxRandomWaitMs);
                if (Verbose) Console.WriteLine($"\t\t\t{Thread.CurrentThread.Name} is waiting {randomWait}ms...");

                Thread.Sleep(randomWait);

                lock (sharedRepository)
                {
                    isUnique = !sharedRepository.Items.Any(l => l.sequentialId == nextId);
                }

                if (!isUnique)
                {
                    if (Verbose) Console.WriteLine($"\t\t\tColision found by {Thread.CurrentThread.Name} in check #{checks}.");
                    break;
                };

                if (Verbose) Console.WriteLine($"\t\t\t{Thread.CurrentThread.Name} verified next id {checks} time(s).");
            }
        }
    }
    else
    {
#pragma warning disable 0162
        lock (sharedRepository)
        {
            nextId = sharedRepository.Items.Max(l => l.sequentialId) + 1;
        }
#pragma warning restore 0162
    }

    return nextId;
}

class Repo
{
    public List<Item> Items = new();
}

class Item
{
    public int sequentialId;
}
