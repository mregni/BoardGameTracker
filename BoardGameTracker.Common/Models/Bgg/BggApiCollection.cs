using System.Xml.Serialization;

namespace BoardGameTracker.Common.Models.Bgg;

[XmlRoot(ElementName = "image")]
public class Image
{
    [XmlText] public string Text { get; set; }
}

[XmlRoot(ElementName = "name")]
public class ImportName
{
    [XmlAttribute(AttributeName = "sortindex")]
    public int Sortindex { get; set; }

    [XmlText] public string Text { get; set; }
}

[XmlRoot(ElementName = "status")]
public class Status
{
    [XmlAttribute(AttributeName = "own")] 
    public int Own { get; set; }

    [XmlAttribute(AttributeName = "prevowned")]
    public int Prevowned { get; set; }

    [XmlAttribute(AttributeName = "fortrade")]
    public int Fortrade { get; set; }
    [XmlAttribute(AttributeName = "want")] 
    public int Want { get; set; }

    [XmlAttribute(AttributeName = "wanttoplay")]
    public int Wanttoplay { get; set; }

    [XmlAttribute(AttributeName = "wanttobuy")]
    public int Wanttobuy { get; set; }

    [XmlAttribute(AttributeName = "wishlist")]
    public int Wishlist { get; set; }

    [XmlAttribute(AttributeName = "preordered")]
    public int Preordered { get; set; }

    [XmlAttribute(AttributeName="lastmodified")] 
    public string LastModified { get; set; } 
}

[XmlRoot(ElementName = "item")]
public class Item
{
    [XmlElement(ElementName = "name")] 
    public ImportName Name { get; set; }
    
    [XmlElement(ElementName = "status")] 
    public Status Status { get; set; }
    
    [XmlElement(ElementName = "image")] 
    public Image Image { get; set; }

    [XmlAttribute(AttributeName = "objecttype")]
    public string Objecttype { get; set; }

    [XmlAttribute(AttributeName = "objectid")]
    public int Objectid { get; set; }

    [XmlAttribute(AttributeName = "subtype")]
    public string Subtype { get; set; }

    [XmlAttribute(AttributeName = "collid")]
    public int Collid { get; set; }

    [XmlText] public string Text { get; set; }
}

[XmlRoot(ElementName = "items")]
public class BggApiCollection
{
    [XmlElement(ElementName = "item")] public List<Item> Item { get; set; }
}