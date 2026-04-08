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
    /// </summary>
    [Serializable]
    public class ItemAddedEvent
    {
        public string id;
        public string containerId;
        public string name;
        public string[] tags;
        public float? positionX;
        public float? positionY;
        public float? positionZ;
        public float? rotationX;
        public float? rotationY;
        public float? rotationZ;
        public float? bboxMinX;
        public float? bboxMinY;
        public float? bboxMinZ;
        public float? bboxMaxX;
        public float? bboxMaxY;
        public float? bboxMaxZ;
        public string meshUrl;
        public string thumbnailUrl;
        public float? confidence;
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
