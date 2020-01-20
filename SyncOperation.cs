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
    }
}