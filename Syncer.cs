using System;
using System.Collections.Generic;
using System.Linq;

public class Syncer<T> {

    private Func<SyncItem<T>, SyncItem<T>, SyncItem<T>> matcher;

    public Syncer(Func<SyncItem<T>, SyncItem<T>, SyncItem<T>> matcher) {
        this.matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
    }

    public List<SyncOperation<T>> GetChangeSet(ISet<SyncItem<T>> setA, ISet<SyncItem<T>> setB, ISet<SyncItem<T>> status) {

        var ops = new Dictionary<string, SyncOperation<T>>();        

        foreach(SyncItem<T> itemA in setA) {
            SyncItem<T> itemB = setB.Where(itemB => itemA.id == itemB.id).FirstOrDefault();
            SyncItem<T> itemStatus = status.Where(itemStatus => itemA.id == itemStatus.id).FirstOrDefault();
                        
            var op = new SyncOperation<T>(itemA);
            if (itemB == null && itemStatus == null) {
                op.copyToB = true;
                op.addToStatus = true;
            } else if (itemB == null && itemStatus != null) {
                op.deleteFromA = true;
                op.deleteFromStatus = true;
            } else if (itemB != null && itemStatus == null) {
                op.item = matcher(itemA, itemB) ?? itemA;
                op.addToStatus = true;
            } else {
                op.item = matcher(matcher(itemA, itemB) ?? itemA, itemStatus);
                op.copyToA = op.item != itemA;
                op.copyToB = op.item != itemB;
                op.addToStatus = op.item != itemStatus;
            }             
            if (op != null)   
                ops.Add(op.item.id, op);
        } 

        foreach(var itemB in setB) {
            if (ops.Where(o => o.Value.item.id == itemB.id).Count() > 0) 
                continue;

            SyncItem<T> itemStatus = status.Where(itemStatus => itemB.id == itemStatus.id).FirstOrDefault();

            var op = new SyncOperation<T>(itemB);
            if (itemStatus == null) {
                op.copyToA = true;
                op.addToStatus = true;    
            } else {
                op.deleteFromB = true;
                op.deleteFromStatus = true;
            }
        }

        foreach(var itemStatus in status) {
            if (ops.Where(o => o.Value.item.id == itemStatus.id).Count() > 0) 
                continue;

            var op = new SyncOperation<T>(itemStatus);
            op.deleteFromStatus = true;
        }

        return ops.Values.ToList();
    }

    public void Sync(IList<SyncOperation<T>> ops) {

    }
}

public class SyncItem<T> {
    public string id { get; }

    public T item { get; set; }

    public SyncItem(string id, T item) {
        this.id = id;
        this.item = item;
    }
}

public class SyncOperation<T> {

    public SyncOperation(SyncItem<T> item) {
        this.item = item;
    }

    public SyncItem<T> item { get; }        
    public bool copyToB { get; set; }
    public bool copyToA { get; set; }
    public bool addToStatus { get; set; }        
    public bool deleteFromA { get; set; }
    public bool deleteFromB { get; set; }
    public bool deleteFromStatus { get; set; }                
}
