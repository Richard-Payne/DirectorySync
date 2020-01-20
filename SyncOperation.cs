using System;

namespace DirectorySync
{
    public class SyncOperation<T> 
    {
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

        public string GetFileOp(bool padded = false) {
            string padding = padded ? new string(' ', 4) : "";
            if (CopyToA)     return $"CopyToA{padding}";
            if (CopyToB)     return $"CopyToB{padding}";
            if (DeleteFromA) return "DeleteFromA";
            if (DeleteFromB) return "DeleteFromB";
            return "";
        }

        public string GetStatusOp(bool padded = false) {
            string padding = padded ? new string(' ', 5) : "";
            if (AddToStatus)      return $"AddToStatus{padding}";
            padding = padded ? new string(' ', 4) : "";
            if (UpdateStatus)     return $"UpdateStatus{padding}";
            if (DeleteFromStatus) return "DeleteFromStatus";
            return "";
        }

        public string ToString(int keyLength) {
            int padding;
            if (keyLength == -1) padding = 2;
            else padding = keyLength - Item.Key.Length;
            string opLine = $"{new string(' ', 4)}{Item.Key}{new string(' ', padding)}";
            opLine += $"{GetFileOp(padded: true)}  ";
            opLine += $"{GetStatusOp(padded: true)}  ";
            opLine += Reason;
            return opLine;
        }

        public override string ToString() {
            return ToString(-1);
        }
    }
}