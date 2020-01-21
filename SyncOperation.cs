using System;

namespace DirectorySync
{
    public enum ItemOperation {
        None,
        CopyToA,
        CopyToB,
        DeleteFromA,
        DeleteFromB
    }

    public static class ItemOperationExtensions {
        private static int maxlen = 11;

        public static string ToString(this ItemOperation op, bool padded = false) {
            string name = op == ItemOperation.None ? "" : Enum.GetName(typeof(ItemOperation), op);
            return name + new string(' ', maxlen - name.Length);
        }
    }

    public enum StatusOperation {
        None,
        AddToStatus,
        UpdateStatus,
        DeleteFromStatus
    }

    public static class StatusOperationExtensions {
        private static int maxlen = 16;

        public static string ToString(this StatusOperation op, bool padded = false) {
            string name = op == StatusOperation.None ? "" : Enum.GetName(typeof(StatusOperation), op);
            return name + new string(' ', maxlen - name.Length);
        }
    }    

    public class SyncOperation<T> 
    {
        public SyncOperation(SyncItem<T> item) {
            this.Item = item;
        }

        public SyncItem<T> Item { get; set; }
        public string Reason { get; set; }
        public ItemOperation ItemOperation { get; set; }
        public StatusOperation StatusOperation { get; set; }

        public string ToString(int keyLength) {
            int padding;
            if (keyLength == -1) padding = 2;
            else padding = keyLength - Item.Key.Length;
            string opLine = $"{new string(' ', 4)}{Item.Key}{new string(' ', padding)}";
            opLine += ItemOperation.ToString(padded: true) + "  ";
            opLine += StatusOperation.ToString(padded: true) + "  ";
            opLine += Reason;
            return opLine;
        }

        public override string ToString() {
            return ToString(-1);
        }
    }
}