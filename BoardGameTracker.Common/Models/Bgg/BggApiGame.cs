using System.Xml.Serialization;

namespace BoardGameTracker.Common.Models.Bgg;

[XmlRoot(ElementName="items")]
public class BggApiGames { 

	[XmlElement(ElementName="item")] 
	public BggRawGame[]? Games { get; set; }
}

[XmlRoot(ElementName="name")]
public class Name { 

	[XmlAttribute(AttributeName="type")] 
	public string Type { get; set; } 

	[XmlAttribute(AttributeName="sortindex")] 
	public int Sortindex { get; set; } 

	[XmlAttribute(AttributeName="value")] 
	public string Value { get; set; } 
}

[XmlRoot(ElementName="yearpublished")]
public class YearPublished { 

	[XmlAttribute(AttributeName="value")] 
	public int Value { get; set; } 
}

[XmlRoot(ElementName="minplayers")]
public class MinPlayers { 

	[XmlAttribute(AttributeName="value")] 
	public int Value { get; set; } 
}

[XmlRoot(ElementName="maxplayers")]
public class MaxPlayers { 

	[XmlAttribute(AttributeName="value")] 
	public int Value { get; set; } 
}

[XmlRoot(ElementName="minplaytime")]
public class MinPlayTime { 

	[XmlAttribute(AttributeName="value")] 
	public int Value { get; set; } 
}

[XmlRoot(ElementName="maxplaytime")]
public class MaxPlayTime { 

	[XmlAttribute(AttributeName="value")] 
	public int Value { get; set; } 
}

[XmlRoot(ElementName="minage")]
public class MinAge { 

	[XmlAttribute(AttributeName="value")] 
	public int Value { get; set; } 
}

[XmlRoot(ElementName="link")]
public class BggRawLink { 

	[XmlAttribute(AttributeName="type")] 
	public string Type { get; set; } 

	[XmlAttribute(AttributeName="id")] 
	public int Id { get; set; } 

	[XmlAttribute(AttributeName="value")] 
	public string Value { get; set; } 
}

[XmlRoot(ElementName="average")]
public class Average { 

	[XmlAttribute(AttributeName="value")] 
	public double Value { get; set; } 
}

[XmlRoot(ElementName="ratings")]
public class Ratings { 

	[XmlElement(ElementName="average")] 
	public Average Average { get; set; } 
}

[XmlRoot(ElementName="statistics")]
public class Statistics { 

	[XmlElement(ElementName="ratings")] 
	public Ratings Ratings { get; set; } 
}

[XmlRoot(ElementName="item")]
public class BggRawGame { 

	[XmlElement(ElementName="thumbnail")] 
	public string Thumbnail { get; set; } 

	[XmlElement(ElementName="image")] 
	public string Image { get; set; } 

	[XmlElement(ElementName="name")] 
	public List<Name> Names { get; set; } 

	[XmlElement(ElementName="description")] 
	public string Description { get; set; } 

	[XmlElement(ElementName="yearpublished")] 
	public YearPublished YearPublished { get; set; } 

	[XmlElement(ElementName="minplayers")] 
	public MinPlayers MinPlayers { get; set; } 

	[XmlElement(ElementName="maxplayers")] 
	public MaxPlayers MaxPlayers { get; set; } 

	[XmlElement(ElementName="minplaytime")] 
	public MinPlayTime MinPlayTime { get; set; } 

	[XmlElement(ElementName="maxplaytime")] 
	public MaxPlayTime MaxPlayTime { get; set; } 

	[XmlElement(ElementName="minage")] 
	public MinAge MinAge { get; set; } 

	[XmlElement(ElementName="link")] 
	public List<BggRawLink> Links { get; set; } 

	[XmlElement(ElementName="statistics")] 
	public Statistics Statistics { get; set; } 

	[XmlAttribute(AttributeName="type")] 
	public string Type { get; set; } 

	[XmlAttribute(AttributeName="id")] 
	public int Id { get; set; } 
}