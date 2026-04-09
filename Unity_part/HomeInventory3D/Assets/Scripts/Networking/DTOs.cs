using System;

namespace HomeInventory3D.Networking
{
    /// <summary>
    /// Container data from the backend API.
    /// </summary>
    [Serializable]
    public class ContainerDto
    {
        public string id;
        public string name;
        public string nfcId;
        public string qrCode;
        public string location;
        public string description;
        public float? widthMm;
        public float? heightMm;
        public float? depthMm;
        public string meshFilePath;
        public string thumbnailPath;
        public int itemCount;
        public string createdAt;
        public string updatedAt;
        public string lastScannedAt;
    }

    /// <summary>
    /// Inventory item data from the backend API.
    /// </summary>
    [Serializable]
    public class ItemDto
    {
        public string id;
        public string containerId;
        public string name;
        public string[] tags;
        public string description;
        public float? positionX;
        public float? positionY;
        public float? positionZ;
        public float? bboxMinX;
        public float? bboxMinY;
        public float? bboxMinZ;
        public float? bboxMaxX;
        public float? bboxMaxY;
        public float? bboxMaxZ;
        public float? rotationX;
        public float? rotationY;
        public float? rotationZ;
        public string photoPath;
        public string meshFilePath;
        public string thumbnailPath;
        public float? confidence;
        public string recognitionSource;
        public string status;
        public string createdAt;
        public string updatedAt;
    }

    /// <summary>
    /// Full 3D scene data — container + all items.
    /// </summary>
    [Serializable]
    public class SceneDto
    {
        public ContainerDto container;
        public ItemDto[] items;
    }

    /// <summary>
    /// SignalR event: item added during scan.
    /// Uses properties (not fields) because SignalR .NET client deserializes via System.Text.Json.
    /// </summary>
    public class ItemAddedEvent
    {
        public string id { get; set; }
        public string containerId { get; set; }
        public string name { get; set; }
        public string[] tags { get; set; }
        public float? positionX { get; set; }
        public float? positionY { get; set; }
        public float? positionZ { get; set; }
        public float? rotationX { get; set; }
        public float? rotationY { get; set; }
        public float? rotationZ { get; set; }
        public float? bboxMinX { get; set; }
        public float? bboxMinY { get; set; }
        public float? bboxMinZ { get; set; }
        public float? bboxMaxX { get; set; }
        public float? bboxMaxY { get; set; }
        public float? bboxMaxZ { get; set; }
        public string meshUrl { get; set; }
        public string thumbnailUrl { get; set; }
        public float? confidence { get; set; }
    }

    /// <summary>
    /// Search result from the API.
    /// </summary>
    [Serializable]
    public class SearchResultDto
    {
        public ItemDto[] items;
    }
}
