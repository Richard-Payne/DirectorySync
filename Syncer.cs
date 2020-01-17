using System;
using System.Collections.Generic;
using System.Linq;

public class Syncer<T> {

    private class SyncOperation<TOp> {

        public SyncOperation(TOp item) {
            this.item = item;
        }

        public TOp item { get; }        
        public bool copyAToB { get; set; }
        public bool copyBToA { get; set; }
        public bool addAToStatus { get; set; }        
        public bool addBToStatus { get; set; }        
        public bool deleteFromA { get; set; }
        public bool deleteFromB { get; set; }
        public bool deleteFromStatus { get; set; }                
    }

    public class MatchResult<TMatch> {
        public TMatch A  { get; }
        public TMatch B  { get; }
        public bool IsANewer  { get; }
        public bool IsBNewer  { get; }

        public MatchResult(TMatch A, TMatch B, bool IsANewer, bool IsBNewer) {
            if (IsANewer && IsBNewer)
                throw new ArgumentException($"{nameof(IsANewer)} and {nameof(IsBNewer)} cannot both be true");
            
            this.A = A;
            this.B = B;
            this.IsANewer = IsANewer;
            this.IsBNewer = IsBNewer;
        }
    }

    public void Sync(ISet<T> setA, ISet<T> setB, ISet<T> status, Func<T, string> getId, Func<T, T, MatchResult<T>> matcher) {

        var ops = new Dictionary<string, SyncOperation<string>>();        

        foreach(T item in setA) {
            bool inB = setB.Select(i => matcher(i, item)).Where(m => m.IsANewer || m.IsBNewer).Count() > 0;
            bool inStatus = status.Select(i => matcher(i, item)).Where(m => m.IsANewer || m.IsBNewer).Count() > 0;
                        
            var op = new SyncOperation<string>(getId(item));
            if (!inB && !inStatus) {
                op.copyAToB = true;
                op.addAToStatus = true;
            } else if (!inB && inStatus) {
                op.deleteFromA = true;
                op.deleteFromStatus = true;
            } else if (inB && !inStatus) {
                op.addToStatus = true;
            } else {
                // In all three
                op = null;
            }             
            if (op != null)   
                ops.Add(op.item, op);
        } 

        foreach(T item in setB) {
            if (ops.Where(o => o.Value.item == getId(item)).Count() > 0) 
                continue;

            bool inStatus = status.Where(i => matcher(i, item)).Count() > 0;

            var op = new SyncOperation<string>(getId(item));
            if (!inStatus) {
                op.addToA = true;
                op.addToStatus = true;    
            } else if ()
        }
    }
}