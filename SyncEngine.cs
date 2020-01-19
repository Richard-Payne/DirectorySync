using System;
using System.Collections.Generic;
using System.Linq;

public class SyncEngine<T> {

    private Func<SyncItem<T>,SyncItem<T>,SyncItem<T>> matcher;

    public SyncEngine(Func<SyncItem<T>,SyncItem<T>,SyncItem<T>> matcher) {
        this.matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
    }

    public List<SyncOperation<T>> GetChangeSet(ISet<SyncItem<T>> setA, ISet<SyncItem<T>> setB, ISet<SyncItem<T>> status) {

        if (setA == null) throw new ArgumentNullException(nameof(setA));
        if (setB == null) throw new ArgumentNullException(nameof(setB));
        if (status == null) throw new ArgumentNullException(nameof(status));

        var ops = new Dictionary<string, SyncOperation<T>>();

        Console.WriteLine($"setA = [{string.Join(",", setA.Select(i => i.Key))}]");
        Console.WriteLine($"setB = [{string.Join(",", setB.Select(i => i.Key))}]");
        Console.WriteLine($"status = [{string.Join(",", status.Select(i => i.Key))}]");

        Console.WriteLine("");
        Console.WriteLine("PROCESSING SET A");
        foreach(SyncItem<T> itemA in setA) {
            SyncItem<T> itemB = setB.Where(itemB => itemA.Key == itemB.Key).FirstOrDefault();
            SyncItem<T> itemStatus = status.Where(itemStatus => itemA.Key == itemStatus.Key).FirstOrDefault();

            Console.WriteLine($"itemA = {itemA?.ToString() ?? "null"}");
            Console.WriteLine($"itemB = {itemB?.ToString() ?? "null"}");
            Console.WriteLine($"itemStatus = {itemStatus?.ToString() ?? "null"}");            
                        
            var op = new SyncOperation<T>(itemA);
            if (itemB == null && itemStatus == null) {
                op.Reason = "New item in A";
                op.CopyToB = true;
                op.AddToStatus = true;
            } else if (itemB == null && itemStatus != null) {
                op.Reason = "Item removed from B";
                op.DeleteFromA = true;
                op.DeleteFromStatus = true;
            } else if (itemB != null && itemStatus == null) {
                var match = matcher(itemA, itemB);
                if (match == null) {
                    op.Reason = "New identical items in A and B";
                } else {
                    op.Item = match;
                    op.Reason = "Simultaneous updates in A and B";
                    op.CopyToA = op.Item.Id != itemA.Id;
                    op.CopyToB = op.Item.Id != itemB.Id;
                }
                op.AddToStatus = true;
            } else {
                var match = matcher(matcher(itemA, itemB) ?? itemA, itemStatus);
                if (match != null) {
                    op.Item = match;
                    op.CopyToA = op.Item.Id != itemA.Id;
                    op.CopyToB = op.Item.Id != itemB.Id;
                    op.UpdateStatus = op.Item?.Id != itemStatus.Id;
                }                
            }             
  
            ops.Add(op.Item.Id, op);
        } 

        Console.WriteLine("");
        Console.WriteLine("PROCESSING SET B");
        foreach(var itemB in setB) {
            Console.WriteLine($"itemB = ${itemB.ToString()}");
            Console.WriteLine(string.Join(",", ops.Keys));
            if (ops.Where(o => o.Value.Item.Key == itemB.Key).Count() > 0) {
                Console.WriteLine($"Item {itemB.Key} in setB skipped. Already set to be processed.");
                continue;
            }

            SyncItem<T> itemStatus = status.Where(itemStatus => itemB.Key == itemStatus.Key).FirstOrDefault();

            var op = new SyncOperation<T>(itemB);
            if (itemStatus == null) {
                op.Reason = "New item in B";
                op.CopyToA = true;
                op.AddToStatus = true;    
            } else {
                op.Reason = "Item removed from A";
                op.DeleteFromB = true;
                op.DeleteFromStatus = true;
            }
            ops.Add(op.Item.Id, op);
        }

        Console.WriteLine("");
        Console.WriteLine("PROCESSING STATUS");
        foreach(var itemStatus in status) {
            if (ops.Where(o => o.Value.Item.Key == itemStatus.Key).Count() > 0) 
                continue;

            var op = new SyncOperation<T>(itemStatus);
            op.Reason = "Item removed from A and B";
            op.DeleteFromStatus = true;
            ops.Add(op.Item.Id, op);
        }
        
        Console.WriteLine("");
        Console.WriteLine("PROCESSING DONE");

        return ops.Values.ToList();
    }
}

public class SyncItem<T> {
    public string Id { get; }

    public string Key { get; }

    public T Item { get; set; }

    public SyncItem(string id, string key, T item) {
        this.Id = id;
        this.Item = item;
        this.Key = key;
    }

    public override string ToString() {
        return $"[Id={Id}, Key={Key}, Item={Item}";
    }
}

public class SyncOperation<T> {

    public SyncOperation(SyncItem<T> item) {
        this.Item = item;
    }

    public SyncItem<T> Item { get; set; }
    public string Reason { get; set; }
    public bool CopyToB { get; set; }
    public bool CopyToA { get; set; }
    public bool AddToStatus { get; set; }
    public bool UpdateStatus { get; set; }
    public bool DeleteFromA { get; set; }
    public bool DeleteFromB { get; set; }
    public bool DeleteFromStatus { get; set; }                
}
